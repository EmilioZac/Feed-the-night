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

        [Header("Obstacle Detection")]
        [Tooltip("Distance at which the NPC detects obstacles and decides to turn.")]
        public float detectionDistance = 2f;
        
        [Tooltip("Layers that NPC considers as obstacles (Walls, other objects).")]
        public LayerMask obstacleMask = ~0;

        [Tooltip("Height offset from NPC position to cast the detection raycast.")]
        public float raycastHeightOffset = 1f;

        private CharacterController _characterController;
        private NPCCivil _npcCivil;
        private float _debugLogTimer = 0f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _npcCivil = GetComponent<NPCCivil>();
            Debug.Log($"[NPCController - {gameObject.name}] Initialized on GameObject '{gameObject.name}'");
        }

        private void Update()
        {
            // Periodic debug log to diagnose movement issues (every 2 seconds)
            _debugLogTimer += Time.deltaTime;
            if (_debugLogTimer >= 2f)
            {
                _debugLogTimer = 0f;
                if (_npcCivil != null && _npcCivil.IsDead)
                {
                    Debug.Log($"[NPCController - {gameObject.name}] NPC is DEAD. Movement skipped.");
                }
                else
                {
                    string ccInfo = (_characterController != null)
                        ? $"CC Enabled: {_characterController.enabled}, Grounded: {_characterController.isGrounded}, Center: {_characterController.center}, Radius: {_characterController.radius}"
                        : "NO CharacterController!";
                    Debug.Log($"[NPCController - {gameObject.name}] Running. Speed: {moveSpeed}, Position: {transform.position}, {ccInfo}");
                }
            }

            // 1. If NPC is dead, stop all movement logic
            if (_npcCivil != null && _npcCivil.IsDead) return;

            // 2. Obstacle detection in front of the NPC
            float radius = (_characterController != null) ? _characterController.radius : 0.5f;
            // Start the raycast slightly in front of the NPC's collider to avoid hitting itself
            Vector3 rayStart = transform.position + Vector3.up * raycastHeightOffset + transform.forward * (radius + 0.1f);
            Vector3 rayDirection = transform.forward;

            // We use Physics.RaycastAll to find obstacles and ignore the NPC's own colliders
            RaycastHit[] hits = Physics.RaycastAll(rayStart, rayDirection, detectionDistance, obstacleMask);
            bool obstacleDetected = false;

            foreach (var hit in hits)
            {
                // Ignore trigger colliders and ignore hits on ourselves (our own gameobject or children)
                if (hit.collider.isTrigger || hit.transform.root == transform.root)
                {
                    continue;
                }

                obstacleDetected = true;
                
                // Choose a random side angle to turn to (e.g. between 90 and 270 degrees to avoid backing straight up if possible)
                float randomTurnAngle = Random.Range(90f, 270f);
                transform.Rotate(0f, randomTurnAngle, 0f);
                
                Debug.Log($"[NPCController - {gameObject.name}] Obstacle '{hit.collider.gameObject.name}' detected at {hit.distance:F2}m. Rotating by {randomTurnAngle:F0} degrees.");
                break; // One turn per frame is enough
            }

            // 3. Move forward
            if (_characterController != null && _characterController.enabled)
            {
                // SimpleMove applies gravity automatically
                _characterController.SimpleMove(transform.forward * moveSpeed);
            }
            else
            {
                // Fallback movement if CharacterController is disabled or not present
                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection ray in inspector/scene view
            Gizmos.color = Color.red;
            float radius = (_characterController != null) ? _characterController.radius : 0.5f;
            Vector3 rayStart = transform.position + Vector3.up * raycastHeightOffset + transform.forward * (radius + 0.1f);
            Gizmos.DrawRay(rayStart, transform.forward * detectionDistance);
        }
    }
}
