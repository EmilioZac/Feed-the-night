using UnityEngine;
using FeedTheNight.Systems;

namespace StarterAssets
{
    public class PlayerVisuals : MonoBehaviour
    {
        private Renderer _renderer;
        private Color _originalColor;
        private float _damageFlashTimer;
        private HealthSystem _health;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null) _originalColor = _renderer.material.color;
            _health = GetComponent<HealthSystem>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDamaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDamaged -= HandleDamaged;
            }
        }

        private void HandleDamaged(float amount)
        {
            _damageFlashTimer = 0.2f;
        }

        private void Update()
        {
            if (_damageFlashTimer > 0)
            {
                _damageFlashTimer -= Time.deltaTime;
            }
        }

        public void UpdateVisuals(bool isCamouflaged, bool isBlocking, bool isExhausted, bool isCrouching)
        {
            if (isCamouflaged)
            {
                SetColor(Color.white);
            }
            else if (_damageFlashTimer > 0)
            {
                SetColor(Color.red);
            }
            else if (isBlocking)
            {
                if (isExhausted)
                    SetColor(new Color(1f, 0.5f, 0f)); // Naranja Fatiga
                else
                    SetColor(Color.yellow); // Bloqueo Perfecto
            }
            else if (isCrouching)
            {
                SetColor(Color.blue);
            }
            else
            {
                SetColor(_originalColor);
            }
        }

        private void SetColor(Color color)
        {
            if (_renderer != null)
            {
                _renderer.material.color = color;
            }
        }
    }
}

