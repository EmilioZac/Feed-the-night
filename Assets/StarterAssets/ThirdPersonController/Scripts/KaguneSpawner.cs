using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    /// <summary>
    /// Se asigna al Jugador. Captura la tecla R para alternar el estado lógico del Kagune
    /// y notifica al KaguneController para que controle las transiciones de animación.
    /// </summary>
    public class KaguneSpawner : MonoBehaviour
    {
        [Header("Kagune Settings")]
        [Tooltip("Arrastra aquí el GameObject del Kagune.")]
        public GameObject kaguneObject;

        [Tooltip("Si está activado, la tecla R funciona como interruptor (toggle). " +
                 "Si está desactivado, mantener R activa el Kagune, y soltar R lo desactiva.")]
        public bool toggleMode = true;

        private bool _kaguneActive = false;
        private KaguneController _kaguneController;

        private void Start()
        {
            if (kaguneObject != null)
            {
                // Obtenemos o añadimos el controlador en el Kagune
                _kaguneController = kaguneObject.GetComponent<KaguneController>();
                if (_kaguneController == null)
                {
                    _kaguneController = kaguneObject.AddComponent<KaguneController>();
                }

                // El objeto siempre permanece ACTIVO en la escena ahora
                kaguneObject.SetActive(true);
                
                // Pero le indicamos al controlador que empiece en estado inactivo (NoIdle)
                _kaguneController.SetKaguneActiveState(false);
            }
            else
            {
                Debug.LogWarning("[KaguneSpawner] ¡No se ha asignado el GameObject del Kagune!", this);
            }
        }

        private void Update()
        {
            if (kaguneObject == null) return;
            if (Keyboard.current == null) return;

            if (toggleMode)
            {
                // Modo Toggle
                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    SetKaguneActive(!_kaguneActive);
                }
            }
            else
            {
                // Modo Hold
                bool rPressed = Keyboard.current.rKey.isPressed;
                if (rPressed != _kaguneActive)
                {
                    SetKaguneActive(rPressed);
                }
            }
        }

        private void SetKaguneActive(bool active)
        {
            _kaguneActive = active;

            if (_kaguneController != null)
            {
                _kaguneController.SetKaguneActiveState(_kaguneActive);
            }

            Debug.Log($"[KaguneSpawner] Estado del Kagune cambiado en Animator a: {(_kaguneActive ? "ACTIVO" : "INACTIVO")}");
        }
    }
}
