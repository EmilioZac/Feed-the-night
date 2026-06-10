 using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif
using FeedTheNight.Systems;
using System.Collections;

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(HungerSystem))]
    [RequireComponent(typeof(StarterAssetsInputs))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        [Header("Physics")]
        public float Gravity = -15.0f;
        public float FallTimeout = 0.15f;
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f, BottomClamp = -30.0f, CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        [Header("Systems")]
        public EnergySystem EnergySystem;
        private HealthSystem _health;
        private HungerSystem _hunger;

        private float _cinemachineTargetYaw, _cinemachineTargetPitch;
        private float _speed, _animationBlend, _targetRotation = 0.0f, _rotationVelocity, _verticalVelocity;
        private float _terminalVelocity = 53.0f, _fallTimeoutDelta;
        private PlayerJump _playerJump;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private const float _threshold = 0.01f;

        private PlayerVisuals _visuals;
        private PlayerCombat _combat;
        private PlayerInteraction _interaction;
        private PlayerFrenzyState _frenzy;
        private PlayerAnimationController _anim;
        private float _blockDuration, _blockResistance, _maxBlockResistance = 3.0f, _blockRecoveryTimer;
        private bool _isDeadStateInitialized = false, _wasCrouching = false;
        public float ScreamDuration = 1.0f;
        private bool _isScreaming = false;
        private float _screamTimer = 0f;
        private bool _wasFrenzy = false;

        public bool IsBlocking => (_combat != null && _combat.IsBlocking);
        private bool IsCurrentDeviceMouse => 
#if ENABLE_INPUT_SYSTEM
            _playerInput.currentControlScheme == "KeyboardMouse";
#else
            false;
#endif


        private void Start()
        {
            if (_mainCamera == null) _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            if (_input == null)
            {
                Debug.LogError($"[ThirdPersonController] ¡Falta el componente StarterAssetsInputs en el objeto '{gameObject.name}'! Por favor, agrégalo en el Inspector.", this);
            }
            _health = GetComponent<HealthSystem>();
            _hunger = GetComponent<HungerSystem>();
            EnergySystem = EnergySystem ?? GetComponent<EnergySystem>();
            _combat = GetComponent<PlayerCombat>() ?? gameObject.AddComponent<PlayerCombat>();
            _anim = GetComponentInChildren<PlayerAnimationController>() ?? gameObject.AddComponent<PlayerAnimationController>();
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.GetComponent<PlayerAudioController>() == null) animator.gameObject.AddComponent<PlayerAudioController>();
            else if (GetComponentInChildren<PlayerAudioController>() == null) gameObject.AddComponent<PlayerAudioController>();
            _interaction = GetComponent<PlayerInteraction>() ?? gameObject.AddComponent<PlayerInteraction>();
            _frenzy = GetComponent<PlayerFrenzyState>() ?? gameObject.AddComponent<PlayerFrenzyState>();
            _visuals = GetComponent<PlayerVisuals>() ?? gameObject.AddComponent<PlayerVisuals>();
            _blockResistance = _maxBlockResistance;
            _playerJump = GetComponent<PlayerJump>() ?? gameObject.AddComponent<PlayerJump>();
            _fallTimeoutDelta = FallTimeout;
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#endif
        }

        private void Update()
        {
            if (_input == null) return;
            if (_health != null && !_health.IsAlive) {
                if (!_isDeadStateInitialized) {
                    _isDeadStateInitialized = true;
                    _anim?.TriggerDeath();
                    transform.position += Vector3.up * 0.1f; 
                }
                _verticalVelocity = 0f; return;
            }
            
            bool isInFrenzyRange = _hunger != null && _hunger.IsFrenzy;
            bool isPersistingDueToFeeding = _wasFrenzy && _interaction.IsFeeding;
            bool shouldBeInFrenzy = isInFrenzyRange || isPersistingDueToFeeding;

            if (shouldBeInFrenzy) { 
                if (isPersistingDueToFeeding && !isInFrenzyRange) {
                    Debug.Log($"[Frenzy Debug] Persisting frenzy state because player is still feeding (Hunger: {_hunger.Hunger:F1}%)");
                }
                HandleFrenzyScream();
                if (!_isScreaming) {
                    GroundedCheck();
                    _frenzy.UpdateFrenzy(_verticalVelocity); 
                    _interaction.HandleInteractions(); // Permitir comer en frenzy
                }
                return; 
            }
            else {
                _isScreaming = false; _screamTimer = 0f; _wasFrenzy = false;
            }

            // Bloquear Crouch y Camouflage si el Kagune está activo
            KaguneSpawner spawner = GetComponent<KaguneSpawner>();
            if (spawner != null && spawner.IsKaguneActive)
            {
                _input.crouch = false;
                _input.camouflage = false;
            }

            JumpAndGravity(); GroundedCheck(); Move(); HandleAdditionalActions();
        }

        private void HandleFrenzyScream()
        {
            if (!_wasFrenzy) {
                _wasFrenzy = true;
                _isScreaming = true;
                _screamTimer = ScreamDuration;
                // Limpiar todos los inputs que puedan mantener animaciones bugeadas
                _input.crouch = false;
                _input.block = false;
                _input.attack = false;
                _input.dash = false;
                // Limpiar todos los parámetros del Animator que puedan quedar activos
                _anim?.SetCrouch(false);
                _anim?.SetBlocking(false);
                if (_combat != null) _combat.ResetCombatStates();
                _anim?.SetScream(true);
                _speed = _animationBlend = 0f;
                _anim?.SetMoveSpeed(0f, 0f);
            }

            if (_isScreaming) {
                _screamTimer -= Time.deltaTime;
                _speed = _animationBlend = 0f;
                _anim?.SetMoveSpeed(0f, 0f);
                if (_screamTimer <= 0f) {
                    _isScreaming = false;
                    _anim?.SetScream(false);
                }
            }
        }

        private void LateUpdate()
        {
            if (_input == null) return;
            CameraRotation();
        }


         private void GroundedCheck()
        {
            Vector3 spherePos = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePos, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            _anim.SetGrounded(Grounded);
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float multiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += _input.look.x * multiplier;
                _cinemachineTargetPitch += _input.look.y * multiplier;
            }
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (_combat.IsDashing || _combat.IsAttacking) {
                _controller.Move(new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);
                _speed = _animationBlend = 0f; _anim.SetMoveSpeed(0, 0); return;
            }

            float targetSpeed = CalculateTargetSpeed();
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude;
            float inputMag = _input.analogMovement ? _input.move.magnitude : 1f;

            if (Mathf.Abs(currentHorizontalSpeed - targetSpeed) > 0.1f) _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMag, Time.deltaTime * SpeedChangeRate);
            else _speed = targetSpeed;
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

            if (_input.move != Vector2.zero && !IsBlocking) {
                Vector3 inputDir = new Vector3(_input.move.x, 0, _input.move.y).normalized;
                _targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0, Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime), 0);
            }

            if (_interaction.IsCamouflaged) {
                _input.crouch = false;
                _input.block = false;
            }

            // Detección de transiciones de agachado
            if (_input.crouch && !_wasCrouching) _anim.TriggerCrouchStart();
            else if (!_input.crouch && _wasCrouching) _anim.TriggerCrouchEnd();
            _wasCrouching = _input.crouch;

            Vector3 move = _interaction.IsFeeding ? Vector3.zero : Quaternion.Euler(0, _targetRotation, 0) * Vector3.forward * _speed;
            _controller.Move(move * Time.deltaTime + new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);
            
            // Ajustar velocidad del animator para agachado (umbral del animator es 1.9)
            float animSpeed = _animationBlend;
            if (_input.crouch && animSpeed > 0.01f) animSpeed = 2.0f; 

            _anim.SetMoveSpeed(animSpeed, inputMag); 
            _anim.SetCrouch(_input.crouch); 
            _anim.SetBlocking(IsBlocking);
        }

        private float CalculateTargetSpeed()
        {
            if (_input.move == Vector2.zero || IsBlocking || _interaction.IsFeeding) return 0f;
            float s = _input.sprint ? SprintSpeed : MoveSpeed;
            if (_input.block) {
                _blockDuration += Time.deltaTime; if (EnergySystem?.Energy > 0) EnergySystem.ModifyEnergy(-Time.deltaTime * 0.5f); s *= 0.5f;
            } else {
                if (_blockDuration > 0) EnergySystem?.ResetRegenDelay(1f);
                _blockDuration = 0f; UpdateBlockResistance();
            }
            if (_input.crouch) s *= 0.4f;
            else if (_interaction.IsCamouflaged) s *= 0.8f;
            
            // Forzar no correr si está agachado
            bool run = _input.move != Vector2.zero && _input.sprint && !(_input.crouch) && (EnergySystem?.CanRun ?? true) && !_input.block && !_interaction.IsCamouflaged && !_interaction.IsFeeding;
            EnergySystem?.SetRunning(run); if (!run && _input.sprint) s = MoveSpeed;
            _visuals.UpdateVisuals(_interaction.IsCamouflaged, _input.block, _blockDuration > 5f || (EnergySystem?.Energy <= 0), _input.crouch);
            return s;
        }

        private void UpdateBlockResistance() {
            if (_blockResistance < _maxBlockResistance) {
                _blockRecoveryTimer += Time.deltaTime;
                if (_blockRecoveryTimer >= 1.5f) { _blockResistance += Time.deltaTime; if (_blockResistance > _maxBlockResistance) _blockResistance = _maxBlockResistance; }
            } else _blockRecoveryTimer = 0f;
        }

        private void JumpAndGravity() {
            if (_combat.IsDashing || _input.crouch || _input.camouflage) { _input.jump = false; return; }
            if (Grounded) {
                _fallTimeoutDelta = FallTimeout; _anim.SetJump(false); _anim.SetFreeFall(false); if (_verticalVelocity < 0) _verticalVelocity = -2f;
            } else {
                if (_fallTimeoutDelta >= 0) _fallTimeoutDelta -= Time.deltaTime; else _anim.SetFreeFall(true);
            }

            if (_playerJump != null) {
                _playerJump.UpdateJump(ref _verticalVelocity, Gravity, Grounded);
            } else {
                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity) _verticalVelocity += Gravity * Time.deltaTime;
        }

        private static float ClampAngle(float lf, float min, float max) {
            if (lf < -360f) lf += 360f; if (lf > 360f) lf -= 360f; return Mathf.Clamp(lf, min, max);
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Grounded ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        public void HandleAdditionalActions() { _combat.HandleCombatActions(); _interaction.HandleInteractions(); }
        public float TryBlock(float damage) => _combat.TryBlock(damage, _blockDuration);
    }
}
