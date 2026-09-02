using UnityEngine;
using StarterAssets;
using System;

namespace FeedTheNight.NPCs
{
    [AddComponentMenu("FeedTheNight/NPCs/Vision Cone Guard")]
    public class VisionConeGuard : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        //  CONFIGURACIÓN DE VISIÓN
        // ──────────────────────────────────────────────
        [Header("Vision Settings")]
        [Tooltip("Campo de visión total en grados (110° - 120° es natural para visión periférica).")]
        public float viewAngle = 120f;
        [Tooltip("Distancia máxima de visión en metros.")]
        public float viewDistance = 18f;
        [Tooltip("Altura de los ojos del NPC respecto a sus pies (en metros).")]
        public float eyeHeight = 1.6f;
        [Tooltip("Capas que representan al objetivo (Jugador).")]
        public LayerMask targetMask;
        [Tooltip("Capas que obstruyen la visión (Paredes, coberturas).")]
        public LayerMask obstacleMask;
        [Tooltip("Tiempo de gracia de visión en segundos: retiene la detección brevemente si el jugador cruza rápido el borde del cono.")]
        public float visionMemoryDuration = 0.4f;

        // ──────────────────────────────────────────────
        //  CONFIGURACIÓN DE AUDIO
        // ──────────────────────────────────────────────
        [Header("Audio Settings")]
        [Tooltip("Radio de detección auditiva. Si es 0, se usa la mitad de viewDistance.")]
        public float audioRange = 0f;
        [Tooltip("Velocidad de detección auditiva (sospecha/segundo) cuando el jugador hace ruido.")]
        public float audioDetectionSpeed = 35f;
        [Tooltip("Multiplicador de ruido cuando el jugador sprinta (más rápido = más ruido).")]
        public float sprintNoiseMultiplier = 1.5f;
        [Tooltip("Multiplicador de ruido cuando el jugador va agachado corriendo.")]
        public float crouchRunNoiseMultiplier = 0.5f;
        [Tooltip("Multiplicador de detección auditiva si hay un obstáculo entre el NPC y el jugador.")]
        public float audioObstacleMultiplier = 0.25f;

        // ──────────────────────────────────────────────
        //  CONFIGURACIÓN DE DETECCIÓN (SOSPECHA)
        // ──────────────────────────────────────────────
        [Header("Detection Settings")]
        [Tooltip("Velocidad de detección visual (sospecha/segundo) cuando el jugador está muy cerca.")]
        public float detectionSpeedMax = 45f;
        [Tooltip("Velocidad de detección visual (sospecha/segundo) cuando el jugador está al límite del rango.")]
        public float detectionSpeedMin = 12f;
        [Tooltip("Velocidad de enfriamiento (sospecha/segundo) cuando no se detecta al jugador.")]
        public float cooldownSpeed = 30f;
        [Tooltip("Segundos de espera antes de enfriar la sospecha tras haber detectado al jugador al 100%.")]
        public float detectionCooldownDelay = 10f;
        [Tooltip("Curva que define cómo decae la velocidad de detección visual con la distancia (x=0 cerca, x=1 lejos).")]
        public AnimationCurve detectionDistanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
        [Tooltip("Multiplicador de detección visual cuando el jugador está agachado.")]
        public float crouchDetectionMultiplier = 0.5f;
        [Tooltip("Multiplicador de detección (visual y audio) cuando el Kagune del jugador está activo.")]
        public float kaguneDetectionMultiplier = 2.0f;

        // ──────────────────────────────────────────────
        //  MÁQUINA DE ESTADOS
        // ──────────────────────────────────────────────
        [Header("State Machine")]
        [SerializeField] private NPCState _currentState = NPCState.Idle;
        [SerializeField] private DetectionSource _currentDetectionSource = DetectionSource.None;

        // ──────────────────────────────────────────────
        //  DEBUG / ESTADO INTERNO
        // ──────────────────────────────────────────────
        [Header("Debug Logs")]
        [Tooltip("Activar o desactivar logs en consola.")]
        public bool showDebugLogs = true;
        [Tooltip("Intervalo en segundos para refrescar el log cuando el jugador está en rango.")]
        public float logInterval = 0.5f;

        [Header("Debug/State")]
        [SerializeField] private float _suspicionMeter = 0f;
        [SerializeField] private float _cooldownDelayTimer = 0f;

        // ──────────────────────────────────────────────
        //  PROPIEDADES PÚBLICAS
        // ──────────────────────────────────────────────

        /// <summary>Estado actual del NPC (máquina de estados única).</summary>
        public NPCState CurrentState => _currentState;

        /// <summary>Fuente de detección activa en el frame actual.</summary>
        public DetectionSource CurrentDetectionSource => _currentDetectionSource;

        /// <summary>Porcentaje de sospecha actual (0-100).</summary>
        public float SuspicionMeter => _suspicionMeter;

        /// <summary>True si el estado actual es Detected o Chasing.</summary>
        public bool IsPlayerDetected => _currentState == NPCState.Detected || _currentState == NPCState.Chasing;

        /// <summary>True si la fuente de detección incluye visión.</summary>
        public bool CanSeePlayer => _currentDetectionSource == DetectionSource.Vision || _currentDetectionSource == DetectionSource.VisionAndAudio;

        /// <summary>True si la fuente de detección incluye audio.</summary>
        public bool CanHearPlayer => _currentDetectionSource == DetectionSource.Audio || _currentDetectionSource == DetectionSource.VisionAndAudio;

        // ──────────────────────────────────────────────
        //  EVENTOS
        // ──────────────────────────────────────────────
        /// <summary>Se dispara cuando la sospecha llega al 100% (transición a Detected).</summary>
        public Action OnPlayerDetected;
        /// <summary>Se dispara cuando la sospecha vuelve al 0% (transición a Idle).</summary>
        public Action OnPlayerLost;
        /// <summary>Se dispara en cada transición de estado con (estadoAnterior, estadoNuevo).</summary>
        public Action<NPCState, NPCState> OnStateChanged;

        // ──────────────────────────────────────────────
        //  REFERENCIAS INTERNAS
        // ──────────────────────────────────────────────
        private Transform _playerTransform;
        private StarterAssetsInputs _playerInputs;
        private KaguneSpawner _playerKagune;
        private SphereCollider _triggerCollider;

        private bool _playerInTrigger;
        private float _lastLoggedSuspicion = -1f;
        private NPCState _lastLoggedState = NPCState.Idle;
        private float _logTimer = 0f;
        private float _lastSeenTimer = 0f;

        // ══════════════════════════════════════════════
        //  UNITY LIFECYCLE
        // ══════════════════════════════════════════════

        private void Awake()
        {
            // Crear o configurar el trigger esférico para detección de rango
            _triggerCollider = GetComponent<SphereCollider>();
            if (_triggerCollider == null)
            {
                _triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = viewDistance;
        }

        private void Start()
        {
            // Búsqueda automática del jugador como respaldo
            FindPlayerReferences();

            Debug.Log($"<color=cyan><b>[VisionCone - {gameObject.name}]</b></color> Inicializado. " +
                      $"Rango Visión: {viewDistance}m | Ángulo: {viewAngle}° | Audio: {(audioRange > 0 ? audioRange : viewDistance / 2f)}m | " +
                      $"Jugador encontrado: {(_playerTransform != null ? _playerTransform.name : "<color=red>NO ENCONTRADO</color>")}");
        }

        private void FindPlayerReferences()
        {
            if (_playerTransform == null)
            {
                var inputs = FindObjectOfType<StarterAssetsInputs>();
                if (inputs != null)
                {
                    _playerInputs = inputs;
                    _playerTransform = inputs.transform;
                }
                else
                {
                    var playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null)
                    {
                        _playerTransform = playerObj.transform;
                        _playerInputs = playerObj.GetComponentInChildren<StarterAssetsInputs>();
                    }
                }
            }

            if (_playerKagune == null && _playerTransform != null)
            {
                _playerKagune = _playerTransform.GetComponentInChildren<KaguneSpawner>();
                if (_playerKagune == null) _playerKagune = FindObjectOfType<KaguneSpawner>();
            }
        }

        private void Update()
        {
            // Si por alguna razón no tenemos referencia al jugador, buscarlo
            if (_playerTransform == null)
            {
                FindPlayerReferences();
            }

            // Sincronizar radio del trigger con viewDistance
            if (_triggerCollider != null && !Mathf.Approximately(_triggerCollider.radius, viewDistance))
            {
                _triggerCollider.radius = viewDistance;
            }

            // Calcular distancia directa al jugador para fallback si el trigger falla
            float distanceToPlayer = _playerTransform != null ? Vector3.Distance(transform.position, _playerTransform.position) : 999f;
            bool isPlayerInRange = _playerInTrigger || (distanceToPlayer <= viewDistance);

            // Calcular el rango de audio efectivo
            float effectiveAudioRange = audioRange > 0f ? audioRange : viewDistance / 2f;

            // ─── PASO 1: Determinar si el jugador es detectable ───
            bool playerIsCamouflaged = _playerInputs != null && _playerInputs.camouflage;

            // Resetear fuente de detección de este frame
            bool seePlayer = false;
            bool hearPlayer = false;

            if (isPlayerInRange && _playerTransform != null && !playerIsCamouflaged)
            {
                // ─── PASO 2: Chequeo de VISIÓN (cono frontal) ───
                seePlayer = CheckVision();

                if (seePlayer)
                {
                    _lastSeenTimer = visionMemoryDuration;
                }
                else if (_lastSeenTimer > 0f)
                {
                    _lastSeenTimer -= Time.deltaTime;
                    seePlayer = true; // Mantener detección activa durante el tiempo de gracia
                }

                // ─── PASO 3: Chequeo de AUDIO (esfera alrededor) ───
                hearPlayer = CheckAudio(effectiveAudioRange);
            }
            else
            {
                _lastSeenTimer = 0f;
            }

            // Actualizar fuente de detección como enum único
            if (seePlayer && hearPlayer)
                _currentDetectionSource = DetectionSource.VisionAndAudio;
            else if (seePlayer)
                _currentDetectionSource = DetectionSource.Vision;
            else if (hearPlayer)
                _currentDetectionSource = DetectionSource.Audio;
            else
                _currentDetectionSource = DetectionSource.None;

            // ─── PASO 4: Actualizar el medidor de sospecha ───
            UpdateSuspicionMeter(effectiveAudioRange, seePlayer, hearPlayer);

            // ─── PASO 5: Actualizar máquina de estados ───
            UpdateStateMachine(playerIsCamouflaged);

            // ─── PASO 6: Log detallado de sospecha ───
            LogSuspicion(playerIsCamouflaged, isPlayerInRange, distanceToPlayer);
        }

        // ══════════════════════════════════════════════
        //  MÁQUINA DE ESTADOS - TRANSICIONES
        // ══════════════════════════════════════════════

        private void UpdateStateMachine(bool playerIsCamouflaged)
        {
            NPCState previousState = _currentState;

            switch (_currentState)
            {
                case NPCState.Idle:
                    if (_suspicionMeter > 0f && _currentDetectionSource != DetectionSource.None)
                        TransitionTo(NPCState.Suspicious);
                    break;

                case NPCState.Suspicious:
                    if (_suspicionMeter >= 100f)
                        TransitionTo(NPCState.Detected);
                    else if (_suspicionMeter <= 0f)
                        TransitionTo(NPCState.Idle);
                    break;

                case NPCState.Detected:
                    // El NPCController se encarga de la transición a Chasing tras el grito
                    if (_suspicionMeter <= 0f)
                        TransitionTo(NPCState.Idle);
                    break;

                case NPCState.Chasing:
                    if (_suspicionMeter <= 0f)
                        TransitionTo(NPCState.Returning);
                    break;

                case NPCState.Returning:
                    if (_suspicionMeter > 0f && _currentDetectionSource != DetectionSource.None)
                        TransitionTo(NPCState.Suspicious);
                    else if (_suspicionMeter <= 0f)
                        TransitionTo(NPCState.Idle);
                    break;
            }
        }

        private void TransitionTo(NPCState newState)
        {
            if (_currentState == newState) return;

            NPCState previousState = _currentState;
            _currentState = newState;

            Debug.Log($"<color=white><b>[StateMachine - {gameObject.name}]</b></color> " +
                      $"<color=yellow>{previousState}</color> → <color=cyan>{newState}</color> " +
                      $"| Sospecha: <b>{_suspicionMeter:F1}%</b>");

            // Disparar evento genérico de cambio de estado
            OnStateChanged?.Invoke(previousState, newState);

            // Disparar eventos específicos de compatibilidad
            if (newState == NPCState.Detected)
            {
                _cooldownDelayTimer = 0f;
                Debug.Log($"<color=red><b>[VisionCone - {gameObject.name}]</b></color> ¡JUGADOR DETECTADO AL 100%! Disparando evento OnPlayerDetected.");
                OnPlayerDetected?.Invoke();
            }
            else if (newState == NPCState.Idle && (previousState == NPCState.Returning || previousState == NPCState.Chasing || previousState == NPCState.Detected || previousState == NPCState.Suspicious))
            {
                _cooldownDelayTimer = 0f;
                Debug.Log($"<color=green><b>[VisionCone - {gameObject.name}]</b></color> Sospecha al 0%. Jugador perdido. Disparando evento OnPlayerLost.");
                OnPlayerLost?.Invoke();
            }
        }

        /// <summary>
        /// Permite al NPCController forzar una transición de estado desde fuera
        /// (ej: de Detected a Chasing tras el grito).
        /// </summary>
        public void SetState(NPCState newState)
        {
            TransitionTo(newState);
        }

        // ══════════════════════════════════════════════
        //  DETECCIÓN VISUAL (Cono)
        // ══════════════════════════════════════════════

        private float _visionLogTimer = 0f;

        private bool CheckVision()
        {
            if (_playerTransform == null) return false;

            float actualEyeHeight = eyeHeight > 0.1f ? eyeHeight : 1.6f;
            Vector3 eyeOrigin = transform.position + Vector3.up * actualEyeHeight;

            // Puntos a comprobar en el jugador (pecho, cabeza, cadera)
            float[] checkHeights = new float[] { 1.0f, 1.6f, 0.4f };

            LayerMask combinedMask = obstacleMask | targetMask;
            if (combinedMask.value == 0) combinedMask = ~0; // Si no está configurada, usar todas las capas

            _visionLogTimer += Time.deltaTime;
            bool shouldLogVision = showDebugLogs && _visionLogTimer >= logInterval;
            if (shouldLogVision) _visionLogTimer = 0f;

            string diagnosticInfo = "";

            for (int i = 0; i < checkHeights.Length; i++)
            {
                float height = checkHeights[i];
                string pointName = i == 0 ? "Pecho" : (i == 1 ? "Cabeza" : "Cadera");

                Vector3 targetPosition = _playerTransform.position + Vector3.up * height;
                Vector3 directionToPlayer = targetPosition - eyeOrigin;

                // Comprobar ángulo de visión horizontal
                Vector3 directionHorizontal = new Vector3(directionToPlayer.x, 0f, directionToPlayer.z);
                float angle = Vector3.Angle(transform.forward, directionHorizontal.normalized);
                float maxAngle = viewAngle / 2f;

                if (angle > maxAngle)
                {
                    if (shouldLogVision) diagnosticInfo += $"[{pointName}: Fuera de ángulo ({angle:F1}° > {maxAngle:F1}°)] ";
                    continue; // Fuera del ángulo para este punto
                }

                // Comprobar distancia
                float distance = directionToPlayer.magnitude;
                if (distance > viewDistance)
                {
                    if (shouldLogVision) diagnosticInfo += $"[{pointName}: Fuera de distancia ({distance:F1}m > {viewDistance:F1}m)] ";
                    continue;
                }

                // Lanzar RaycastAll para atravesar los propios colliders del NPC
                RaycastHit[] hits = Physics.RaycastAll(eyeOrigin, directionToPlayer.normalized, distance, combinedMask, QueryTriggerInteraction.Ignore);
                if (hits.Length == 0)
                {
                    if (shouldLogVision) diagnosticInfo += $"[{pointName}: Sin impactos raycast] ";
                    continue;
                }

                // Ordenar impactos por cercanía al ojo
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    // Ignorar si el collider pertenece al propio NPC (raíz o hijos)
                    if (hit.transform.root == transform.root)
                        continue;

                    // Si el primer impacto no-propio es el jugador -> ¡VISTO!
                    bool isPlayerHit = ((1 << hit.collider.gameObject.layer) & targetMask) != 0 ||
                                       hit.transform.root == _playerTransform.root ||
                                       hit.collider.CompareTag("Player") ||
                                       hit.collider.GetComponentInParent<StarterAssetsInputs>() != null;

                    if (isPlayerHit)
                    {
                        if (shouldLogVision)
                        {
                            Debug.Log($"<color=lime><b>[VisionGuard - {gameObject.name}]</b></color> <color=white>¡JUGADOR VISTO en {pointName}!</color> | " +
                                      $"Collider: <b>{hit.collider.gameObject.name}</b> (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}) | " +
                                      $"Ángulo: <b>{angle:F1}°</b> (Max: {maxAngle:F1}°) | Distancia: <b>{distance:F1}m</b>");
                        }
                        return true;
                    }
                    else
                    {
                        // Hay un obstáculo real (pared, objeto) bloqueando la vista antes de llegar al jugador
                        if (shouldLogVision) diagnosticInfo += $"[{pointName}: Bloqueado por '{hit.collider.gameObject.name}' (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}) a {hit.distance:F1}m] ";
                        break;
                    }
                }
            }

            if (shouldLogVision && !string.IsNullOrEmpty(diagnosticInfo))
            {
                Debug.Log($"<color=yellow><b>[VisionGuard Diagnostic - {gameObject.name}]</b></color> No visto -> {diagnosticInfo}");
            }

            return false;
        }

        // ══════════════════════════════════════════════
        //  DETECCIÓN AUDITIVA (Esfera)
        // ══════════════════════════════════════════════

        private bool CheckAudio(float range)
        {
            if (_playerInputs == null || _playerTransform == null) return false;

            Vector3 targetPosition = _playerTransform.position + Vector3.up * 1.0f;
            float distance = Vector3.Distance(transform.position, targetPosition);

            // Fuera del rango de audio
            if (distance > range) return false;

            // El jugador debe estar haciendo ruido (moviéndose y no agachado en silencio)
            bool isMoving = _playerInputs.move.sqrMagnitude > 0.01f;
            bool isCrouched = _playerInputs.crouch;
            bool isSprinting = _playerInputs.sprint;
            bool isCrouchRunning = isCrouched && isSprinting && isMoving;

            // Si no se mueve, o va agachado sin correr → no hace ruido
            if (!isMoving || (isCrouched && !isCrouchRunning))
                return false;

            return true;
        }

        // ══════════════════════════════════════════════
        //  MEDIDOR DE SOSPECHA
        // ══════════════════════════════════════════════

        private void UpdateSuspicionMeter(float effectiveAudioRange, bool seePlayer, bool hearPlayer)
        {
            bool isDetecting = seePlayer || hearPlayer;

            if (isDetecting)
            {
                // ─── SUBIR SOSPECHA ───
                _cooldownDelayTimer = 0f;

                // Calcular la tasa de detección visual
                float visualRate = 0f;
                if (seePlayer && _playerTransform != null)
                {
                    float distance = Vector3.Distance(transform.position, _playerTransform.position + Vector3.up * 1.0f);
                    float normalizedDistance = Mathf.Clamp01(distance / viewDistance);
                    float curveFactor = detectionDistanceCurve.Evaluate(normalizedDistance);
                    visualRate = Mathf.Lerp(detectionSpeedMin, detectionSpeedMax, curveFactor);

                    // Agachado reduce la detección visual
                    if (_playerInputs != null && _playerInputs.crouch)
                    {
                        visualRate *= crouchDetectionMultiplier;
                    }
                }

                // Calcular la tasa de detección auditiva
                float audioRate = 0f;
                if (hearPlayer && _playerTransform != null)
                {
                    audioRate = audioDetectionSpeed;

                    bool isSprinting = _playerInputs != null && _playerInputs.sprint;
                    bool isCrouched = _playerInputs != null && _playerInputs.crouch;
                    bool isMoving = _playerInputs != null && _playerInputs.move.sqrMagnitude > 0.01f;
                    bool isCrouchRunning = isCrouched && isSprinting && isMoving;

                    if (isSprinting && !isCrouched)
                    {
                        audioRate *= sprintNoiseMultiplier;
                    }
                    else if (isCrouchRunning)
                    {
                        audioRate *= crouchRunNoiseMultiplier;
                    }

                    // Obstáculo entre NPC y jugador reduce el sonido
                    Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;
                    Vector3 dir = targetPos - transform.position;
                    if (Physics.Raycast(transform.position, dir.normalized, out _, dir.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        audioRate *= audioObstacleMultiplier;
                    }
                }

                // Usar la tasa más alta entre visión y audio
                float finalRate = Mathf.Max(visualRate, audioRate);

                // Kagune activo duplica la velocidad de detección
                if (_playerKagune != null && _playerKagune.IsKaguneActive)
                {
                    finalRate *= kaguneDetectionMultiplier;
                }

                _suspicionMeter = Mathf.Min(100f, _suspicionMeter + finalRate * Time.deltaTime);
            }
            else
            {
                // ─── BAJAR SOSPECHA ───
                if (_currentState == NPCState.Detected || _currentState == NPCState.Chasing)
                {
                    if (_cooldownDelayTimer < detectionCooldownDelay)
                    {
                        _cooldownDelayTimer += Time.deltaTime;
                        return; // No enfriar todavía
                    }
                }

                float suspicionFactor = Mathf.Clamp01(1f - (_suspicionMeter / 100f));
                float cooldownFactor = Mathf.Max(0.02f, suspicionFactor);

                _suspicionMeter = Mathf.Max(0f, _suspicionMeter - cooldownSpeed * cooldownFactor * Time.deltaTime);
            }
        }

        // ══════════════════════════════════════════════
        //  LOG DETALLADO DE SOSPECHA
        // ══════════════════════════════════════════════

        private void LogSuspicion(bool playerIsCamouflaged, bool isPlayerInRange, float distanceToPlayer)
        {
            if (!showDebugLogs) return;

            _logTimer += Time.deltaTime;

            // Condiciones para logear:
            // 1. Cambio relevante en la sospecha (>= 1%)
            // 2. Cambio de estado
            // 3. Temporizador regular si el jugador está en rango (para ver distancia, camuflaje y obstáculos en vivo)
            bool suspicionChanged = Mathf.Abs(_suspicionMeter - _lastLoggedSuspicion) >= 1.0f ||
                (_suspicionMeter == 0f && _lastLoggedSuspicion > 0f) ||
                (_suspicionMeter >= 100f && _lastLoggedSuspicion < 100f);
            bool stateChanged = _currentState != _lastLoggedState;
            bool timerTick = isPlayerInRange && _logTimer >= logInterval;

            if (!suspicionChanged && !stateChanged && !timerTick) return;

            _logTimer = 0f;
            _lastLoggedSuspicion = _suspicionMeter;
            _lastLoggedState = _currentState;

            // ─── Fuente de detección ───
            string fuente;
            switch (_currentDetectionSource)
            {
                case DetectionSource.VisionAndAudio:
                    fuente = "<color=cyan>VISIÓN + AUDIO</color>";
                    break;
                case DetectionSource.Vision:
                    fuente = "<color=lime>VISIÓN</color>";
                    break;
                case DetectionSource.Audio:
                    fuente = "<color=yellow>AUDIO</color>";
                    break;
                default:
                    fuente = "Ninguna";
                    break;
            }

            // ─── Distancia al jugador ───
            string distanciaInfo = (_playerTransform != null && isPlayerInRange)
                ? $"{distanceToPlayer:F1}m"
                : "— (Fuera de rango)";

            // ─── Obstáculo visual ───
            string obstaculoInfo = "—";
            if (_playerTransform != null && isPlayerInRange)
            {
                Vector3 targetPos = _playerTransform.position + Vector3.up * 1.0f;
                Vector3 dir = targetPos - transform.position;
                if (Physics.Raycast(transform.position, dir.normalized, out RaycastHit hit, dir.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform.root != _playerTransform.root)
                    {
                        obstaculoInfo = $"<color=red>SÍ ({hit.collider.gameObject.name})</color>";
                    }
                    else
                    {
                        obstaculoInfo = "<color=lime>NO (Despejado)</color>";
                    }
                }
                else
                {
                    obstaculoInfo = "<color=lime>NO (Despejado)</color>";
                }
            }

            // ─── Camuflaje ───
            string camuInfo = playerIsCamouflaged
                ? "<color=magenta>🥷 CAMUFLAJE ACTIVO</color>"
                : "<color=white>No</color>";

            // ─── Estado actual ───
            string estadoColor;
            switch (_currentState)
            {
                case NPCState.Idle: estadoColor = "<color=white>Idle</color>"; break;
                case NPCState.Suspicious: estadoColor = "<color=yellow>Suspicious</color>"; break;
                case NPCState.Detected: estadoColor = "<color=red>Detected</color>"; break;
                case NPCState.Chasing: estadoColor = "<color=#FF4444>Chasing</color>"; break;
                case NPCState.Returning: estadoColor = "<color=green>Returning</color>"; break;
                default: estadoColor = _currentState.ToString(); break;
            }

            // Debug.Log($"<color=orange><b>[Sospecha - {gameObject.name}]</b></color> " +
            //           $"<b>{_suspicionMeter:F1}%</b> | " +
            //           $"Estado: {estadoColor} | " +
            //           $"Fuente: {fuente} | " +
            //           $"Distancia: {distanciaInfo} | " +
            //           $"Obstáculo: {obstaculoInfo} | " +
            //           $"Camuflaje: {camuInfo}");
        }

        // ══════════════════════════════════════════════
        //  TRIGGER (Entrada/Salida del rango)
        // ══════════════════════════════════════════════

        private void OnTriggerEnter(Collider other)
        {
            bool isPlayer = ((1 << other.gameObject.layer) & targetMask) != 0 ||
                            other.CompareTag("Player") ||
                            other.GetComponentInParent<StarterAssetsInputs>() != null;

            if (!isPlayer) return;

            _playerTransform = other.transform;

            // Buscar StarterAssetsInputs de forma robusta
            _playerInputs = other.GetComponentInParent<StarterAssetsInputs>();
            if (_playerInputs == null) _playerInputs = other.GetComponentInChildren<StarterAssetsInputs>();
            if (_playerInputs == null) _playerInputs = FindObjectOfType<StarterAssetsInputs>();

            // Buscar KaguneSpawner
            _playerKagune = other.GetComponentInParent<KaguneSpawner>();
            if (_playerKagune == null) _playerKagune = other.GetComponentInChildren<KaguneSpawner>();
            if (_playerKagune == null) _playerKagune = FindObjectOfType<KaguneSpawner>();

            _playerInTrigger = true;
            Debug.Log($"<color=green><b>[VisionCone - {gameObject.name}]</b></color> Jugador entró en rango de detección ({other.gameObject.name}).");
        }

        private void OnTriggerExit(Collider other)
        {
            bool isPlayer = ((1 << other.gameObject.layer) & targetMask) != 0 ||
                            other.CompareTag("Player") ||
                            other.GetComponentInParent<StarterAssetsInputs>() != null;

            if (!isPlayer) return;

            _playerInTrigger = false;
            Debug.Log($"<color=yellow><b>[VisionCone - {gameObject.name}]</b></color> Jugador salió del rango de detección ({other.gameObject.name}).");
        }

        // ══════════════════════════════════════════════
        //  GIZMOS (Visualización en el Editor)
        // ══════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            // Dibujar cono de visión — color según estado
            switch (_currentState)
            {
                case NPCState.Detected:
                case NPCState.Chasing:
                    Gizmos.color = Color.red;
                    break;
                case NPCState.Suspicious:
                    Gizmos.color = Color.yellow;
                    break;
                case NPCState.Returning:
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
                    break;
                default:
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                    break;
            }

            Vector3 leftLimit = RotateVectorAroundY(transform.forward, -viewAngle / 2f);
            Vector3 rightLimit = RotateVectorAroundY(transform.forward, viewAngle / 2f);
            Gizmos.DrawRay(transform.position, leftLimit * viewDistance);
            Gizmos.DrawRay(transform.position, rightLimit * viewDistance);

            // Dibujar rango de audio (esfera)
            float effectiveAudioRange = audioRange > 0f ? audioRange : viewDistance / 2f;
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, effectiveAudioRange);

            // Línea hacia el jugador si está en rango
            if (_playerTransform != null && _playerInTrigger)
            {
                switch (_currentDetectionSource)
                {
                    case DetectionSource.Vision:
                    case DetectionSource.VisionAndAudio:
                        Gizmos.color = Color.green;
                        break;
                    case DetectionSource.Audio:
                        Gizmos.color = Color.yellow;
                        break;
                    default:
                        Gizmos.color = Color.red;
                        break;
                }
                Gizmos.DrawLine(transform.position, _playerTransform.position);
            }
        }

        private Vector3 RotateVectorAroundY(Vector3 vector, float angleDegrees)
        {
            return Quaternion.Euler(0, angleDegrees, 0) * vector;
        }
    }
}
