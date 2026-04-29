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

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

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

        public float FeedRange = 2.0f;

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

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

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
        private bool _canFeed;
        private GameObject _closestDeadNPC;
        private float _frenzyAttackTimer;
        private float _continuousFeedTimer;
        private float _continuousFeedTickTimer;
        private bool _isFeedingAction;
        private int _animIDFeed;
        private bool _isCamouflaged;


        // animation IDs
        private int _animIDCrouch;
        private int _animIDAttack;
        private int _animIDBlocked;
        private int _animIDDash;
        private int _animIDBlockedHit;
        private int _animIDDeath;
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

            _visuals = GetComponent<PlayerVisuals>();
            if (_visuals == null) _visuals = gameObject.AddComponent<PlayerVisuals>();

            _blockResistance = _maxBlockResistance;

#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

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
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDDeath, true);
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
                HandleFrenzyState();
                _input.jump = false; // Disable jump in frenzy
                return;
            }

            // --- CAMOUFLAGE TOGGLE ---
            if (_input.camouflage)
            {
                _isCamouflaged = !_isCamouflaged;
                _input.camouflage = false; // Reset toggle
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

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDCrouch = Animator.StringToHash("Crouch");
            _animIDAttack = Animator.StringToHash("Attack");
            _animIDBlocked = Animator.StringToHash("Blocked");
            _animIDFeed = Animator.StringToHash("Feed");
            _animIDDash = Animator.StringToHash("Dash");
            _animIDBlockedHit = Animator.StringToHash("BlockedHit");
            _animIDDeath = Animator.StringToHash("Death");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
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
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, 0f);
                    _animator.SetFloat(_animIDMotionSpeed, 0f);
                }
                return;
            }

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero || IsBlocking || _isFeedingAction) targetSpeed = 0.0f;

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
            float finalSpeed = (IsBlocking || _isFeedingAction) ? 0f : _speed;
            
            if (_isCamouflaged)
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
            _visuals.UpdateVisuals(_isCamouflaged, _input.block, isExhausted, _input.crouch);

            // check if running for energy system
            bool isActuallyRunning = _input.move != Vector2.zero && _input.sprint && (EnergySystem == null || EnergySystem.CanRun) && !_input.crouch && !_input.block && !_isCamouflaged && !_isFeedingAction;
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
            if (!_isFeedingAction) // Prevent movement while feeding if we want to lock it
            {
                _controller.Move(targetDirection.normalized * (finalSpeed * Time.deltaTime) +
                                 new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }
            else
            {
                _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
                _animator.SetBool(_animIDCrouch, _input.crouch);
                _animator.SetBool(_animIDBlocked, IsBlocking);
            }
        }

        private void JumpAndGravity()
        {
            if (_combat.IsDashing) _input.jump = false;

            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    bool energyOk = EnergySystem == null || EnergySystem.Energy >= EnergySystem.jumpDrainFlat;
                    if (energyOk && !_isCamouflaged)
                    {
                        // the square root of H * -2 * G = how much velocity needed to reach desired height
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                        // update animator if using character
                        if (_hasAnimator)
                        {
                            _animator.SetBool(_animIDJump, true);
                        }

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
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
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

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void HandleAdditionalActions()
        {
            _combat.HandleCombatActions();

            // Lógica de comer manteniendo presionado
            if (_input.feed && _canFeed && _closestDeadNPC != null)
            {
                _isFeedingAction = true;
                _continuousFeedTimer += Time.deltaTime;
                _continuousFeedTickTimer += Time.deltaTime;

                if (_continuousFeedTimer <= 8.0f)
                {
                    if (_continuousFeedTickTimer >= 1.0f)
                    {
                        if (_hunger != null) _hunger.ModifyHunger(2.5f);
                        _continuousFeedTickTimer -= 1.0f;
                    }
                }
                else
                {
                    // Al terminar los 8 segundos, destruimos el NPC
                    Destroy(_closestDeadNPC);
                    _closestDeadNPC = null;
                    _canFeed = false;
                    _isFeedingAction = false;
                    _input.feed = false;
                }
            }
            else
            {
                _isFeedingAction = false;
                _continuousFeedTimer = 0f;
                _continuousFeedTickTimer = 0f;
            }

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDFeed, _isFeedingAction);
            }
        }


        private void HandleFrenzyState()
        {
            GameObject nearestNPC = FindNearestNPC();
            Vector3 move = Vector3.zero;

            if (nearestNPC != null)
            {
                Vector3 direction = (nearestNPC.transform.position - transform.position);
                direction.y = 0;
                if (direction.magnitude > 1.5f) move = direction.normalized;
            }

            float frenzySpeed = SprintSpeed * 0.8f;
            _controller.Move(move * frenzySpeed * Time.deltaTime + new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);

            _frenzyAttackTimer += Time.deltaTime;
            if (_frenzyAttackTimer >= 0.5f)
            {
                _frenzyAttackTimer = 0f;
                bool timeOk = Time.time >= (_combat.AttackDuration + _combat.AttackCooldown); // Simplified check or use state
                if (_combat.CanAttack)
                {
                    int step = 1; // Simplified for now or logic to track combo
                    _combat.ExecuteFrenzyAttack(step);
                }
            }
        }

        private GameObject FindNearestNPC()
        {
            GameObject[] npcs = GameObject.FindGameObjectsWithTag("npc");
            GameObject nearest = null;
            float minDist = Mathf.Infinity;
            foreach (GameObject npc in npcs)
            {
                float dist = Vector3.Distance(npc.transform.position, transform.position);
                if (dist < minDist) { nearest = npc; minDist = dist; }
            }
            return nearest;
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("npc"))
            {
                var npcScript = other.gameObject.GetComponentInParent<FeedTheNight.NPCs.NPCCivil>();
                if (npcScript != null && npcScript.IsDead)
                {
                    _canFeed = true;
                    _closestDeadNPC = npcScript.gameObject;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("npc"))
            {
                var npcScript = other.gameObject.GetComponentInParent<FeedTheNight.NPCs.NPCCivil>();
                if (npcScript != null && _closestDeadNPC == npcScript.gameObject)
                {
                    _canFeed = false;
                    _closestDeadNPC = null;
                }
            }
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