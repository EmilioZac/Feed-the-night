using UnityEngine;
using Unity.Cinemachine;

namespace FeedTheNight.Controllers
{
    /// <summary>
    /// Sincroniza la rotación horizontal del jugador con la rotación de la cámara Cinemachine.
    /// Esto permite que el jugador gire para mirar hacia donde apunta la cámara de forma fluida.
    /// </summary>
    public class CinemachinePlayerLink : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("El cuerpo del jugador que queremos rotar.")]
        public Transform playerBody;
        
        [Tooltip("La cámara virtual de Cinemachine.")]
        public CinemachineCamera virtualCamera;

        [Header("Configuración")]
        [Tooltip("Si es true, el jugador siempre mirará en la dirección horizontal de la cámara.")]
        public bool rotatePlayerYaw = true;

        private void Start()
        {
            // Bloquear cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerBody == null) playerBody = transform;
        }

        private void LateUpdate()
        {
            if (rotatePlayerYaw && Camera.main != null)
            {
                // Obtenemos la rotación Y (Yaw) de la cámara principal (manejada por Cinemachine)
                float targetYaw = Camera.main.transform.eulerAngles.y;
                
                // Aplicamos solo esa rotación al cuerpo del jugador
                playerBody.rotation = Quaternion.Euler(0, targetYaw, 0);
            }
        }
    }
}
