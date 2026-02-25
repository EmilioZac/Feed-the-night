using System;
using UnityEngine;

namespace FeedTheNight.Systems
{
    /// <summary>
    /// Sistema de Vida del Ghoul.
    ///
    /// Acoplamiento con HungerSystem:
    ///   - Si HP < Max y hambre > 0 → regenera +1 HP/seg
    ///   - El coste de la regen (−0.2%/seg) lo gestiona HungerSystem
    ///     a través de SetHealthRegen().
    /// </summary>
    [AddComponentMenu("FeedTheNight/Systems/Health System")]
    public class HealthSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Stats")]
        public float maxHealth     = 100f;
        [SerializeField] private float _health;

        [Header("Regeneration")]
        [Tooltip("+HP por segundo cuando hambre > 0 y vida < máximo.")]
        public float regenRate     = 1f;     // +1 HP/s (ROADMAP)
        [Tooltip("Umbral mínimo de hambre para que arranque la regen.")]
        public float regenHungerMin = 5f;

        [Header("References")]
        [Tooltip("Referencia al HungerSystem del mismo jugador.")]
        public HungerSystem hungerSystem;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Arg: HP actual (0–maxHealth).</summary>
        public event Action<float> OnHealthChanged;
        /// <summary>Se lanza cuando HP llega a 0.</summary>
        public event Action         OnDeath;
        /// <summary>Arg: cantidad de daño recibido.</summary>
        public event Action<float>  OnDamaged;

        // ── Properties ────────────────────────────────────────────────────────
        public float Health    => _health;
        public float MaxHealth => maxHealth;
        public bool  IsAlive   => _health > 0f;
        public bool  IsAtMax   => Mathf.Approximately(_health, maxHealth);

        // ── Private ───────────────────────────────────────────────────────────
        private bool _isDead;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            _health = maxHealth;
        }

        private void Update()
        {
            HandleRegen();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Aplica daño al jugador.</summary>
        public void TakeDamage(float amount)
        {
            if (_isDead || amount <= 0f) return;
            SetHealth(_health - amount);
            OnDamaged?.Invoke(amount);
        }

        /// <summary>Cura al jugador (sin superar maxHealth).</summary>
        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f) return;
            SetHealth(_health + amount);
        }

        /// <summary>Mata al jugador instantáneamente.</summary>
        public void Kill() => SetHealth(0f);

        // ── Internals ─────────────────────────────────────────────────────────
        private void HandleRegen()
        {
            if (_isDead || IsAtMax) { StopRegen(); return; }

            bool canRegen = hungerSystem != null
                         && hungerSystem.Hunger > regenHungerMin;

            if (canRegen)
            {
                hungerSystem.SetHealthRegen(true);
                SetHealth(_health + regenRate * Time.deltaTime);
            }
            else
            {
                StopRegen();
            }
        }

        private void StopRegen()
        {
            if (hungerSystem != null)
                hungerSystem.SetHealthRegen(false);
        }

        private void SetHealth(float value)
        {
            float prev = _health;
            _health = Mathf.Clamp(value, 0f, maxHealth);

            if (Mathf.Approximately(prev, _health)) return;

            OnHealthChanged?.Invoke(_health);

            if (_health <= 0f && !_isDead)
            {
                _isDead = true;
                StopRegen();
                OnDeath?.Invoke();
            }
        }
    }
}
