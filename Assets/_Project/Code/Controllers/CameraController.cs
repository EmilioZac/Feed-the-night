using UnityEngine;
using UnityEngine.InputSystem;

namespace FeedTheNight.Controllers
{
    /// <summary>
    /// Controla la cámara con el ratón usando el nuevo Input System:
    ///  - Eje Y del ratón → rota la cámara verticalmente (pitch)
    ///  - Eje X del ratón → rota el cuerpo del jugador horizontalmente (yaw)
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Transform del cuerpo del jugador para rotación horizontal.")]
        public Transform playerBody;
        
        [Tooltip("Si la cámara no es hija del jugador, arrastra al jugador aquí para seguirlo.")]
        public Transform targetToFollow;

        [Header("Seguimiento (Si no es hija)")]
        public Vector3 followOffset = new Vector3(0, 2f, -5f);
        public float smoothTime = 0.12f;
        private Vector3 _currentVelocity = Vector3.zero;

        [Header("Sensibilidad")]
        [Tooltip("¡IMPORTANTE! Si es muy rápido, baja este valor en el INSPECTOR a 0.01 o menos.")]
        public float mouseSensitivity = 0.05f; 

        [Header("Límites verticales")]
        [Range(-90f, 0f)]  public float minPitch = -80f;
        [Range(0f,  90f)]  public float maxPitch =  80f;

        private float _xRotation = 0f;

        private void Start()
        {
            // Bloquear y ocultar el cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            // Si no hay playerBody pero hay target, intentar auto-asignar
            if (playerBody == null && targetToFollow != null) playerBody = targetToFollow;
        }

        private void LateUpdate()
        {
            // Si el juego está en pausa, no procesar
            if (Time.timeScale == 0f) return;

            // 1. Seguimiento de posición (si no es hija)
            if (targetToFollow != null)
            {
                Vector3 targetPos = targetToFollow.TransformPoint(followOffset);
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, smoothTime);
            }

            // 2. Rotación (Mouse Look con New Input System)
            Vector2 mouseDelta = Vector2.zero;
            
            if (Mouse.current != null)
            {
                mouseDelta = Mouse.current.delta.ReadValue();
            }

            // Aplicar sensibilidad
            float mouseX = mouseDelta.x * mouseSensitivity;
            float mouseY = mouseDelta.y * mouseSensitivity;

            // Debug para ver qué valores están llegando realmente
            if (mouseX != 0 || mouseY != 0)
            {
                // Debug.Log($"[CAMERA DEBUG] Delta: {mouseDelta}, Rot: ({mouseX}, {mouseY}), Sens: {mouseSensitivity}");
            }

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, minPitch, maxPitch);

            // Rotación de la cámara (X local)
            // Si es hija, ajustamos localRotation. Si no, ajustamos rotación global basada en el target.
            if (targetToFollow == null)
            {
                transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            }
            else
            {
                // Mantenemos el Yaw del jugador y aplicamos nuestro Pitch
                transform.rotation = Quaternion.Euler(_xRotation, targetToFollow.eulerAngles.y, 0f);
            }

            // Rotación del cuerpo (Y global)
            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * mouseX);
            }
        }
    }
}
// Force Reimport - Timestamp: 19:15
