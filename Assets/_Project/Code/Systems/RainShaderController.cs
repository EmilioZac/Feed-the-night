using System.Collections.Generic;
using UnityEngine;

namespace FeedTheNight.Systems
{
    /// <summary>
    /// Drives the _Wetness parameter on all Renderers that use the
    /// FeedTheNight/RainSurface shader without mutating shared material assets.
    ///
    /// Usage:
    ///   1. Add this component to any GameObject in the scene.
    ///   2. Set Wetness (0 = dry, 1 = fully wet) from the Inspector or via
    ///      SetWetness(float) at runtime (e.g. from a WeatherManager).
    /// </summary>
    [AddComponentMenu("FeedTheNight/Systems/Rain Shader Controller")]
    public class RainShaderController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Wetness")]
        [Range(0f, 1f)]
        [Tooltip("0 = dry surface, 1 = fully wet with ripples.")]
        [SerializeField] private float _wetness = 0f;

        [Header("Settings")]
        [Tooltip("If true, targets are refreshed every frame (slow). Disable for static scenes.")]
        [SerializeField] private bool _dynamicRefresh = false;

        [Tooltip("Seconds between target refreshes when DynamicRefresh is enabled.")]
        [SerializeField] private float _refreshInterval = 5f;

        // ── Private ───────────────────────────────────────────────────────────
        private static readonly int WetnessID = Shader.PropertyToID("_Wetness");
        private const string ShaderName = "FeedTheNight/RainSurface";

        private readonly List<Renderer> _targets        = new List<Renderer>();
        private MaterialPropertyBlock   _propertyBlock;
        private float                   _refreshTimer;

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>Set wetness at runtime (e.g. from a WeatherManager).</summary>
        public void SetWetness(float value)
        {
            _wetness = Mathf.Clamp01(value);
            ApplyWetness();
        }

        /// <summary>Current wetness value.</summary>
        public float Wetness => _wetness;

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            RefreshTargets();
        }

        private void Update()
        {
            if (_dynamicRefresh)
            {
                _refreshTimer += Time.deltaTime;
                if (_refreshTimer >= _refreshInterval)
                {
                    _refreshTimer = 0f;
                    RefreshTargets();
                }
            }

            ApplyWetness();
        }

        // ── Internals ─────────────────────────────────────────────────────────

        /// <summary>
        /// Scans the entire scene for Renderers that use the RainSurface shader
        /// and caches them so we avoid FindObjectsOfType every frame.
        /// </summary>
        private void RefreshTargets()
        {
            _targets.Clear();

            Renderer[] allRenderers = FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (Renderer r in allRenderers)
            {
                foreach (Material mat in r.sharedMaterials)
                {
                    if (mat != null && mat.shader != null &&
                        mat.shader.name == ShaderName)
                    {
                        _targets.Add(r);
                        break; // one match per renderer is enough
                    }
                }
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[RainShaderController] Found {_targets.Count} target(s) using '{ShaderName}'.");
            #endif
        }

        /// <summary>
        /// Pushes _Wetness to every cached Renderer via a MaterialPropertyBlock
        /// so shared material assets are NOT mutated.
        /// </summary>
        private void ApplyWetness()
        {
            _propertyBlock.SetFloat(WetnessID, _wetness);

            foreach (Renderer r in _targets)
            {
                if (r == null) continue;
                r.SetPropertyBlock(_propertyBlock);
            }
        }

        // ── Validation ───────────────────────────────────────────────────────
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Live preview in the Editor without entering Play Mode
            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
            if (_targets.Count == 0) RefreshTargets();
            ApplyWetness();
        }
        #endif
    }
}
