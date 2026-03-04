using UnityEngine;

namespace FeedTheNight.NPCs
{
    /// <summary>
    /// Script para NPCs civiles con salud integrada.
    /// Maneja la vida, la muerte y el feedback visual.
    /// </summary>
    [AddComponentMenu("FeedTheNight/NPCs/NPC Civil")]
    public class NPCCivil : MonoBehaviour
    {
        [Header("Health Settings")]
        public float maxHealth = 1f;
        [SerializeField] private float _currentHealth;

        [Header("Visuals")]
        [Tooltip("Arrastra aquí el objeto que tiene el MeshRenderer (ej. el cubo).")]
        public Renderer targetRenderer;
        public Color deathColor = Color.red;

        private bool _isDead;
        public bool IsDead => _isDead;

        private void Awake()
        {
            _currentHealth = maxHealth;
            // Intento automático de encontrar el Renderer si no se asigna
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        }

        /// <summary>
        /// Aplica daño al NPC directamente.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;
            Debug.Log($"[NPC] {gameObject.name} recibió {amount} de daño (Vida: {_currentHealth})");

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            _currentHealth = 0;

            // Feedback visual: Rojo
            if (targetRenderer != null)
            {
                targetRenderer.material.color = deathColor;
            }

            // Congelar física: todos los Rigidbodies (root + hijos)
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.linearVelocity   = Vector3.zero;
                rb.angularVelocity  = Vector3.zero;
                rb.isKinematic      = true;
                rb.constraints      = RigidbodyConstraints.FreezeAll;
            }

            // Desactivar CharacterController si existe
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Desactivar lógica de movimiento u otros scripts
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script != this)
                {
                    script.enabled = false;
                }
            }

            Debug.Log($"<color=red>[NPC] {gameObject.name} HA MUERTO.</color> Puedes alimentarte con 'E'.");
        }

        private void OnDrawGizmosSelected()
        {
            // Color según estado: cyan = vivo (zona feeding), rojo = muerto (listo para comer)
            Color zoneColor = _isDead ? new Color(1f, 0f, 0f, 0.2f) : new Color(0f, 1f, 1f, 0.15f);
            Color wireColor = _isDead ? Color.red : Color.cyan;

            // Dibuja el BoxCollider trigger como zona de feeding
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(
                    transform.TransformPoint(col.center),
                    transform.rotation,
                    transform.lossyScale
                );
                Gizmos.color = zoneColor;
                Gizmos.DrawCube(Vector3.zero, col.size);
                Gizmos.color = wireColor;
                Gizmos.DrawWireCube(Vector3.zero, col.size);
                Gizmos.matrix = oldMatrix;
            }
            else
            {
                // Fallback: esfera si no hay BoxCollider
                Gizmos.color = wireColor;
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            }
        }
    }
}
