using UnityEngine;
using FeedTheNight.Systems;

namespace FeedTheNight.Environment
{
    /// <summary>
    /// Baja el hambre del jugador al 30% al entrar en el trigger.
    /// Tiene un cooldown de 20 segundos.
    /// </summary>
    public class HungerTrap : MonoBehaviour
    {
        [Header("Settings")]
        public float TargetHunger = 30f;
        public float Cooldown = 20f;

        private float _nextAllowedTime;

        private void OnTriggerEnter(Collider other)
        {
            // Solo procesar si ha pasado el cooldown
            if (Time.time < _nextAllowedTime)
            {
                Debug.Log($"[HungerTrap] En cooldown. Faltan {(_nextAllowedTime - Time.time):F1} segundos.");
                return;
            }

            // Buscar el sistema de hambre en el objeto que entró o sus padres
            HungerSystem hunger = other.GetComponentInParent<HungerSystem>();

            if (hunger != null)
            {
                if (hunger.Hunger > TargetHunger)
                {
                    float difference = hunger.Hunger - TargetHunger;
                    hunger.ModifyHunger(-difference);
                    
                    _nextAllowedTime = Time.time + Cooldown;
                    
                    Debug.Log($"[HungerTrap] ¡Activado! Hambre reducida a {TargetHunger}%. Próximo uso en {Cooldown}s.");
                }
                else
                {
                    Debug.Log($"[HungerTrap] El jugador ya tiene menos del {TargetHunger}% de hambre. No se aplica el efecto.");
                }
            }
        }
    }
}
