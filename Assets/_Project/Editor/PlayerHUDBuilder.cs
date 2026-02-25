using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace FeedTheNight.Editor
{
    /// <summary>
    /// Menú: Tools ▶ FeedTheNight ▶ Create Player HUD
    ///
    /// Genera en la Hierarchy todos los GameObjects del HUD:
    ///   PlayerHUD (Canvas)
    ///   └── StatusBars
    ///       ├── HealthBar   (rojo,   "VIDA")
    ///       ├── HungerBar   (naranja,"HAMBRE")
    ///       └── EnergyBar   (azul,   "ENERGIA")
    ///
    /// Cada barra tiene: fondo oscuro + Fill coloreado + texto % + etiqueta.
    /// Al terminar añade PlayerHUDController al Canvas raíz con todas las
    /// referencias ya asignadas (sólo falta arrastrar el Player).
    /// </summary>
    public static class PlayerHUDBuilder
    {
        // ── Colores ───────────────────────────────────────────────────────────
        static readonly Color HealthFill  = new Color(0.87f, 0.18f, 0.18f);   // Rojo
        static readonly Color HungerFill  = new Color(0.93f, 0.52f, 0.10f);   // Naranja
        static readonly Color EnergyFill  = new Color(0.22f, 0.58f, 0.95f);   // Azul
        static readonly Color BarBG       = new Color(0.05f, 0.05f, 0.05f, 0.75f);

        const float BAR_W    = 230f;
        const float BAR_H    = 22f;
        const float SPACING  = 32f;
        const float MARGIN_X = 20f;
        const float MARGIN_Y = 20f;

        // ── MenuItem ─────────────────────────────────────────────────────────
        [MenuItem("Tools/FeedTheNight/Create Player HUD")]
        public static void CreateHUD()
        {
            // Si ya existe, confirmar sobreescritura
            var existing = GameObject.Find("PlayerHUD");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Player HUD",
                    "Ya existe un 'PlayerHUD' en la escena.\n¿Deseas reemplazarlo?",
                    "Reemplazar", "Cancelar"))
                    return;
                Undo.DestroyObjectImmediate(existing);
            }

            // ── Canvas ────────────────────────────────────────────────────────
            var canvasGO = new GameObject("PlayerHUD");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create PlayerHUD");

            var canvas            = canvasGO.AddComponent<Canvas>();
            canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder   = 10;

            var scaler            = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode    = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Contenedor de barras (esquina inferior izquierda) ─────────────
            var barsRT = CreatePanel(canvasGO.transform, "StatusBars",
                new Vector2(MARGIN_X, MARGIN_Y),
                new Vector2(BAR_W, MARGIN_Y + SPACING * 3f),
                new Color(0, 0, 0, 0));   // transparente

            // ── Barras (índice 0 = inferior) ──────────────────────────────────
            Image healthFill, hungerFill, energyFill;
            Text  healthPct,  hungerPct,  energyPct;

            CreateBar(barsRT, "HealthBar",  "VIDA",    HealthFill, 2,
                out healthFill, out healthPct);
            CreateBar(barsRT, "HungerBar",  "HAMBRE",  HungerFill, 1,
                out hungerFill, out hungerPct);
            CreateBar(barsRT, "EnergyBar",  "ENERGÍA", EnergyFill, 0,
                out energyFill, out energyPct);

            // ── Controlador runtime ───────────────────────────────────────────
            var ctrl = canvasGO.AddComponent<FeedTheNight.UI.PlayerHUDController>();
            // Asignar referencias via SerializedObject para que Unity las guarde
            var so = new SerializedObject(ctrl);
            so.FindProperty("healthFill").objectReferenceValue  = healthFill;
            so.FindProperty("hungerFill").objectReferenceValue  = hungerFill;
            so.FindProperty("energyFill").objectReferenceValue  = energyFill;
            so.FindProperty("healthText").objectReferenceValue  = healthPct;
            so.FindProperty("hungerText").objectReferenceValue  = hungerPct;
            so.FindProperty("energyText").objectReferenceValue  = energyPct;
            so.ApplyModifiedProperties();

            // ── Marcar escena como modificada ─────────────────────────────────
            Selection.activeGameObject = canvasGO;
            EditorUtility.SetDirty(canvasGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[PlayerHUDBuilder] ✅ HUD creado. Arrastra el Player al PlayerHUDController en el Inspector.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        static RectTransform CreatePanel(Transform parent, string name,
            Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt         = go.AddComponent<RectTransform>();
            rt.anchorMin   = Vector2.zero;
            rt.anchorMax   = Vector2.zero;
            rt.pivot       = Vector2.zero;
            rt.anchoredPosition = pos;
            rt.sizeDelta   = size;
            var img        = go.AddComponent<Image>();
            img.color      = color;
            return rt;
        }

        static void CreateBar(RectTransform parent, string name, string label,
            Color fillColor, int slotIndex,
            out Image fillImage, out Text percentText)
        {
            float yPos = MARGIN_Y * 0.5f + slotIndex * SPACING;

            // ── Fondo ─────────────────────────────────────────────────────────
            var bgGO       = new GameObject(name);
            bgGO.transform.SetParent(parent, false);
            var bgRT       = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.zero;
            bgRT.pivot     = Vector2.zero;
            bgRT.anchoredPosition = new Vector2(0f, yPos);
            bgRT.sizeDelta = new Vector2(BAR_W, BAR_H);
            var bgImg      = bgGO.AddComponent<Image>();
            bgImg.color    = BarBG;

            // ── Fill (barra coloreada) ────────────────────────────────────────
            var fillGO     = new GameObject("Fill");
            fillGO.transform.SetParent(bgGO.transform, false);
            var fillRT     = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillImage      = fillGO.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.type  = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;

            // ── Brillo (highlight) ────────────────────────────────────────────
            var gloss      = new GameObject("Gloss");
            gloss.transform.SetParent(bgGO.transform, false);
            var glRT       = gloss.AddComponent<RectTransform>();
            glRT.anchorMin = new Vector2(0f, 0.55f);
            glRT.anchorMax = Vector2.one;
            glRT.offsetMin = Vector2.zero;
            glRT.offsetMax = Vector2.zero;
            var glImg      = gloss.AddComponent<Image>();
            glImg.color    = new Color(1f, 1f, 1f, 0.07f);

            // ── Label (nombre izquierda) ───────────────────────────────────────
            var lblGO      = new GameObject("Label");
            lblGO.transform.SetParent(bgGO.transform, false);
            var lblRT      = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(6f, 0f);
            lblRT.offsetMax = Vector2.zero;
            var lblTxt     = lblGO.AddComponent<Text>();
            lblTxt.text    = label;
            lblTxt.fontSize = 11;
            lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.color   = new Color(1f, 1f, 1f, 0.85f);
            lblTxt.alignment = TextAnchor.MiddleLeft;
            lblTxt.font    = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ── Porcentaje (derecha) ───────────────────────────────────────────
            var pctGO      = new GameObject("PercentText");
            pctGO.transform.SetParent(bgGO.transform, false);
            var pctRT      = pctGO.AddComponent<RectTransform>();
            pctRT.anchorMin = Vector2.zero;
            pctRT.anchorMax = Vector2.one;
            pctRT.offsetMin = Vector2.zero;
            pctRT.offsetMax = new Vector2(-6f, 0f);
            percentText    = pctGO.AddComponent<Text>();
            percentText.text = "100%";
            percentText.fontSize  = 11;
            percentText.fontStyle = FontStyle.Bold;
            percentText.color     = Color.white;
            percentText.alignment = TextAnchor.MiddleRight;
            percentText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
