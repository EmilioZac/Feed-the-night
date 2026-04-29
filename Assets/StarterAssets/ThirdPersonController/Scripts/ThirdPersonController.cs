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
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;


        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;


        [Header("Systems Integration")]
        public EnergySystem EnergySystem;
        private HealthSystem _health;
        private HungerSystem _hunger;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;


#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private PlayerVisuals _visuals;
        private float _blockDuration;
        private float _blockResistance;
        private float _maxBlockResistance = 3.0f;
        private float _blockRecoveryTimer;
        private PlayerCombat _combat;
        private PlayerInteraction _interaction;
        private PlayerFrenzyState _frenzy;


        private PlayerAnimationController _anim;
        private bool _isDeadStateInitialized = false;

        public bool IsBlocking => (_combat != null && _combat.IsBlocking);

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _health = GetComponent<HealthSystem>();
            _hunger = GetComponent<HungerSystem>();

            if (EnergySystem == null) EnergySystem = GetComponent<EnergySystem>();
            _combat = GetComponent<PlayerCombat>();
            if (_combat == null) _combat = gameObject.AddComponent<PlayerCombat>();
            _anim = GetComponentInChildren<PlayerAnimationController>();
            if (_anim == null) _anim = gameObject.AddComponent<PlayerAnimationController>();

            if (GetComponentInChildren<PlayerAudioController>() == null) gameObject.AddComponent<PlayerAudioController>();

            _interaction = GetComponent<PlayerInteraction>();
            if (_interaction == null) _interaction = gameObject.AddComponent<PlayerInteraction>();

            _frenzy = GetComponent<PlayerFrenzyState>();
            if (_frenzy == null) _frenzy = gameObject.AddComponent<PlayerFrenzyState>();

            _visuals = GetComponent<PlayerVisuals>();
            if (_visuals == null) _visuals = gameObject.AddComponent<PlayerVisuals>();

            _blockResistance = _maxBlockResistance;

#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif


            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            // --- DEATH STATE ---
            if (_health != null && !_health.IsAlive)
            {
                if (!_isDeadStateInitialized)
                {
                    _isDeadStateInitialized = true;
                    if (_anim != null)
                    {
                        _anim.TriggerDeath();
                    }
                    // Subimos un poco al jugador para evitar que atraviese el suelo
                    transform.position += Vector3.up * 0.1f; 
                }

                _verticalVelocity = 0f;
                return;
            }

            // --- FRENZY STATE ---
            if (_hunger != null && _hunger.IsFrenzy)
            {
                _frenzy.UpdateFrenzy(_verticalVelocity);
                return;
            }


            JumpAndGravity();
            GroundedCheck();
            Move();
            HandleAdditionalActions();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }


        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            _anim.SetGrounded(Grounded);
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (_combat.IsDashing || _combat.IsAttacking)
            {
                _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
                
                _speed = 0f;
                _animationBlend = 0f;
                _anim.SetMoveSpeed(0f, 0f);
                return;
            }

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero || IsBlocking || _interaction.IsFeeding) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // Speed Modifiers & Logic
            float finalSpeed = (IsBlocking || _interaction.IsFeeding) ? 0f : _speed;
            
            if (_interaction.IsCamouflaged)
            {
                finalSpeed *= 0.4f;
            }

            if (_input.block)
            {
                _blockDuration += Time.deltaTime;
                // Consumo de Energía real (barra azul) -> 1 cada 2 segundos (0.5/s)
                if (EnergySystem != null && EnergySystem.Energy > 0)
                {
                    EnergySystem.ModifyEnergy(-Time.deltaTime * 0.5f);
                }
                finalSpeed *= 0.5f;
            }
            else
            {
                if (_blockDuration > 0)
                {
                    // Delay de estamina de 1 segundo al dejar de bloquear
                    if (EnergySystem != null) EnergySystem.ResetRegenDelay(1.0f);
                }

                _blockDuration = 0f;
                
                // Recuperación de resistencia si no se está bloqueando
                if (_blockResistance < _maxBlockResistance)
                {
                    _blockRecoveryTimer += Time.deltaTime;
                    if (_blockRecoveryTimer >= 1.5f) // Espera 1.5s para empezar a recuperar
                    {
                        _blockResistance += Time.deltaTime; // Recupera 1 por segundo
                        if (_blockResistance > _maxBlockResistance) _blockResistance = _maxBlockResistance;
                    }
                }
                else
                {
                    _blockRecoveryTimer = 0f;
                }
            }

            if (_input.crouch)
            {
                finalSpeed *= 0.4f;
            }

            // Actualizar visuales centralizadamente
            bool isExhausted = (_blockDuration > 5.0f) || (EnergySystem != null && EnergySystem.Energy <= 0);
            _visuals.UpdateVisuals(_interaction.IsCamouflaged, _input.block, isExhausted, _input.crouch);

            // check if running for energy system
            bool isActuallyRunning = _input.move != Vector2.zero && _input.sprint && (EnergySystem == null || EnergySystem.CanRun) && !_input.crouch && !_input.block && !_interaction.IsCamouflaged && !_interaction.IsFeeding;
            if (EnergySystem != null) EnergySystem.SetRunning(isActuallyRunning);
            if (!isActuallyRunning && _input.sprint) finalSpeed = MoveSpeed; // Force walk speed if cannot run

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero && !IsBlocking)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            if (!_interaction.IsFeeding) // Prevent movement while feeding if we want to lock it
            {
                _controller.Move(targetDirection.normalized * (finalSpeed * Time.deltaTime) +
                                 new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }
            else
            {
                _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }

            // update animator if using character
            _anim.SetMoveSpeed(_animationBlend, inputMagnitude);
            _anim.SetCrouch(_input.crouch);
            _anim.SetBlocking(IsBlocking);
        }

        private void JumpAndGravity()
        {
            if (_combat.IsDashing) _input.jump = false;

            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                _anim.SetJump(false);
                _anim.SetFreeFall(false);

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    bool energyOk = EnergySystem == null || EnergySystem.Energy >= EnergySystem.jumpDrainFlat;
                    if (energyOk && !_interaction.IsCamouflaged)
                    {
                        // the square root of H * -2 * G = how much velocity needed to reach desired height
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                        // update animator if using character
                        _anim.SetJump(true);

                        if (EnergySystem != null) EnergySystem.OnJump();
                    }
                    else
                    {
                        _input.jump = false;
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                // update animator if using character
                _anim.SetFreeFall(true);
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }


        private void HandleAdditionalActions()
        {
            _combat.HandleCombatActions();
            _interaction.HandleInteractions();
        }



        /// <summary>
        /// Intenta bloquear el daño. Devuelve la cantidad de daño que el jugador REALMENTE recibe.
        /// </summary>
        public float TryBlock(float damage)
        {
            return _combat.TryBlock(damage, _blockDuration);
        }
    }
}