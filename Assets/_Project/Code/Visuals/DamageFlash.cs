using UnityEngine;
using FeedTheNight.Systems;

namespace FeedTheNight.Visuals
{
    /// <summary>
    /// Componente simple que hace que un objeto parpadee en rojo al recibir daño.
    /// Útil para NPCs o cualquier objeto con HealthSystem.
    /// El Player no lo necesita porque ya tiene su propia lógica en PlayerController.
    /// </summary>
    [AddComponentMenu("FeedTheNight/Visuals/Damage Flash")]
    [RequireComponent(typeof(HealthSystem))]
    public class DamageFlash : MonoBehaviour
    {
        [Header("Settings")]
        public Renderer targetRenderer;
        public Color flashColor = Color.red;
        public float duration = 0.2f;

        private Color _originalColor;
        private float _timer;
        private HealthSystem _health;

        private void Awake()
        {
            _health = GetComponent<HealthSystem>();
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            
            if (targetRenderer != null) _originalColor = targetRenderer.material.color;
        }

        private void OnEnable()
        {
            if (_health != null) _health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(float amount)
        {
            _timer = duration;
        }

        private void Update()
        {
            if (targetRenderer == null) return;

            if (_timer > 0)
            {
                _timer -= Time.deltaTime;
                targetRenderer.material.color = flashColor;
            }
            else
            {
                targetRenderer.material.color = _originalColor;
            }
        }
    }
}
