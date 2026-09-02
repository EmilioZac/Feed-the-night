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
        private Coroutine _screamCoroutine;

        private void Awake()
        {
            Debug.Log($"<color=cyan><b>[NPCController - Awake]</b></color> Inicializando script en <b>{gameObject.name}</b>. ¿Componente activo? {enabled}");
        }

        void Start()
        {
            if (AI == null)
            {
                AI = GetComponent<NavMeshAgent>();
                if (AI != null)
                {
                    Debug.Log($"<b>[NPCController - {gameObject.name}]</b> NavMeshAgent asignado automáticamente mediante GetComponent.");
                }
                else
                {
                    Debug.LogError($"<b>[NPCController - {gameObject.name}]</b> ¡ERROR! No se encontró NavMeshAgent.");
                }
            }

            // Validar componentes y clips asignados
            if (Anim == null)
            {
                Anim = GetComponentInChildren<Animator>();
                if (Anim != null)
                    Debug.Log($"<b>[NPCController - {gameObject.name}]</b> Animator asignado automáticamente desde hijos.");
                else
                    Debug.LogWarning($"<b>[NPCController - {gameObject.name}]</b> Animator no asignado y no encontrado en hijos.");
            }
            if (CaminandoClip == null) Debug.LogWarning($"<b>[NPCController - {gameObject.name}]</b> CaminandoClip no asignado en el Inspector.");
            if (GritandoClip == null) Debug.LogWarning($"<b>[NPCController - {gameObject.name}]</b> GritandoClip no asignado en el Inspector.");
            if (CorriendoClip == null) Debug.LogWarning($"<b>[NPCController - {gameObject.name}]</b> CorriendoClip no asignado en el Inspector.");
            
            Debug.Log($"<b>[NPCController - {gameObject.name}]</b> Configuración de velocidad -> Caminar: {Velocidad} | Correr: {VelocidadCorrer}");

            if (Objetivos != null && Objetivos.Length > 0)
            {
                Objetivo = Objetivos[Random.Range(0, Objetivos.Length)];
            }

            // Suscribirse a los eventos del cono de visión
            visionCone = GetComponent<VisionCone>();
            if (visionCone == null) visionCone = GetComponentInChildren<VisionCone>();
            if (visionCone == null) visionCone = GetComponentInParent<VisionCone>();

            if (visionCone != null)
            {
                visionCone.OnStateChanged += HandleVisionStateChanged;
                visionCone.OnPlayerDetected += IniciarModoScream;
                visionCone.OnPlayerLost += VolverAPatrulla;
                Debug.Log($"<b>[NPCController - {gameObject.name}]</b> Suscrito con éxito a los eventos de VisionCone ({visionCone.gameObject.name}).");
            }
            else
            {
                Debug.LogError($"<b>[NPCController - {gameObject.name}]</b> ¡ERROR! No se encontró el componente 'Vision Cone' en este objeto ni en sus padres/hijos.");
            }

            // Iniciar en estado Idle con animación de caminar
            SetState(NPCState.Idle);
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
            switch (_currentState)
            {
                case NPCState.Idle:
                case NPCState.Suspicious:
                case NPCState.Returning:
                    UpdatePatrol(Velocidad);
                    break;

                case NPCState.Detected:
                    // Durante el grito, el NPC no se mueve
                    if (AI != null && !AI.isStopped)
                    {
                        AI.isStopped = true;
                    }
                    break;

                case NPCState.Chasing:
                    UpdatePatrol(VelocidadCorrer);
                    break;
            }
        }

        private void UpdatePatrol(float speed)
        {
            if (AI == null) return;

            if (AI.isStopped)
            {
                AI.isStopped = false;
            }

            if (Mathf.Abs(AI.speed - speed) > 0.01f)
            {
                AI.speed = speed;
            }

            if (Objetivo != null)
            {
                Distancia = Vector3.Distance(transform.position, Objetivo.position);

                if (Distancia < 2f && Objetivos != null && Objetivos.Length > 0)
                {
                    Objetivo = Objetivos[Random.Range(0, Objetivos.Length)];
                }

                AI.destination = Objetivo.position;
            }
        }

        // ══════════════════════════════════════════════
        //  MANEJO DE MÁQUINA DE ESTADOS
        // ══════════════════════════════════════════════

        private void HandleVisionStateChanged(NPCState oldState, NPCState newState)
        {
            // Sincronizar estado si proviene de VisionCone
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
                    if (AI != null) AI.isStopped = false;
                    PlayAnimationClip(CaminandoClip);
                    break;

                case NPCState.Detected:
                    if (AI != null) AI.isStopped = true;
                    PlayAnimationClip(GritandoClip);
                    break;

                case NPCState.Chasing:
                    if (AI != null) AI.isStopped = false;
                    PlayAnimationClip(CorriendoClip);
                    break;
            }
        }

        private void IniciarModoScream()
        {
            if (_currentState == NPCState.Detected || _currentState == NPCState.Chasing) return;

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

            Debug.Log($"<color=yellow><b>[NPCController - {gameObject.name}]</b></color> Fin del grito. Transicionando a estado Chasing.");
            SetState(NPCState.Chasing);

            if (visionCone != null)
            {
                visionCone.SetState(NPCState.Chasing);
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
