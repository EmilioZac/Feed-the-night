using UnityEngine;
using UnityEngine.UI;

namespace FeedTheNight.UI
{
    using FeedTheNight.Systems;

    /// <summary>
    /// HUD principal del jugador. Crea por código las tres barras:
    ///   🔴 Rojo   = Vida     (HealthSystem)
    ///   🟠 Naranja = Hambre  (HungerSystem)
    ///   🔵 Azul   = Energía  (EnergySystem)
    ///
    /// Añade este componente a un Empty en la escena y asigna las
    /// referencias en el Inspector. El Canvas se genera automáticamente.
    /// </summary>
    [AddComponentMenu("FeedTheNight/UI/Player HUD")]
    public class PlayerHUD : MonoBehaviour
    {
        // ── Inspector – Referencias de sistemas ───────────────────────────────
        [Header("Systems  (arrastrar desde el Player)")]
        public HealthSystem healthSystem;
        public HungerSystem hungerSystem;
        public EnergySystem energySystem;

        // ── Inspector – Estética ──────────────────────────────────────────────
        [Header("HUD Layout")]
        [Tooltip("Anchura de cada barra en píxeles.")]
        public float barWidth   = 220f;
        [Tooltip("Altura de cada barra en píxeles.")]
        public float barHeight  = 18f;
        [Tooltip("Margen desde la esquina inferior izquierda.")]
        public Vector2 margin   = new Vector2(24f, 24f);
        [Tooltip("Separación vertical entre barras.")]
        public float barSpacing = 28f;
        [Tooltip("Opacidad del fondo de cada barra (0=invisible, 1=sólido).")]
        [Range(0f, 1f)]
        public float bgAlpha    = 0.45f;

        [Header("Colors")]
        public Color healthColor  = new Color(0.87f, 0.18f, 0.18f, 1f);   // Rojo
        public Color hungerColor  = new Color(0.93f, 0.52f, 0.10f, 1f);   // Naranja
        public Color energyColor  = new Color(0.22f, 0.58f, 0.95f, 1f);   // Azul

        // ── Private ───────────────────────────────────────────────────────────
        private Image _healthFill;
        private Image _hungerFill;
        private Image _energyFill;

        private Canvas _canvas;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            BuildHUD();
            SubscribeEvents();
            RefreshAll();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        // ── HUD Builder ───────────────────────────────────────────────────────
        private void BuildHUD()
        {
            // ── Canvas ────────────────────────────────────────────────────────
            var canvasGO = new GameObject("PlayerHUD_Canvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas                  = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode       = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder     = 10;

            canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Tres barras (orden: Energía, Hambre, Vida de abajo a arriba) ─
            //   Se colocan desde la esquina inferior izquierda hacia arriba.
            _energyFill = CreateBar(canvasGO.transform, "Energy",
                energyColor, 0, out _);

            _hungerFill = CreateBar(canvasGO.transform, "Hunger",
                hungerColor, 1, out _);

            _healthFill = CreateBar(canvasGO.transform, "Health",
                healthColor, 2, out _);
        }

        /// <summary>
        /// Crea una barra completa (fondo + relleno + etiqueta).
        /// <paramref name="slotIndex"/> 0 = inferior, 1 = centro, 2 = superior.
        /// </summary>
        private Image CreateBar(Transform parent, string label, Color fillColor,
                                int slotIndex, out RectTransform barRect)
        {
            float yPos = margin.y + slotIndex * barSpacing;

            // ── Contenedor ────────────────────────────────────────────────────
            var container = new GameObject($"Bar_{label}");
            container.transform.SetParent(parent, false);
            var containerRT               = container.AddComponent<RectTransform>();
            containerRT.anchorMin         = Vector2.zero;
            containerRT.anchorMax         = Vector2.zero;
            containerRT.pivot             = Vector2.zero;
            containerRT.anchoredPosition  = new Vector2(margin.x, yPos);
            containerRT.sizeDelta         = new Vector2(barWidth, barHeight);

            // ── Fondo ─────────────────────────────────────────────────────────
            var bg = CreateImage(container.transform, "BG",
                new Color(0.05f, 0.05f, 0.05f, bgAlpha),
                Vector2.zero, Vector2.one);

            // ── Máscara (clip del fill) ───────────────────────────────────────
            var maskGO = new GameObject("Mask");
            maskGO.transform.SetParent(container.transform, false);
            var maskRT       = maskGO.AddComponent<RectTransform>();
            maskRT.anchorMin = Vector2.zero;
            maskRT.anchorMax = Vector2.one;
            maskRT.offsetMin = Vector2.zero;
            maskRT.offsetMax = Vector2.zero;
            var maskImg      = maskGO.AddComponent<Image>();
            maskImg.color    = Color.white;
            var mask         = maskGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // ── Fill ──────────────────────────────────────────────────────────
            var fill = CreateImage(maskGO.transform, "Fill", fillColor,
                Vector2.zero, Vector2.one);
            fill.type = Image.Type.Filled;
            fill.fillMethod    = Image.FillMethod.Horizontal;
            fill.fillOrigin    = (int)Image.OriginHorizontal.Left;
            fill.fillAmount    = 1f;

            // ── Brillo (highlight sup.) ───────────────────────────────────────
            var gloss = CreateImage(container.transform, "Gloss",
                new Color(1f, 1f, 1f, 0.08f),
                new Vector2(0f, 0.55f), Vector2.one);

            // ── Label (icono texto) ───────────────────────────────────────────
            var labelGO = new GameObject($"Label_{label}");
            labelGO.transform.SetParent(container.transform, false);
            var labelRT      = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.offsetMin = new Vector2(6f, 0f);
            labelRT.offsetMax = Vector2.zero;
            var text         = labelGO.AddComponent<Text>();
            text.text        = label.ToUpper();
            text.fontSize    = 10;
            text.color       = new Color(1f, 1f, 1f, 0.75f);
            text.alignment   = TextAnchor.MiddleLeft;
            text.font        = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            barRect = containerRT;
            return fill;
        }

        private Image CreateImage(Transform parent, string name, Color color,
                                   Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt       = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img      = go.AddComponent<Image>();
            img.color    = color;
            return img;
        }

        // ── Events ────────────────────────────────────────────────────────────
        private void SubscribeEvents()
        {
            if (healthSystem != null)
                healthSystem.OnHealthChanged += OnHealthChanged;
            if (hungerSystem != null)
                hungerSystem.OnHungerChanged += OnHungerChanged;
            if (energySystem != null)
                energySystem.OnEnergyChanged += OnEnergyChanged;
        }

        private void UnsubscribeEvents()
        {
            if (healthSystem != null)
                healthSystem.OnHealthChanged -= OnHealthChanged;
            if (hungerSystem != null)
                hungerSystem.OnHungerChanged -= OnHungerChanged;
            if (energySystem != null)
                energySystem.OnEnergyChanged -= OnEnergyChanged;
        }

        // ── Handlers ──────────────────────────────────────────────────────────
        private void OnHealthChanged(float val)
        {
            if (_healthFill != null && healthSystem != null)
                _healthFill.fillAmount = val / healthSystem.MaxHealth;
        }

        private void OnHungerChanged(float val)
        {
            if (_hungerFill != null)
                _hungerFill.fillAmount = val / 100f;
        }

        private void OnEnergyChanged(float val)
        {
            if (_energyFill != null && energySystem != null)
                _energyFill.fillAmount = val / energySystem.MaxEnergy;
        }

        // ── Initial refresh ───────────────────────────────────────────────────
        private void RefreshAll()
        {
            if (healthSystem != null && _healthFill != null)
                _healthFill.fillAmount = healthSystem.Health / healthSystem.MaxHealth;

            if (hungerSystem != null && _hungerFill != null)
                _hungerFill.fillAmount = hungerSystem.Hunger / 100f;

            if (energySystem != null && _energyFill != null)
                _energyFill.fillAmount = energySystem.Energy / energySystem.MaxEnergy;
        }
    }
}
