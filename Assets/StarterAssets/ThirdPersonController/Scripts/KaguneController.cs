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

        [Tooltip("Nombre del Trigger para iniciar un ataque en el Animator.")]
        public string attackTriggerName = "Attack";

        [Tooltip("Nombre del Integer en el Animator que define el índice del combo actual (1 a 4).")]
        public string comboIntName = "AttackCombo";

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        /// <summary>
        /// Cambia el parámetro booleano en el Animator para activar o desactivar el Kagune.
        /// </summary>
        public void SetKaguneActiveState(bool active)
        {
            if (_animator != null)
            {
                _animator.SetBool(activeBoolName, active);
            }
        }

        /// <summary>
        /// Dispara la animación del combo correspondiente.
        /// </summary>
        public void PlayAttackAnimation(int comboStep)
        {
            if (_animator != null)
            {
                _animator.SetInteger(comboIntName, comboStep);
                _animator.SetTrigger(attackTriggerName);
            }
        }
    }
}
