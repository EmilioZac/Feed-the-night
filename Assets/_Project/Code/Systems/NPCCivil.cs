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
    }
}
