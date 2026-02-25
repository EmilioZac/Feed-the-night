using System;
using UnityEngine;

namespace FeedTheNight.Systems
{
    /// <summary>
    /// Sistema de Hambre del Ghoul (Feed the Night).
    ///
    /// Reglas numéricas (fuente: ROADMAP Fase 1.1):
    ///   Decaimiento pasivo  : -1% cada 20 seg
    ///   Correr              : -1% cada 10 seg (2× velocidad)
    ///   Habilidad de combate: -1% instantáneo (llamar UseCombatAbility())
    ///   Regen de vida       : -0.2%/seg mientras activa (llamar SetHealthRegen())
    ///
    ///   Alimentación base   : Civil +20% | Inv.Bajo +30% | Inv.Alto +40%
    ///   Diminishing returns : -0.1% acumulativo por comida
    ///
    ///   Frenzy              : hambre == 0 → override de controles
    /// </summary>
    [AddComponentMenu("FeedTheNight/Systems/Hunger System")]
    public class HungerSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Starting Values")]
        [Range(0f, 100f)] public float startHunger = 100f;

        [Header("Decay Rates  (% per second)")]
        [Tooltip("Decaimiento pasivo base: -1% / 10 seg = 0.10 %/s")]
        public float passiveDecayRate  = 0.10f;   // -1% cada 10 seg
        [Tooltip("Extra mientras corre: suma este valor al pasivo.")]
        public float runningExtraDecay = 0.10f;   // total en carrera = 0.20 %/s
        [Tooltip("Coste de regen de vida por segundo (DUPLICADO).")]
        public float healthRegenCost   = 0.40f;   // -0.4 %/s (antes 0.2)

        [Header("Energy Influence")]
        [Tooltip("Referencia para duplicar hambre si stamina < 60%.")]
        public EnergySystem energySystem;
        public float energyThreshold  = 60f;
        public float lowEnergyPenalty = 2.0f;

        [Header("Feeding Gains (%)")]
        public float gainCivil    = 20f;
        public float gainLowRank  = 30f;
        public float gainHighRank = 40f;
        [Tooltip("Reducción acumulativa por cada comida (-0.1% por ingesta).")]
        public float diminishingStep = 0.1f;

        [Header("Frenzy")]
        [Tooltip("Debajo de este valor se activa la alerta visual/sonora.")]
        [Range(0f, 50f)] public float lowHungerThreshold = 20f;

        [Header("Debug")]
        [SerializeField] [Range(0f, 100f)] private float _hunger;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Se lanza cada vez que cambia el nivel de hambre. Arg = valor 0-100.</summary>
        public event Action<float> OnHungerChanged;

        /// <summary>Se lanza cuando el hambre llega a 0 (estado Frenzy).</summary>
        public event Action OnFrenzyEntered;

        /// <summary>Se lanza cuando el hambre sube por encima de 0 saliendo del Frenzy.</summary>
        public event Action OnFrenzyExited;

        /// <summary>Se lanza cuando baja del umbral lowHungerThreshold.</summary>
        public event Action OnLowHunger;

        // ── State ─────────────────────────────────────────────────────────────
        private bool  _isRunning;
        private bool  _healthRegenActive;
        private bool  _isFrenzy;
        private float _diminishingAccumulator;   // total acumulado de diminishing returns

        // ── Properties ────────────────────────────────────────────────────────
        public float Hunger       => _hunger;
        public bool  IsFrenzy     => _isFrenzy;
        public bool  IsLowHunger  => _hunger <= lowHungerThreshold;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            // Forzamos el hambre al 100% al inicio para ignorar valores previos guardados
            _hunger = 100f;
            
            // Auto-asignar sistemas si están en el mismo objeto
            if (energySystem == null) energySystem = GetComponent<EnergySystem>();
        }

        private void Update()
        {
            ApplyDecay(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Notifica que el jugador está corriendo (aumenta el decaimiento).
        /// </summary>
        public void SetRunning(bool running) => _isRunning = running;

        /// <summary>
        /// Activa/desactiva la regeneración de vida (consume hambre extra).
        /// </summary>
        public void SetHealthRegen(bool active) => _healthRegenActive = active;

        /// <summary>
        /// Gasta 1% de hambre instantáneamente al usar una habilidad de combate.
        /// </summary>
        public void UseCombatAbility()
        {
            ModifyHunger(-1f);
        }

        /// <summary>
        /// Alimentarse de un NPC. Proporciona hambre según el tipo y aplica DR.
        /// </summary>
        public void Feed(NPCType npcType)
        {
            float baseGain = npcType switch
            {
                NPCType.Civil        => gainCivil,
                NPCType.LowRankInv   => gainLowRank,
                NPCType.HighRankInv  => gainHighRank,
                _                   => gainCivil
            };

            float actualGain = Mathf.Max(0f, baseGain - _diminishingAccumulator);
            _diminishingAccumulator += diminishingStep;

            ModifyHunger(actualGain);

            // Salir de Frenzy si habíamos entrado
            if (_isFrenzy && _hunger > 0f)
            {
                _isFrenzy = false;
                OnFrenzyExited?.Invoke();
            }
        }

        /// <summary>
        /// Añade/quita hambre manualmente (útil para debug o power-ups).
        /// </summary>
        public void ModifyHunger(float delta)
        {
            SetHunger(_hunger + delta);
        }

        // ── Internals ─────────────────────────────────────────────────────────
        private void ApplyDecay(float dt)
        {
            float decay = passiveDecayRate;

            if (_isRunning)          decay += runningExtraDecay;
            if (_healthRegenActive)  decay += healthRegenCost;

            // Multiplicador si la energía es baja (< 60%)
            if (energySystem != null && energySystem.Energy < (energySystem.MaxEnergy * 0.6f))
            {
                decay *= lowEnergyPenalty;
            }

            ModifyHunger(-decay * dt);
        }

        private void SetHunger(float value)
        {
            float prev   = _hunger;
            _hunger      = Mathf.Clamp(value, 0f, 100f);

            if (Mathf.Approximately(prev, _hunger)) return;

            OnHungerChanged?.Invoke(_hunger);

            // Frenzy
            if (_hunger <= 0f && !_isFrenzy)
            {
                _isFrenzy = true;
                OnFrenzyEntered?.Invoke();
            }

            // LowHunger warning
            if (!_isFrenzy && _hunger <= lowHungerThreshold && prev > lowHungerThreshold)
            {
                OnLowHunger?.Invoke();
            }
        }

        // ── Enum ──────────────────────────────────────────────────────────────
        public enum NPCType
        {
            Civil,
            LowRankInv,    // Investigador Rango Bajo
            HighRankInv    // Investigador Rango Alto
        }
    }
}
