using UnityEngine;
using UnityEngine.InputSystem;
using FeedTheNight.Systems;

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

        /// <summary>
        /// Indica si el Kagune está actualmente activado.
        /// </summary>
        public bool IsKaguneActive => _kaguneActive;

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

            // Si se desactiva, reseteamos el combo
            if (!_kaguneActive)
            {
                _currentComboStep = 0;
            }

            Debug.Log($"[KaguneSpawner] Estado del Kagune cambiado en Animator a: {(_kaguneActive ? "ACTIVO" : "INACTIVO")}");
        }

        [Header("Combat Combo Settings")]
        public float attackDamage = 1.0f;
        public float attackRange = 2.0f;
        public float attackDuration = 0.6f;
        public float attackCooldown = 0.8f;
        public LayerMask hitLayers;
        public int maxComboSwings = 4;
        public float comboResetTime = 0.8f;

        private int _currentComboStep = 0;
        private float _lastClickTime = -999f;
        private bool _isAttacking = false;

        public void ExecuteKaguneAttack()
        {
            if (_isAttacking) return;

            // Resetear el combo si ha pasado demasiado tiempo
            if (_currentComboStep > 0 && Time.time > _lastClickTime + attackDuration + comboResetTime)
            {
                _currentComboStep = 0;
            }

            bool timeOk = Time.time >= (_lastClickTime + attackCooldown);
            if (timeOk)
            {
                _currentComboStep++;
                if (_currentComboStep > maxComboSwings) _currentComboStep = 1;

                _lastClickTime = Time.time;
                StartCoroutine(PerformKaguneAttackCoroutine(_currentComboStep));
            }
        }

        private System.Collections.IEnumerator PerformKaguneAttackCoroutine(int comboStep)
        {
            _isAttacking = true;

            // Notificamos al controlador del Kagune para que haga sonar/reproducir la animación de ataque
            if (_kaguneController != null)
            {
                _kaguneController.PlayAttackAnimation(comboStep);
            }

            // Esperamos a la mitad de la animación para aplicar el daño (momento del impacto)
            yield return new WaitForSeconds(attackDuration * 0.4f);

            // Detección de daño (usamos OverlapSphere en el frente del jugador)
            if (hitLayers.value == 0) hitLayers = LayerMask.GetMask("npc", "NPC", "Enemy", "Default");
            
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1.2f, attackRange, hitLayers);
            foreach (var hit in hits)
            {
                if (hit.transform.root == transform.root) continue;

                float finalDamage = attackDamage;
                if (comboStep == 4) finalDamage *= 1.8f; // El último golpe hace más daño

                var npcCivil = hit.GetComponentInParent<FeedTheNight.NPCs.NPCCivil>();
                if (npcCivil != null)
                {
                    npcCivil.TakeDamage(finalDamage);
                    continue;
                }

                HealthSystem targetHealth = hit.GetComponentInParent<HealthSystem>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(finalDamage);
                }
            }

            // Esperar el resto de la animación
            yield return new WaitForSeconds(attackDuration * 0.6f);
            _isAttacking = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (_kaguneActive)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + transform.forward * 1.2f, attackRange);
            }
        }
    }
}
