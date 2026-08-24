using UnityEngine;
using UnityEngine.AI;

namespace FeedTheNight.NPCs
{
    [AddComponentMenu("FeedTheNight/NPCs/NPC Controller")]
    public class NPCController : MonoBehaviour
    {
        public NavMeshAgent AI;
        public float Velocidad;
        public Transform[] Objetivos;
        Transform Objetivo;
        public float Distancia;

        [Header("Animaciones")]
        public Animator Anim;
        public string CaminandoAnim;

        void Start()
        {
            Objetivo = Objetivos[Random.Range(0, Objetivos.Length)];

            if (Anim != null && !string.IsNullOrEmpty(CaminandoAnim))
            {
                Anim.Play(CaminandoAnim);
            }
        }

        // Update is called once per frame
        void Update()
        {
            Distancia = Vector3.Distance(transform.position, Objetivo.position);

            if (Distancia < 2)
            {
                Objetivo = Objetivos[Random.Range(0, Objetivos.Length)];
            }

            AI.destination = Objetivo.position;

            AI.speed = Velocidad;
        }
    }
}
