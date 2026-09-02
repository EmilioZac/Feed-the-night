using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FeedTheNight.NPCs
{
    [AddComponentMenu("FeedTheNight/NPCs/NPC Controller")]
    public class NPCController : MonoBehaviour
    {
        [Header("Navegación / Movimiento")]
        public NavMeshAgent AI;
        public float Velocidad = 2.0f;
        public float VelocidadCorrer = 5.0f;
        [Tooltip("Distancia a la que el NPC intenta alejarse del jugador al huir.")]
        public float DistanciaHuida = 15.0f;
        public Transform[] Objetivos;
        private Transform Objetivo;
        public float Distancia;

        [Header("Animaciones")]
        public Animator Anim;
        [Tooltip("Arrastra aquí el clip de animación de caminar (el archivo de animación de Mixamo)")]
        public AnimationClip CaminandoClip;
        [Tooltip("Arrastra aquí el clip de animación de grito (el archivo de animación de Mixamo)")]
        public AnimationClip GritandoClip;
        [Tooltip("Arrastra aquí el clip de animación de correr (el archivo de animación de Mixamo)")]
        public AnimationClip CorriendoClip;

        [Header("Máquina de Estados")]
        [SerializeField] private NPCState _currentState = NPCState.Idle;

        /// <summary>Estado actual del NPC (única fuente de verdad).</summary>
        public NPCState CurrentState => _currentState;

        private VisionCone visionCone;
        private Transform _playerTransform;
        private Coroutine _screamCoroutine;

        private int _currentWaypointIndex = -1;
        private float _stuckTimer = 0f;
        private Vector3 _chaseOffset;
        private float _repathTimer = 0f;

        private void Awake()
        {
            Debug.Log($"<color=cyan><b>[NPCController - Awake]</b></color> Inicializando script en <b>{gameObject.name}</b>. ¿Componente activo? {enabled}");
        }

        void Start()
        {
            // 1. Obtener y configurar NavMeshAgent
            if (AI == null)
            {
                AI = GetComponent<NavMeshAgent>();
            }

            if (AI != null)
            {
                // Asegurar que el NPC esté correctamente posicionado en el NavMesh
                if (!AI.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                    {
                        AI.Warp(hit.position);
                        Debug.Log($"<b>[NPCController - {gameObject.name}]</b> NPC reubicado en NavMesh ({hit.position}).");
                    }
                    else
                    {
                        Debug.LogError($"<b>[NPCController - {gameObject.name}]</b> ¡No se encontró NavMesh cerca de la posición inicial!");
                    }
                }

                // ─── CONFIGURACIÓN DE EVITACIÓN DE OBSTÁCULOS ENTRE NPCs ───
                AI.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                AI.avoidancePriority = Random.Range(10, 90); // Prioridad única para que uno siempre ceda el paso al otro
                AI.radius = 0.4f;
                AI.stoppingDistance = 1.0f;
                AI.autoRepath = true;
                AI.speed = Velocidad;
            }
            else
            {
                Debug.LogError($"<b>[NPCController - {gameObject.name}]</b> ¡ERROR! No se encontró NavMeshAgent.");
            }

            // 2. Configurar Animator y evitar rotaciones de raíz
            if (Anim == null)
            {
                Anim = GetComponentInChildren<Animator>();
            }

            if (Anim != null)
            {
                Anim.applyRootMotion = false;
            }

            // 3. Ignorar colisiones físicas entre este NPC y otros NPCs para evitar atascos físicos
            IgnoreCollisionsWithOtherNPCs();

            // 4. Generar offset aleatorio para rodear al jugador si se usa modo persecución
            float randomAngle = Random.Range(-60f, 60f);
            _chaseOffset = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward * Random.Range(1.0f, 2.0f);

            // 5. Buscar jugador
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }

            // 6. Suscribirse a los eventos del cono de visión
            visionCone = GetComponent<VisionCone>();
            if (visionCone == null) visionCone = GetComponentInChildren<VisionCone>();
            if (visionCone == null) visionCone = GetComponentInParent<VisionCone>();

            if (visionCone != null)
            {
                visionCone.OnStateChanged += HandleVisionStateChanged;
                visionCone.OnPlayerDetected += IniciarModoScream;
                visionCone.OnPlayerLost += VolverAPatrulla;
            }

            // 7. Seleccionar primer waypoint y comenzar a caminar
            PickNextWaypoint();
            SetState(NPCState.Idle);
        }

        private void IgnoreCollisionsWithOtherNPCs()
        {
            Collider[] myColliders = GetComponentsInChildren<Collider>();
            NPCController[] allNPCs = FindObjectsOfType<NPCController>();

            foreach (var npc in allNPCs)
            {
                if (npc != this)
                {
                    Collider[] otherColliders = npc.GetComponentsInChildren<Collider>();
                    foreach (var myCol in myColliders)
                    {
                        if (myCol.isTrigger) continue;
                        foreach (var otherCol in otherColliders)
                        {
                            if (otherCol.isTrigger) continue;
                            Physics.IgnoreCollision(myCol, otherCol, true);
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (visionCone != null)
            {
                visionCone.OnStateChanged -= HandleVisionStateChanged;
                visionCone.OnPlayerDetected -= IniciarModoScream;
                visionCone.OnPlayerLost -= VolverAPatrulla;
            }
        }

        void Update()
        {
            if (AI == null || !AI.isOnNavMesh) return;

            switch (_currentState)
            {
                case NPCState.Idle:
                case NPCState.Suspicious:
                case NPCState.Returning:
                    UpdatePatrol(Velocidad);
                    break;

                case NPCState.Detected:
                    // Durante el grito, el NPC se detiene por completo
                    if (!AI.isStopped)
                    {
                        AI.isStopped = true;
                    }
                    break;

                case NPCState.Fleeing:
                    UpdateFlee(VelocidadCorrer);
                    break;

                case NPCState.Chasing:
                    UpdateChase(VelocidadCorrer);
                    break;
            }
        }

        private void UpdatePatrol(float speed)
        {
            if (AI.isStopped)
            {
                AI.isStopped = false;
            }

            if (Mathf.Abs(AI.speed - speed) > 0.01f)
            {
                AI.speed = speed;
            }

            if (Objetivo == null)
            {
                PickNextWaypoint();
                return;
            }

            Distancia = Vector3.Distance(transform.position, Objetivo.position);

            // Si llegó cerca del waypoint o el camino terminó, cambiar al siguiente
            bool reachedDestination = Distancia < 2.0f || (!AI.pathPending && AI.hasPath && AI.remainingDistance <= AI.stoppingDistance + 0.5f);

            if (reachedDestination)
            {
                PickNextWaypoint();
            }

            // Sistema anti-bloqueo: Si la velocidad física es casi cero durante 1.5s estando en patrulla, reintentar waypoint
            if (AI.velocity.sqrMagnitude < 0.05f && !AI.pathPending)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer >= 1.5f)
                {
                    _stuckTimer = 0f;
                    PickNextWaypoint();
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
        }

        private void UpdateFlee(float speed)
        {
            if (AI.isStopped)
            {
                AI.isStopped = false;
            }

            if (Mathf.Abs(AI.speed - speed) > 0.01f)
            {
                AI.speed = speed;
            }

            _repathTimer += Time.deltaTime;

            // Recalcular ruta de huida cada 0.25s
            if (_repathTimer >= 0.25f)
            {
                _repathTimer = 0f;

                if (_playerTransform == null)
                {
                    var playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null) _playerTransform = playerObj.transform;
                }

                if (_playerTransform != null)
                {
                    // Vector en dirección opuesta al jugador
                    Vector3 dirAway = (transform.position - _playerTransform.position);
                    dirAway.y = 0;
                    if (dirAway.sqrMagnitude < 0.001f) dirAway = -transform.forward;
                    dirAway.Normalize();

                    // Buscar la mejor ruta de escape en abanico (0°, +45°, -45°, +90°, -90°, +135°, -135°)
                    float[] angles = new float[] { 0f, 45f, -45f, 90f, -90f, 135f, -135f };
                    Vector3 bestFleePoint = transform.position + dirAway * DistanciaHuida;
                    float maxPlayerDist = Vector3.Distance(bestFleePoint, _playerTransform.position);
                    bool foundValidPoint = false;

                    foreach (float angle in angles)
                    {
                        Vector3 rotatedDir = Quaternion.Euler(0, angle, 0) * dirAway;
                        Vector3 candidatePoint = transform.position + rotatedDir * DistanciaHuida;

                        if (NavMesh.SamplePosition(candidatePoint, out NavMeshHit hit, 6.0f, NavMesh.AllAreas))
                        {
                            float distToPlayer = Vector3.Distance(hit.position, _playerTransform.position);
                            if (distToPlayer > maxPlayerDist || !foundValidPoint)
                            {
                                maxPlayerDist = distToPlayer;
                                bestFleePoint = hit.position;
                                foundValidPoint = true;
                            }
                        }
                    }

                    // También comprobar si algún waypoint está más lejos y es una buena ruta de escape
                    if (Objetivos != null && Objetivos.Length > 0)
                    {
                        foreach (var wp in Objetivos)
                        {
                            if (wp == null) continue;
                            float distToPlayer = Vector3.Distance(wp.position, _playerTransform.position);
                            float distFromNPC = Vector3.Distance(wp.position, transform.position);

                            if (distToPlayer > maxPlayerDist && distFromNPC < DistanciaHuida * 2f)
                            {
                                maxPlayerDist = distToPlayer;
                                bestFleePoint = wp.position;
                            }
                        }
                    }

                    AI.SetDestination(bestFleePoint);
                }
            }
        }

        private void UpdateChase(float speed)
        {
            if (AI.isStopped)
            {
                AI.isStopped = false;
            }

            if (Mathf.Abs(AI.speed - speed) > 0.01f)
            {
                AI.speed = speed;
            }

            _repathTimer += Time.deltaTime;

            // Actualizar destino hacia el jugador cada 0.2s para no saturar NavMesh y rodearlo con offset
            if (_repathTimer >= 0.2f)
            {
                _repathTimer = 0f;

                if (_playerTransform == null)
                {
                    var playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null) _playerTransform = playerObj.transform;
                }

                if (_playerTransform != null)
                {
                    Vector3 targetPos = _playerTransform.position + _chaseOffset;
                    AI.SetDestination(targetPos);
                }
                else if (Objetivo != null)
                {
                    AI.SetDestination(Objetivo.position);
                }
            }
        }

        private void PickNextWaypoint()
        {
            if (Objetivos == null || Objetivos.Length == 0) return;

            if (Objetivos.Length == 1)
            {
                _currentWaypointIndex = 0;
                Objetivo = Objetivos[0];
            }
            else
            {
                // Elegir un waypoint diferente al actual para no quedarse en el mismo sitio
                int nextIndex;
                int attempts = 0;
                do
                {
                    nextIndex = Random.Range(0, Objetivos.Length);
                    attempts++;
                } while (nextIndex == _currentWaypointIndex && attempts < 10);

                _currentWaypointIndex = nextIndex;
                Objetivo = Objetivos[_currentWaypointIndex];
            }

            if (Objetivo != null && AI != null && AI.isOnNavMesh)
            {
                AI.SetDestination(Objetivo.position);
            }
        }

        // ══════════════════════════════════════════════
        //  MANEJO DE MÁQUINA DE ESTADOS
        // ══════════════════════════════════════════════

        private void HandleVisionStateChanged(NPCState oldState, NPCState newState)
        {
            if (newState == NPCState.Idle && _currentState != NPCState.Idle)
            {
                VolverAPatrulla();
            }
        }

        public void SetState(NPCState newState)
        {
            if (_currentState == newState && _currentState != NPCState.Idle) return;

            NPCState oldState = _currentState;
            _currentState = newState;

            Debug.Log($"<color=cyan><b>[NPCController - {gameObject.name}]</b></color> Cambio de Estado: <b>{oldState}</b> → <b>{newState}</b>");

            switch (newState)
            {
                case NPCState.Idle:
                case NPCState.Suspicious:
                case NPCState.Returning:
                    if (AI != null && AI.isOnNavMesh) AI.isStopped = false;
                    PlayAnimationClip(CaminandoClip);
                    break;

                case NPCState.Detected:
                    if (AI != null && AI.isOnNavMesh) AI.isStopped = true;
                    PlayAnimationClip(GritandoClip);
                    break;

                case NPCState.Fleeing:
                case NPCState.Chasing:
                    if (AI != null && AI.isOnNavMesh) AI.isStopped = false;
                    PlayAnimationClip(CorriendoClip);
                    break;
            }
        }

        private void IniciarModoScream()
        {
            if (_currentState == NPCState.Detected || _currentState == NPCState.Fleeing || _currentState == NPCState.Chasing) return;

            Debug.Log($"<color=red><b>[NPCController - {gameObject.name}]</b></color> ¡Jugador detectado al 100%! Iniciando grito de alerta.");

            if (_screamCoroutine != null)
            {
                StopCoroutine(_screamCoroutine);
            }

            _screamCoroutine = StartCoroutine(ScreamRoutine());
        }

        private IEnumerator ScreamRoutine()
        {
            SetState(NPCState.Detected);

            yield return new WaitForSeconds(1.0f);

            Debug.Log($"<color=yellow><b>[NPCController - {gameObject.name}]</b></color> Fin del grito. Transicionando a estado Fleeing (Huyendo).");
            SetState(NPCState.Fleeing);

            if (visionCone != null)
            {
                visionCone.SetState(NPCState.Fleeing);
            }

            _screamCoroutine = null;
        }

        private void VolverAPatrulla()
        {
            if (_screamCoroutine != null)
            {
                StopCoroutine(_screamCoroutine);
                _screamCoroutine = null;
            }

            Debug.Log($"<color=green><b>[NPCController - {gameObject.name}]</b></color> Volviendo al estado de patrulla normal (Idle).");
            PickNextWaypoint();
            SetState(NPCState.Idle);
        }

        private void PlayAnimationClip(AnimationClip clip)
        {
            if (Anim != null && clip != null)
            {
                Debug.Log($"<color=magenta><b>[NPCController - {gameObject.name}]</b></color> Reproduciendo clip de animación: <b>{clip.name}</b> para estado: <b>{_currentState}</b>");
                Anim.Play(clip.name);
            }
            else
            {
                Debug.LogWarning($"<color=yellow><b>[NPCController - {gameObject.name}]</b></color> No se pudo reproducir animación. ¿Animator nulo? {Anim == null} | ¿Clip nulo? {clip == null}");
            }
        }
    }
}
