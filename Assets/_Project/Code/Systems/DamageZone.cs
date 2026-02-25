using UnityEngine;
using System.Collections.Generic;

namespace FeedTheNight.Systems
{
    /// <summary>
    /// Zona que inflige daño periódico a cualquier objeto con HealthSystem.
    /// Requisito: BoxCollider con IsTrigger = true.
    /// </summary>
    [AddComponentMenu("FeedTheNight/Systems/Damage Zone")]
    [RequireComponent(typeof(BoxCollider))]
    public class DamageZone : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Porcentaje de la vida máxima a quitar (0.05 = 5%).")]
        public float damagePercent = 0.05f;
        [Tooltip("Intervalo entre cada pulso de daño en segundos.")]
        public float interval = 2.0f;

        // Diccionario para rastrear el tiempo de cada entidad dentro de la zona
        private Dictionary<HealthSystem, float> _entitiesInRange = new Dictionary<HealthSystem, float>();

        private void OnTriggerEnter(Collider other)
        {
            var health = other.GetComponentInParent<HealthSystem>();
            if (health != null && !_entitiesInRange.ContainsKey(health))
            {
                // El primer golpe ocurre inmediatamente al entrar (opcional) o tras el intervalo
                _entitiesInRange.Add(health, 0f); 
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var health = other.GetComponentInParent<HealthSystem>();
            if (health != null && _entitiesInRange.ContainsKey(health))
            {
                _entitiesInRange.Remove(health);
            }
        }

        private void Update()
        {
            // Necesitamos una lista temporal para evitar modificar el diccionario mientras iteramos
            List<HealthSystem> toRemove = null;

            // Procesar daño para todos los que estén dentro
            // Usamos una copia de las llaves para poder iterar
            var keys = new List<HealthSystem>(_entitiesInRange.Keys);
            
            foreach (var health in keys)
            {
                if (health == null || !health.IsAlive)
                {
                    if (toRemove == null) toRemove = new List<HealthSystem>();
                    toRemove.Add(health);
                    continue;
                }

                _entitiesInRange[health] += Time.deltaTime;

                if (_entitiesInRange[health] >= interval)
                {
                    float damageAmount = health.MaxHealth * damagePercent;
                    health.TakeDamage(damageAmount);
                    
                    // Reiniciar timer para esta entidad
                    _entitiesInRange[health] = 0f;
                    Debug.Log($"[DamageZone] {health.gameObject.name} recibió {damageAmount} de daño.");
                }
            }

            if (toRemove != null)
            {
                foreach (var r in toRemove) _entitiesInRange.Remove(r);
            }
        }
    }
}
