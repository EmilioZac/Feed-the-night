using System;
using UnityEngine;

namespace FeedTheNight.Systems
{
    /// <summary>
    /// Sistema de Energía del Ghoul.
    ///
    /// Acoplamiento con HungerSystem:
    ///   - Correr gasta energía Y activa el decaimiento de hambre extra.
    ///   - Si energía == 0, el jugador no puede correr (notifica HungerSystem).
    ///
    /// Llama a SetRunning(true) y SetJumping(true) desde el PlayerController.
    /// </summary>
    [AddComponentMenu("FeedTheNight/Systems/Energy System")]
    public class EnergySystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Stats")]
        public float maxEnergy = 100f;
        [SerializeField] private float _energy;

        [Header("Drain Rates  (units / second)")]
        public float runDrainRate  = 4f;    // antes 8
        public float jumpDrainFlat = 5f;    // antes 10

        [Header("Regen")]
        [Tooltip("Regen pasivo de energía cuando no se corre ni salta.")]
        public float regenRate     = 2.5f;  // antes 5 (recupera el doble de lento)
        [Tooltip("Segundos sin correr antes de que empiece la regen.")]
        public float regenDelay    = 1.5f;

        [Header("References")]
        public HungerSystem hungerSystem;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Arg: energía actual (0–maxEnergy).</summary>
        public event Action<float> OnEnergyChanged;
        /// <summary>La energía llegó a 0 (sin estamina).</summary>
        public event Action         OnEnergyDepleted;
        /// <summary>La energía superó 0 tras haber estado en 0.</summary>
        public event Action         OnEnergyRestored;

        // ── Properties ────────────────────────────────────────────────────────
        public float Energy    => _energy;
        public float MaxEnergy => maxEnergy;
        /// <summary>True si el jugador puede seguir corriendo.</summary>
        public bool  CanRun    => _energy > 0f;

        // ── Private ───────────────────────────────────────────────────────────
        private bool  _isRunning;
        private bool  _wasDepletedLastFrame;
        private float _regenDelayTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _energy = maxEnergy;
        }

        private void Update()
        {
            HandleDrainAndRegen();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Llama desde PlayerController cada frame con el estado de carrera.
        /// Si no hay energía, fuerza running=false hacia el HungerSystem.
        /// </summary>
        public void SetRunning(bool running)
        {
            // No puede correr sin energía
            _isRunning = running && CanRun;

            // Notificar al HungerSystem el estado real de carrera
            if (hungerSystem != null)
                hungerSystem.SetRunning(_isRunning);
        }

        /// <summary>Gasta energía de forma plana al saltar.</summary>
        public void OnJump()
        {
            if (_energy <= 0f) return;
            ModifyEnergy(-jumpDrainFlat);
        }

        // ── Internals ─────────────────────────────────────────────────────────
        private void HandleDrainAndRegen()
        {
            if (_isRunning)
            {
                _regenDelayTimer = regenDelay;   // reinicia el delay
                ModifyEnergy(-runDrainRate * Time.deltaTime);
            }
            else
            {
                // Regen con delay
                if (_regenDelayTimer > 0f)
                {
                    _regenDelayTimer -= Time.deltaTime;
                }
                else if (_energy < maxEnergy)
                {
                    ModifyEnergy(regenRate * Time.deltaTime);
                }
            }
        }

        public void ModifyEnergy(float delta)
        {
            float prev = _energy;
            _energy = Mathf.Clamp(_energy + delta, 0f, maxEnergy);

            if (Mathf.Approximately(prev, _energy)) return;

            OnEnergyChanged?.Invoke(_energy);

            // Depletion event
            if (_energy <= 0f && !_wasDepletedLastFrame)
            {
                _wasDepletedLastFrame = true;
                _isRunning = false;
                if (hungerSystem != null) hungerSystem.SetRunning(false);
                OnEnergyDepleted?.Invoke();
            }
            else if (_energy > 0f && _wasDepletedLastFrame)
            {
                _wasDepletedLastFrame = false;
                OnEnergyRestored?.Invoke();
            }
        }
    }
}
