using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace FeedTheNight.Controllers
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float runSpeed = 8f;
        public float crouchSpeed = 2f;
        public float gravity = -9.81f;
        public float jumpHeight = 1.0f;

        [Header("State")]
        public State currentState;
        public enum State
        {
            Idle,
            Walk,
            Run,
            Crouch,
            Feed,
            Attack,
            Block,
            Camouflage,
            Fatigue
        }

        private CharacterController _controller;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;
        private InputAction _interactAction;
        private InputAction _jumpAction;

        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isCamouflaged;
        private float _blockDuration;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            // Cache visuals
            _renderer = GetComponent<Renderer>();
            if (_renderer != null) _originalColor = _renderer.material.color;

            // Cache actions safely
            if (_playerInput.actions == null)
            {
                Debug.LogError("PlayerInput has no Actions assigned! Please assign 'InputSystem_Actions' in the Inspector.");
                enabled = false;
                return;
            }

            _moveAction = _playerInput.actions["Move"];
            _sprintAction = _playerInput.actions["Sprint"];
            _crouchAction = _playerInput.actions["Crouch"];
            _interactAction = _playerInput.actions["Interact"];
            _jumpAction = _playerInput.actions["Jump"];
        }

        private void Update()
        {
            HandleGravity();
            HandleStateAndMovement();
        }

        private void HandleGravity()
        {
            _isGrounded = _controller.isGrounded;
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small force to keep grounded
            }

            _velocity.y += gravity * Time.deltaTime;
        }

        private void HandleStateAndMovement()
        {
            // 1. Read Input
            Vector2 input = _moveAction.ReadValue<Vector2>();
            Vector3 move = transform.right * input.x + transform.forward * input.y;
            bool isSprinting = _sprintAction.IsPressed();
            bool isCrouching = false; // Reset per frame for priority logic
            bool isFeeding = _interactAction.IsPressed();

            // Toggle Camouflage (T Check)
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                _isCamouflaged = !_isCamouflaged;
            }

            // If Camouflaged, ignore other combat/movement modifiers
            if (_isCamouflaged)
            {
                currentState = State.Camouflage;
                float targetSpeed = walkSpeed * 0.4f; // 40% speed
                
                if (_renderer != null) _renderer.material.color = Color.white;
                
                // Only move allowed
                _controller.Move(move * targetSpeed * Time.deltaTime);
                _controller.Move(_velocity * Time.deltaTime); // Gravity
                return; // Exit early to prevent other actions
            }

            // Combat Inputs
            bool isAttacking = false;
            if (_playerInput.actions.FindAction("Attack") != null)
                isAttacking = _playerInput.actions["Attack"].WasPressedThisFrame();
            
            bool isBlocking = Keyboard.current != null && Keyboard.current.fKey.isPressed;
            bool isDashingInput = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
            
            if (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed)
                isCrouching = true;

            // 2. Dash Logic
            if (isDashingInput && canDash)
            {
                StartCoroutine(PerformDash(move));
                return;
            }
            if (isDashing)
            {
                 _controller.Move(_velocity * Time.deltaTime);
                return; 
            }

            // 3. Determine State & Speed
            float finalSpeed = walkSpeed;
            
            // Visual Feedback Reset
            if (_renderer != null) _renderer.material.color = _originalColor;

            if (isFeeding)
            {
                currentState = State.Feed;
                move = Vector3.zero;
            }
            else if (isBlocking)
            {
                _blockDuration += Time.deltaTime;
                move = Vector3.zero; // Stop moving while blocking? Request said "relantize la mitad"
                // Correction based on previous prompt: blocking slows to 50%
                move = transform.right * input.x + transform.forward * input.y; // Ensure move is calculated
                finalSpeed = walkSpeed * 0.5f;

                if (_blockDuration > 4.0f)
                {
                    currentState = State.Fatigue;
                    if (_renderer != null) _renderer.material.color = new Color(1f, 0.5f, 0f); // Orange
                }
                else
                {
                    currentState = State.Block;
                    if (_renderer != null) _renderer.material.color = Color.yellow;
                }
            }
            else
            {
                _blockDuration = 0f; // Reset if not blocking

                if (isAttacking)
                {
                     currentState = State.Attack;
                     if (_renderer != null) _renderer.material.color = Color.green;
                }
                else if (isCrouching)
                {
                    currentState = State.Crouch;
                    finalSpeed = walkSpeed * 0.4f;
                    if (_renderer != null) _renderer.material.color = Color.blue;
                }
                else if (input.magnitude > 0.1f)
                {
                    if (isSprinting)
                    {
                        currentState = State.Run;
                        finalSpeed = runSpeed;
                    }
                    else
                    {
                        currentState = State.Walk;
                    }
                }
                else
                {
                    currentState = State.Idle;
                    finalSpeed = 0f;
                }
            }

            // 4. Apply Movement
            _controller.Move(move * finalSpeed * Time.deltaTime);

            // 5. Apply Vertical Velocity (Gravity)
            _controller.Move(_velocity * Time.deltaTime);

            // Optional: Jump
            if (_jumpAction.WasPressedThisFrame() && _isGrounded && currentState != State.Feed && currentState != State.Camouflage)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        [Header("Combat Settings")]
        public float dashDistance = 5f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 5f;
        private bool canDash = true;
        private bool isDashing = false;
        private Renderer _renderer;
        private Color _originalColor;

        private System.Collections.IEnumerator PerformDash(Vector3 direction)
        {
            canDash = false;
            isDashing = true;
            
            float startTime = Time.time;
            
            // If no direction, dash forward
            if (direction.magnitude < 0.1f) direction = transform.forward;
            else direction.Normalize();

            while (Time.time < startTime + dashDuration)
            {
                _controller.Move(direction * (dashDistance / dashDuration) * Time.deltaTime);
                yield return null;
            }

            isDashing = false;
            yield return new WaitForSeconds(dashCooldown);
            canDash = true;
        }
    }
}
