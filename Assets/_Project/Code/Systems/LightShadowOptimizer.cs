using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FeedTheNight.Controllers;

namespace FeedTheNight.Systems
{
    [AddComponentMenu("FeedTheNight/Systems/Light Shadow Optimizer")]
    public class LightShadowOptimizer : MonoBehaviour
    {
        [Header("Optimization Settings")]
        [Tooltip("Distancia máxima al jugador para renderizar sombras.")]
        public float maxShadowDistance = 30f;
        
        [Tooltip("Intervalo en segundos para comprobar las distancias a las luces.")]
        public float updateInterval = 0.5f;

        [Header("Debug")]
        [SerializeField] private int activeShadowsCount;
        [SerializeField] private int optimizedLightsCount;

        private Transform _playerTransform;
        
        // Estructura para recordar el estado original de sombras de cada luz
        private struct LightShadowState
        {
            public Light light;
            public LightShadows originalShadows;
        }

        private List<LightShadowState> _optimizedLights = new List<LightShadowState>();

        private void Start()
        {
            // Buscar al jugador
            FindPlayer();

            // Buscar todas las luces en la escena
            Light[] allLights = FindObjectsOfType<Light>();
            
            foreach (var l in allLights)
            {
                // Ignorar la luz direccional (Sol/Luna) para que siempre tenga sombras globales
                if (l.type == LightType.Directional) continue;

                // Solo optimizar luces que originalmente tenían sombras habilitadas
                if (l.shadows != LightShadows.None)
                {
                    _optimizedLights.Add(new LightShadowState
                    {
                        light = l,
                        originalShadows = l.shadows
                    });
                }
            }

            optimizedLightsCount = _optimizedLights.Count;
            Debug.Log($"[LightShadowOptimizer] Inicializado. Optimizando {optimizedLightsCount} luces dinámicas.");

            // Iniciar la rutina de optimización periódica
            StartCoroutine(OptimizeShadowsRoutine());
        }

        private void FindPlayer()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }
            else
            {
                // Alternativa por tag si no encuentra PlayerController
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _playerTransform = playerObj.transform;
                }
            }
        }

        private IEnumerator OptimizeShadowsRoutine()
        {
            while (true)
            {
                if (_playerTransform == null)
                {
                    FindPlayer();
                    yield return new WaitForSeconds(1.0f);
                    continue;
                }

                Vector3 playerPos = _playerTransform.position;
                int currentActiveShadows = 0;

                // Recorrer las luces y activar/desactivar sombras según distancia
                for (int i = 0; i < _optimizedLights.Count; i++)
                {
                    var state = _optimizedLights[i];
                    if (state.light == null) continue; // Por si se destruyó alguna luz

                    float distance = Vector3.Distance(playerPos, state.light.transform.position);

                    if (distance <= maxShadowDistance)
                    {
                        state.light.shadows = state.originalShadows;
                        currentActiveShadows++;
                    }
                    else
                    {
                        state.light.shadows = LightShadows.None;
                    }
                }

                activeShadowsCount = currentActiveShadows;
                yield return new WaitForSeconds(updateInterval);
            }
        }
    }
}
