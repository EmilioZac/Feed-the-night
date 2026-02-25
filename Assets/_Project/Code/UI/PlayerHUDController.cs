using UnityEngine;
using UnityEngine.UI;
using FeedTheNight.Systems;

namespace FeedTheNight.UI
{
    /// <summary>
    /// Controlador runtime del HUD.
    /// Las referencias se asignan automáticamente por PlayerHUDBuilder al crear el HUD.
    /// Sólo tienes que arrastrar los tres sistemas (HealthSystem, HungerSystem, EnergySystem)
    /// del Player al Inspector de este componente.
    /// </summary>
    [AddComponentMenu("FeedTheNight/UI/Player HUD Controller")]
    public class PlayerHUDController : MonoBehaviour
    {
        // ── References assigned by PlayerHUDBuilder ───────────────────────────
        [Header("Bar Images  (auto-asignadas por el Builder)")]
        [SerializeField] internal Image healthFill;
        [SerializeField] internal Image hungerFill;
        [SerializeField] internal Image energyFill;

        [Header("Percent Texts  (auto-asignadas por el Builder)")]
        [SerializeField] internal Text  healthText;
        [SerializeField] internal Text  hungerText;
        [SerializeField] internal Text  energyText;

        // ── Player systems  (arrastrar desde el Player en el Inspector) ────────
        [Header("Systems  ← arrastrar desde el Player")]
        public HealthSystem healthSystem;
        public HungerSystem hungerSystem;
        public EnergySystem energySystem;

        // ── Colores extra para estado bajo ────────────────────────────────────
        [Header("Low-state Colors")]
        public Color hungerLowColor  = new Color(0.85f, 0.20f, 0.08f);   // rojo fuerte = frenzy warning
        public Color energyLowColor  = new Color(0.55f, 0.55f, 0.55f);   // gris = sin estamina
        readonly Color _hungerNormal = new Color(0.93f, 0.52f, 0.10f);
        readonly Color _energyNormal = new Color(0.22f, 0.58f, 0.95f);

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void OnEnable()
        {
            Subscribe();
            // Refrescar en el siguiente frame para asegurar que los sistemas despertaron
            Invoke(nameof(RefreshAll), 0.1f);
        }

        private void Start()
        {
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // ── Events ────────────────────────────────────────────────────────────
        private void Subscribe()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += SetHealth;
            }
            if (hungerSystem != null)
            {
                hungerSystem.OnHungerChanged += SetHunger;
                hungerSystem.OnLowHunger     += OnLowHunger;
                hungerSystem.OnFrenzyEntered += OnFrenzy;
                hungerSystem.OnFrenzyExited  += OnFrenzyOut;
            }
            if (energySystem != null)
            {
                energySystem.OnEnergyChanged   += SetEnergy;
                energySystem.OnEnergyDepleted  += OnEnergyDepleted;
                energySystem.OnEnergyRestored  += OnEnergyRestored;
            }
        }

        private void Unsubscribe()
        {
            if (healthSystem != null) healthSystem.OnHealthChanged -= SetHealth;
            if (hungerSystem != null)
            {
                hungerSystem.OnHungerChanged -= SetHunger;
                hungerSystem.OnLowHunger     -= OnLowHunger;
                hungerSystem.OnFrenzyEntered -= OnFrenzy;
                hungerSystem.OnFrenzyExited  -= OnFrenzyOut;
            }
            if (energySystem != null)
            {
                energySystem.OnEnergyChanged  -= SetEnergy;
                energySystem.OnEnergyDepleted -= OnEnergyDepleted;
                energySystem.OnEnergyRestored -= OnEnergyRestored;
            }
        }

        // ── Handlers ──────────────────────────────────────────────────────────
        private void SetHealth(float val)
        {
            if (healthFill == null || healthSystem == null) return;
            float t = val / healthSystem.MaxHealth;
            healthFill.fillAmount = t;
            SetText(healthText, t);
        }

        private void SetHunger(float val)
        {
            if (hungerFill == null) return;
            float t = val / 100f;
            hungerFill.fillAmount = t;
            SetText(hungerText, t);
        }

        private void SetEnergy(float val)
        {
            if (energyFill == null || energySystem == null) return;
            float t = val / energySystem.MaxEnergy;
            energyFill.fillAmount = t;
            SetText(energyText, t);
        }

        private void OnLowHunger()
        {
            if (hungerFill != null) hungerFill.color = hungerLowColor;
        }

        private void OnFrenzy()
        {
            if (hungerFill != null) hungerFill.color = Color.red;
        }

        private void OnFrenzyOut()
        {
            if (hungerFill != null) hungerFill.color = _hungerNormal;
        }

        private void OnEnergyDepleted()
        {
            if (energyFill != null) energyFill.color = energyLowColor;
        }

        private void OnEnergyRestored()
        {
            if (energyFill != null) energyFill.color = _energyNormal;
        }

        // ── Initial refresh ───────────────────────────────────────────────────
        private void RefreshAll()
        {
            if (healthSystem != null) SetHealth(healthSystem.Health);
            if (hungerSystem != null) SetHunger(hungerSystem.Hunger);
            if (energySystem != null) SetEnergy(energySystem.Energy);
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private static void SetText(Text t, float ratio)
        {
            if (t != null) t.text = Mathf.RoundToInt(ratio * 100f) + "%";
        }
    }
}
