using UnityEngine;

namespace FeedTheNight.NPCs
{
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("FeedTheNight/NPCs/NPC Controller")]
    public class NPCController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Walking speed of the NPC.")]
        public float moveSpeed = 2f;

        [Tooltip("Speed multiplier when fleeing from the player.")]
        public float fleeSpeedMultiplier = 2f;

        [Header("Obstacle Detection")]
        [Tooltip("Distance at which the NPC detects obstacles and decides to turn.")]
        public float detectionDistance = 2f;
        
        [Tooltip("Layers that NPC considers as obstacles (Walls, other objects).")]
        public LayerMask obstacleMask = ~0;

        [Tooltip("Height offset from NPC position to cast the detection raycast.")]
        public float raycastHeightOffset = 1f;

        private CharacterController _characterController;
        private NPCCivil _npcCivil;
        private VisionCone _visionCone;

        // Flee state
        private bool _isFleeing = false;
        private Transform _playerTransform;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _npcCivil = GetComponent<NPCCivil>();
            _visionCone = GetComponent<VisionCone>();
        }

        private void OnEnable()
        {
            if (_visionCone != null)
            {
                _visionCone.OnPlayerDetected += OnPlayerDetected;
                _visionCone.OnPlayerLost += OnPlayerLost;
            }
        }

        private void OnDisable()
        {
            if (_visionCone != null)
            {
                _visionCone.OnPlayerDetected -= OnPlayerDetected;
                _visionCone.OnPlayerLost -= OnPlayerLost;
            }
        }

        private void OnPlayerDetected()
        {
            _isFleeing = true;

            // Find the player transform to know which direction to flee from
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }

            Debug.Log($"[NPCController - {gameObject.name}] ¡HUYENDO del jugador!");
        }

        private void OnPlayerLost()
        {
            _isFleeing = false;
            _playerTransform = null;
            Debug.Log($"[NPCController - {gameObject.name}] Jugador perdido. Volviendo a patrullar.");
        }

        private void Update()
        {
            // 1. If NPC is dead, stop all movement logic
            if (_npcCivil != null && _npcCivil.IsDead) return;

            // 2. Determine movement direction
            if (_isFleeing && _playerTransform != null)
            {
                HandleFleeMovement();
            }
            else
            {
                HandleWanderMovement();
            }
        }

        private void HandleFleeMovement()
        {
            // Calculate direction AWAY from the player
            Vector3 fleeDirection = (transform.position - _playerTransform.position);
            fleeDirection.y = 0f; // Keep horizontal
            fleeDirection.Normalize();

            // Rotate the NPC to face the flee direction
            if (fleeDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(fleeDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }

            // Check for obstacles while fleeing
            float radius = (_characterController != null) ? _characterController.radius : 0.5f;
            Vector3 rayStart = transform.position + Vector3.up * raycastHeightOffset + transform.forward * (radius + 0.1f);

            RaycastHit[] hits = Physics.RaycastAll(rayStart, transform.forward, detectionDistance, obstacleMask);
            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger || hit.transform.root == transform.root)
                    continue;

                // If obstacle ahead while fleeing, turn sideways to dodge it
                float dodgeAngle = Random.value > 0.5f ? 90f : -90f;
                transform.Rotate(0f, dodgeAngle, 0f);
                break;
            }

            // Move forward (away from player) at flee speed
            float currentSpeed = moveSpeed * fleeSpeedMultiplier;
            if (_characterController != null && _characterController.enabled)
            {
                _characterController.SimpleMove(transform.forward * currentSpeed);
            }
            else
            {
                transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.Self);
            }
        }

        private void HandleWanderMovement()
        {
            // Obstacle detection in front of the NPC
            float radius = (_characterController != null) ? _characterController.radius : 0.5f;
            Vector3 rayStart = transform.position + Vector3.up * raycastHeightOffset + transform.forward * (radius + 0.1f);
            Vector3 rayDirection = transform.forward;

            RaycastHit[] hits = Physics.RaycastAll(rayStart, rayDirection, detectionDistance, obstacleMask);

            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger || hit.transform.root == transform.root)
                    continue;

                float randomTurnAngle = Random.Range(90f, 270f);
                transform.Rotate(0f, randomTurnAngle, 0f);
                break;
            }

            // Move forward at normal speed
            if (_characterController != null && _characterController.enabled)
            {
                _characterController.SimpleMove(transform.forward * moveSpeed);
            }
            else
            {
                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection ray in inspector/scene view
            float radius = 0.5f;
            if (_characterController != null) radius = _characterController.radius;

            Vector3 rayStart = transform.position + Vector3.up * raycastHeightOffset + transform.forward * (radius + 0.1f);

            Gizmos.color = _isFleeing ? Color.yellow : Color.red;
            Gizmos.DrawRay(rayStart, transform.forward * detectionDistance);
        }
    }
}
