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

        [Header("Combat Settings")]
        public float AttackDamage = 0.5f;
        public float AttackRange = 1.5f;
        public LayerMask HitLayers;
        public float DashDistance = 5f;
        public float DashDuration = 0.2f;
        public float DashCooldown = 5f;
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

        // Visuals & State
        private Renderer _renderer;
        private Color _originalColor;
        private float _damageFlashTimer;
        private float _blockDuration;
        private bool _canDash = true;
        private bool _isDashing = false;
        private bool _canFeed;
        private GameObject _closestDeadNPC;
        private float _frenzyAttackTimer;
        private bool _isCamouflaged;

        // animation IDs
        private int _animIDCrouch;
        private int _animIDAttack;
        private int _animIDBlocked;
        private int _animIDFeeding;

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

            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null) _originalColor = _renderer.material.color;

            if (_health != null) _health.OnDamaged += (amt) => _damageFlashTimer = 0.2f;

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
                if (_renderer != null) _renderer.material.color = Color.black;
                _verticalVelocity = 0f;
                Time.timeScale = 0f;
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
            _animIDFeeding = Animator.StringToHash("Feeding");
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
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

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
            float finalSpeed = _speed;
            
            if (_isCamouflaged)
            {
                finalSpeed *= 0.4f;
                if (_renderer != null) _renderer.material.color = Color.white;
            }
            else if (_damageFlashTimer > 0)
            {
                _damageFlashTimer -= Time.deltaTime;
                if (_renderer != null) _renderer.material.color = Color.red;
            }
            else
            {
                if (_renderer != null) _renderer.material.color = _originalColor;

                if (_input.block)
                {
                    _blockDuration += Time.deltaTime;
                    finalSpeed *= 0.5f;
                    if (_renderer != null) _renderer.material.color = _blockDuration > 4.0f ? new Color(1f, 0.5f, 0f) : Color.yellow;
                }
                else
                {
                    _blockDuration = 0f;
                }

                if (_input.crouch)
                {
                    finalSpeed *= 0.4f;
                    if (_renderer != null) _renderer.material.color = Color.blue;
                }
            }

            // check if running for energy system
            bool isActuallyRunning = _input.move != Vector2.zero && _input.sprint && (EnergySystem == null || EnergySystem.CanRun) && !_input.crouch && !_input.block && !_isCamouflaged;
            if (EnergySystem != null) EnergySystem.SetRunning(isActuallyRunning);
            if (!isActuallyRunning && _input.sprint) finalSpeed = MoveSpeed; // Force walk speed if cannot run

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
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
            if (!_input.interact || !_canFeed) // Prevent movement while feeding if we want to lock it
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
                _animator.SetBool(_animIDBlocked, _input.block);
            }
        }

        private void JumpAndGravity()
        {
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
                    bool energyOk = EnergySystem == null || EnergySystem.Energy >= 10f;
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
            if (_input.attack)
            {
                PerformAttack();
                _input.attack = false;
            }

            if (_input.dash && _canDash && !_isDashing)
            {
                Vector3 moveDir = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                StartCoroutine(PerformDash(moveDir));
                _input.dash = false;
            }

            if (_input.interact)
            {
                if (_canFeed)
                {
                    ConsumeNPC();
                }
                _input.interact = false;
            }

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDFeeding, _input.interact && _canFeed);
            }

            // Reset canFeed per frame (it will be set in OnTriggerStay)
            _canFeed = false;
        }

        private void PerformAttack()
        {
            if (_hasAnimator) _animator.SetTrigger(_animIDAttack);
            if (_renderer != null) _renderer.material.color = Color.green;

            if (EnergySystem != null)
            {
                EnergySystem.ModifyEnergy(-EnergySystem.maxEnergy * 0.005f);
                EnergySystem.ResetRegenDelay(0.4f);
            }

            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1f, AttackRange, HitLayers);
            foreach (var hit in hits)
            {
                if (hit.transform.root == transform.root) continue;

                var npcCivil = hit.GetComponentInParent<FeedTheNight.NPCs.NPCCivil>();
                if (npcCivil != null)
                {
                    npcCivil.TakeDamage(AttackDamage);
                    continue;
                }

                HealthSystem targetHealth = hit.GetComponentInParent<HealthSystem>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(AttackDamage);
                }
            }
        }

        private IEnumerator PerformDash(Vector3 direction)
        {
            _canDash = false;
            _isDashing = true;

            if (EnergySystem != null) EnergySystem.ModifyEnergy(-EnergySystem.maxEnergy * 0.01f);

            float startTime = Time.time;
            if (direction.magnitude < 0.1f) direction = transform.forward;

            while (Time.time < startTime + DashDuration)
            {
                _controller.Move(direction * (DashDistance / DashDuration) * Time.deltaTime);
                yield return null;
            }

            _isDashing = false;
            yield return new WaitForSeconds(DashCooldown);
            _canDash = true;
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
                PerformAttack();
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

        private void ConsumeNPC()
        {
            if (_closestDeadNPC != null)
            {
                if (_hunger != null) _hunger.Feed(HungerSystem.NPCType.Civil);
                Destroy(_closestDeadNPC);
                _closestDeadNPC = null;
                _canFeed = false;
            }
        }
    }
}