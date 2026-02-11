using UnityEngine;
using UnityEngine.InputSystem;

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
        public float jumpHeight = 1.0f; // Added for basic traversal if needed

        [Header("State")]
        public State currentState;
        public enum State
        {
            Idle,
            Walk,
            Run,
            Crouch,
            Feed
        }

        private CharacterController _controller;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;
        private InputAction _interactAction; // Used for Feed
        private InputAction _jumpAction;

        private Vector3 _velocity;
        private bool _isGrounded;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _playerInput = GetComponent<PlayerInput>();
        }

        private void Start()
        {
            // Cache actions
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
            bool isCrouching = _crouchAction.IsPressed();
            bool isFeeding = _interactAction.IsPressed(); // Placeholder logic for Feed

            // 2. Determine State & Speed
            float targetSpeed = walkSpeed;

            if (isFeeding)
            {
                currentState = State.Feed;
                move = Vector3.zero; // Lock movement
            }
            else if (isCrouching)
            {
                currentState = State.Crouch;
                targetSpeed = crouchSpeed;
                // Reduce height logic could go here
            }
            else if (input.magnitude > 0.1f)
            {
                if (isSprinting)
                {
                    currentState = State.Run;
                    targetSpeed = runSpeed;
                }
                else
                {
                    currentState = State.Walk;
                }
            }
            else
            {
                currentState = State.Idle;
                targetSpeed = 0f;
            }

            // 3. Apply Movement
            _controller.Move(move * targetSpeed * Time.deltaTime);

            // 4. Apply Vertical Velocity (Gravity)
            _controller.Move(_velocity * Time.deltaTime);

            // Optional: Jump
            if (_jumpAction.WasPressedThisFrame() && _isGrounded && currentState != State.Feed)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
    }
}
