using UnityEngine;
using FeedTheNight.Systems;

namespace StarterAssets
{
    public class PlayerVisuals : MonoBehaviour
    {
        private Renderer[] _renderers;
        private Color _originalColor = Color.white;
        private float _damageFlashTimer;
        private HealthSystem _health;
        private MaterialPropertyBlock _propBlock;
        private static readonly int _baseColorProp = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            // Buscamos todos los renderers en los hijos
            var allRenderers = GetComponentsInChildren<Renderer>();
            
            // Filtramos para ignorar aquellos que pertenezcan al Kagune
            var rendererList = new System.Collections.Generic.List<Renderer>();
            foreach (var r in allRenderers)
            {
                if (r.GetComponentInParent<KaguneController>() != null)
                {
                    // Es parte del Kagune, lo ignoramos
                    continue;
                }
                rendererList.Add(r);
            }
            _renderers = rendererList.ToArray();
            
            _propBlock = new MaterialPropertyBlock();
            
            // Intentar capturar el color original del primer renderer con color
            foreach (var r in _renderers)
            {
                if (r.HasPropertyBlock()) continue;
                if (r.sharedMaterial.HasProperty("_BaseColor"))
                {
                    _originalColor = r.sharedMaterial.GetColor("_BaseColor");
                    break;
                }
                else if (r.sharedMaterial.HasProperty("_Color"))
                {
                    _originalColor = r.sharedMaterial.color;
                    break;
                }
            }
            _health = GetComponent<HealthSystem>();
        }

        private void OnEnable() => _health.OnDamaged += HandleDamaged;
        private void OnDisable() => _health.OnDamaged -= HandleDamaged;

        private void HandleDamaged(float amount) => _damageFlashTimer = 0.2f;

        private void Update()
        {
            if (_damageFlashTimer > 0) _damageFlashTimer -= Time.deltaTime;
        }

        public void UpdateVisuals(bool isCamouflaged, bool isBlocking, bool isExhausted, bool isCrouching)
        {
            Color targetColor = _originalColor;

            if (_damageFlashTimer > 0) targetColor = Color.red;
            else if (isBlocking) targetColor = isExhausted ? new Color(1f, 0.5f, 0f) : Color.yellow;

            ApplyColor(targetColor);
        }

        private void ApplyColor(Color color)
        {
            if (_renderers == null) return;
            
            _propBlock.SetColor(_baseColorProp, color);
            // Fallback para shaders estándar si no usan _BaseColor
            _propBlock.SetColor("_Color", color);

            foreach (var r in _renderers)
            {
                r.SetPropertyBlock(_propBlock);
            }
        }
    }
}

