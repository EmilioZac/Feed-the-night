using UnityEngine;
using StarterAssets;

namespace FeedTheNight.NPCs
{
    [AddComponentMenu("FeedTheNight/NPCs/Audio Detection")]
    [RequireComponent(typeof(VisionCone))]
    public class AudioDetection : MonoBehaviour
    {
        private VisionCone _visionCone;
        private Transform _playerTransform;
        private StarterAssetsInputs _playerInputs;
        private KaguneSpawner _playerKagune;

        [Header("Audio Settings")]
        [Tooltip("Velocidad de detección de audio (sospecha/segundo) cuando el jugador se mueve.")]
        public float audioDetectionSpeed = 30f;
        [Tooltip("Velocidad de enfriamiento base (sospecha/segundo) cuando el jugador va agachado o camuflado.")]
        public float audioCooldownSpeed = 20f;
        [Tooltip("Capas que obstruyen el sonido (Paredes, coberturas).")]
        public LayerMask obstacleMask;

        private void Awake()
        {
            _visionCone = GetComponent<VisionCone>();
        }

        private void Update()
        {
            // Intentar encontrar al jugador si no está asignado
            if (_playerInputs == null)
            {
                var inputs = FindObjectOfType<StarterAssetsInputs>();
                if (inputs != null)
                {
                    _playerInputs = inputs;
                    _playerTransform = inputs.transform;
                    _playerKagune = inputs.GetComponentInParent<KaguneSpawner>();
                    if (_playerKagune == null)
                    {
                        _playerKagune = inputs.GetComponentInChildren<KaguneSpawner>();
                    }
                }
            }

            if (_playerTransform == null || _playerInputs == null || _visionCone == null) return;

            // El rango de audio es la mitad del rango de visión
            float audioRange = _visionCone.viewDistance / 2f;
            
            // Medir la distancia al centro del jugador
            Vector3 targetPosition = _playerTransform.position + Vector3.up * 1.0f;
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance <= audioRange)
            {
                // Comprobar si hay obstáculos en medio
                bool hasObstacle = false;
                Vector3 directionToPlayer = (targetPosition - transform.position);
                RaycastHit hit;
                
                // Hacemos un raycast desde el NPC hasta el jugador usando la máscara de obstáculos
                if (Physics.Raycast(transform.position, directionToPlayer.normalized, out hit, distance, obstacleMask))
                {
                    hasObstacle = true;
                }

                // Determinar el comportamiento
                bool isCrouched = _playerInputs.crouch;
                bool isCamouflaged = _playerInputs.camouflage;
                bool isMoving = _playerInputs.move.sqrMagnitude > 0.01f;

                if (!isMoving || isCrouched || isCamouflaged)
                {
                    // Baja la sospecha si el jugador no se mueve (no hace ruido) o va agachado/camuflado
                    float currentSuspicion = _visionCone.SuspicionMeter;
                    _visionCone.SuspicionMeter = currentSuspicion - audioCooldownSpeed * Time.deltaTime;
                    
                    Debug.Log($"[AudioDetection - {gameObject.name}] Jugador sin hacer ruido o sigiloso en rango de audio ({distance:F2}m). Enfriando sospecha: {_visionCone.SuspicionMeter:F1}%");
                }
                else
                {
                    // Sube la sospecha si se mueve (hace ruido)
                    float detectionRate = audioDetectionSpeed;

                    // Si está en modo kagune, sube el doble
                    if (_playerKagune != null && _playerKagune.IsKaguneActive)
                    {
                        detectionRate *= 2f;
                    }

                    // Redúcelo a la cuarta parte (4 veces más lento) si hay un objeto de por medio
                    if (hasObstacle)
                    {
                        detectionRate *= 0.25f;
                    }

                    float currentSuspicion = _visionCone.SuspicionMeter;
                    _visionCone.SuspicionMeter = currentSuspicion + detectionRate * Time.deltaTime;

                    Debug.Log($"[AudioDetection - {gameObject.name}] Jugador HACIENDO RUIDO en rango de audio ({distance:F2}m). " +
                              $"Tasa: {detectionRate:F1}/s | Obstáculo: {(hasObstacle ? "SÍ" : "NO")} | Sospecha: {_visionCone.SuspicionMeter:F1}%");
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_visionCone != null)
            {
                // Dibujar el rango de audio (esfera amarilla de alambre)
                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.35f);
                Gizmos.DrawWireSphere(transform.position, _visionCone.viewDistance / 2f);
            }
        }
    }
}
