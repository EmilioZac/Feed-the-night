using UnityEngine;
using FeedTheNight.Controllers;

namespace FeedTheNight.NPCs
{
    [AddComponentMenu("FeedTheNight/NPCs/Vision Cone")]
    public class VisionCone : MonoBehaviour
    {
        [Header("Vision Settings")]
        [Tooltip("Campo de visión total en grados.")]
        public float viewAngle = 90f;
        [Tooltip("Distancia máxima de visión.")]
        public float viewDistance = 10f;
        [Tooltip("Capas que representan al objetivo (Jugador).")]
        public LayerMask targetMask;
        [Tooltip("Capas que obstruyen la visión (Paredes, coberturas).")]
        public LayerMask obstacleMask;
        [Tooltip("Frecuencia con la que se comprueba la visión (segundos).")]
        public float checkInterval = 0.1f;

        [Header("Detection Settings")]
        [Tooltip("Velocidad de detección (sospecha/segundo) cuando el jugador está pegado al NPC.")]
        public float detectionSpeedMax = 100f;
        [Tooltip("Velocidad de detección (sospecha/segundo) cuando el jugador está al límite del rango.")]
        public float detectionSpeedMin = 10f;
        [Tooltip("Velocidad de enfriamiento base (sospecha/segundo) cuando no se ve al jugador.")]
        public float cooldownSpeed = 50f;
        [Tooltip("Tiempo de espera en segundos antes de enfriar la sospecha tras haber detectado al jugador al 100%.")]
        public float detectionCooldownDelay = 15f;
        [Tooltip("Curva que define cómo decae la velocidad de detección con la distancia (x: 0 es cerca, 1 es lejos).")]
        public AnimationCurve detectionDistanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.1f);

        [Header("Debug/State")]
        [SerializeField] private float _suspicionMeter = 0f;
        [SerializeField] private bool _isPlayerDetected = false;
        [SerializeField] private float _cooldownDelayTimer = 0f;

        public float SuspicionMeter => _suspicionMeter;
        public bool IsPlayerDetected => _isPlayerDetected;

        // Eventos
        public System.Action<float> OnSuspicionChanged;
        public System.Action OnPlayerDetected;
        public System.Action OnPlayerLost;

        private Transform _playerTransform;
        private PlayerController _playerController;
        private SphereCollider _triggerCollider;

        private float _checkTimer;
        private bool _playerInTrigger;
        private bool _canSeePlayer;

        private void Awake()
        {
            // Aseguramos que tenemos el collider trigger
            _triggerCollider = GetComponent<SphereCollider>();
            if (_triggerCollider == null)
            {
                _triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = viewDistance;
        }

        private void Update()
        {
            // Sincronizar el radio del trigger con el viewDistance configurado
            if (_triggerCollider != null && _triggerCollider.radius != viewDistance)
            {
                _triggerCollider.radius = viewDistance;
            }

            _checkTimer += Time.deltaTime;

            // Determinar si el jugador está camuflado
            bool playerIsCamouflaged = _playerController != null && _playerController.currentState == PlayerController.State.Camouflage;

            if (_checkTimer >= checkInterval)
            {
                _checkTimer = 0f;
                // Si el jugador está camuflado, no podemos verlo bajo ninguna circunstancia
                if (_playerInTrigger && _playerTransform != null && !playerIsCamouflaged)
                {
                    _canSeePlayer = CheckVision();

                    // Lógica de Logging
                    Vector3 targetPosition = _playerTransform.position + Vector3.up * 1.0f;
                    float distance = Vector3.Distance(transform.position, targetPosition);
                    Vector3 directionToPlayer = (targetPosition - transform.position);
                    float angle = Vector3.Angle(transform.forward, directionToPlayer);
                    string obstacleInfo = "Ninguno (Despejado)";

                    if (angle < viewAngle / 2f)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(transform.position, directionToPlayer.normalized, out hit, directionToPlayer.magnitude, obstacleMask | targetMask))
                        {
                            if (((1 << hit.collider.gameObject.layer) & targetMask) == 0)
                            {
                                obstacleInfo = $"Bloqueado por '{hit.collider.gameObject.name}'";
                            }
                        }
                        else
                        {
                            obstacleInfo = "No hay impacto (Fuera de rango o capa incorrecta)";
                        }
                    }
                    else
                    {
                        obstacleInfo = "Fuera del ángulo visual";
                    }

                    Debug.Log($"[VisionCone - {gameObject.name}] " +
                              $"Sospecha: {_suspicionMeter:F1}% | " +
                              $"Distancia: {distance:F2}m | " +
                              $"Obstáculo: {obstacleInfo} | " +
                              $"¿Detectado?: {(_isPlayerDetected ? "SÍ" : "NO")} | " +
                              $"Espera CD: {_cooldownDelayTimer:F1}s");
                }
                else
                {
                    _canSeePlayer = false;

                    // Si el jugador está en el trigger pero camuflado, logearlo ocasionalmente
                    if (_playerInTrigger && playerIsCamouflaged)
                    {
                        Debug.Log($"[VisionCone - {gameObject.name}] Jugador CAMUFLADO en rango. Sospecha: {_suspicionMeter:F1}% | ¿Detectado?: {(_isPlayerDetected ? "SÍ" : "NO")}");
                    }
                }
            }

            // Manejo del medidor de sospecha
            if (_canSeePlayer)
            {
                // Si volvemos a ver al jugador, cancelamos la cuenta regresiva del cooldown
                _cooldownDelayTimer = 0f;

                float camoMultiplier = 1.0f;
                if (_playerController != null)
                {
                    if (_playerController.currentState == PlayerController.State.Crouch)
                    {
                        camoMultiplier = 0.5f; // Mitad de velocidad si está agachado
                    }
                }

                float distance = Vector3.Distance(transform.position, _playerTransform.position + Vector3.up * 1.0f);
                float normalizedDistance = Mathf.Clamp01(distance / viewDistance);
                
                // Evaluar la velocidad en la curva
                float curveFactor = detectionDistanceCurve.Evaluate(normalizedDistance);
                float detectionRate = Mathf.Lerp(detectionSpeedMin, detectionSpeedMax, curveFactor) * camoMultiplier;

                _suspicionMeter = Mathf.Min(100f, _suspicionMeter + detectionRate * Time.deltaTime);
            }
            else
            {
                // Si el jugador ya fue detectado al 100%
                if (_isPlayerDetected)
                {
                    // Si el retraso de enfriamiento está activo, incrementar el temporizador
                    if (_cooldownDelayTimer < detectionCooldownDelay)
                    {
                        _cooldownDelayTimer += Time.deltaTime;
                    }
                }

                // Solo enfriamos sospecha si no estamos en el delay de 15s (o si no se había llegado al 100%)
                if (!_isPlayerDetected || _cooldownDelayTimer >= detectionCooldownDelay)
                {
                    // Cuanto más alto sea el medidor de sospecha, más lento baja
                    float suspicionFactor = Mathf.Clamp01(1f - (_suspicionMeter / 100f));
                    // Mínimo 10% de velocidad para que no se detenga completamente cerca del 100%
                    float cooldownFactor = Mathf.Max(0.1f, suspicionFactor);
                    
                    _suspicionMeter = Mathf.Max(0f, _suspicionMeter - cooldownSpeed * cooldownFactor * Time.deltaTime);
                }
            }

            OnSuspicionChanged?.Invoke(_suspicionMeter);

            // Cambios de estado de detección
            if (!_isPlayerDetected && _suspicionMeter >= 100f)
            {
                _isPlayerDetected = true;
                _cooldownDelayTimer = 0f;
                OnPlayerDetected?.Invoke();
                Debug.Log($"[VisionCone] JUGADOR DETECTADO por {gameObject.name}");
            }
            else if (_isPlayerDetected && _suspicionMeter <= 0f)
            {
                _isPlayerDetected = false;
                _cooldownDelayTimer = 0f;
                OnPlayerLost?.Invoke();
                Debug.Log($"[VisionCone] Jugador perdido de vista por {gameObject.name}");
            }
        }

        private bool CheckVision()
        {
            // Apuntamos al "centro/pecho" del jugador (1 metro arriba de su pivot de pies) para evitar chocar con el suelo
            Vector3 targetPosition = _playerTransform.position + Vector3.up * 1.0f;
            Vector3 directionToPlayer = (targetPosition - transform.position);
            
            // Ángulo entre la orientación del NPC (transform.forward) y la dirección al jugador
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle < viewAngle / 2f)
            {
                // Hacer Raycast para comprobar si hay obstáculos en la línea de visión
                float distance = directionToPlayer.magnitude;
                RaycastHit hit;

                // Buscamos colisiones en las capas de obstáculo y de jugador
                if (Physics.Raycast(transform.position, directionToPlayer.normalized, out hit, distance, obstacleMask | targetMask))
                {
                    // Si el raycast choca primero con algo en la capa targetMask, es visible
                    if (((1 << hit.collider.gameObject.layer) & targetMask) != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & targetMask) != 0)
            {
                _playerTransform = other.transform;
                
                // Intento robusto de encontrar PlayerController en el objeto, hijos o padres
                _playerController = other.GetComponentInParent<PlayerController>();
                if (_playerController == null)
                {
                    _playerController = other.GetComponentInChildren<PlayerController>();
                }
                if (_playerController == null)
                {
                    _playerController = FindObjectOfType<PlayerController>();
                }
                
                _playerInTrigger = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & targetMask) != 0)
            {
                _playerInTrigger = false;
            }
        }

        private void OnDrawGizmos()
        {
            // Dibujar el rango de visión en el editor
            Gizmos.color = _isPlayerDetected ? Color.red : new Color(1f, 0.5f, 0f, 0.3f);
            
            // Dibujar el arco de visión
            Vector3 leftLimit = RotateVectorAroundY(transform.forward, -viewAngle / 2f);
            Vector3 rightLimit = RotateVectorAroundY(transform.forward, viewAngle / 2f);

            Gizmos.DrawRay(transform.position, leftLimit * viewDistance);
            Gizmos.DrawRay(transform.position, rightLimit * viewDistance);

            // Dibujar una línea hacia el jugador si está dentro del cono
            if (_playerTransform != null && _playerInTrigger)
            {
                Gizmos.color = _canSeePlayer ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, _playerTransform.position);
            }
        }

        private Vector3 RotateVectorAroundY(Vector3 vector, float angleDegrees)
        {
            return Quaternion.Euler(0, angleDegrees, 0) * vector;
        }
    }
}
