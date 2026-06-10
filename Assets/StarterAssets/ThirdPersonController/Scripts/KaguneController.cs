using UnityEngine;

namespace StarterAssets
{
    /// <summary>
    /// Se asigna directamente al GameObject del Kagune.
    /// Controla el Animator del Kagune enviando los parámetros de visibilidad y estado.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class KaguneController : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Nombre del parámetro booleano en el Animator que indica si el Kagune está activo.")]
        public string activeBoolName = "Active";

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        /// <summary>
        /// Cambia el parámetro booleano en el Animator para activar o desactivar el Kagune.
        /// </summary>
        /// <param name="active">True para iniciar Spawn/Idle, False para volver a NoIdle.</param>
        public void SetKaguneActiveState(bool active)
        {
            if (_animator != null)
            {
                _animator.SetBool(activeBoolName, active);
            }
            else
            {
                Debug.LogWarning("[KaguneController] No se encontró el componente Animator en este GameObject.", this);
            }
        }
    }
}
