using UnityEngine;

namespace StarterAssets
{
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        private bool _hasAnimator;

        // IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDCrouch;
        private int _animIDAttack;
        private int _animIDComboStep;
        private int _animIDBlocked;
        private int _animIDFeed;
        private int _animIDDash;
        private int _animIDBlockedHit;
        private int _animIDDeath;
        private int _animIDCrouchStart;
        private int _animIDCrouchEnd;
        private int _animIDScream;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator == null)
            {
                Debug.LogError($"[Anim] No se encontró ningún Animator en '{gameObject.name}' ni en sus hijos. " +
                               "Asegúrate de que el nuevo modelo (Kanki1WithSkeleton) sea hijo de PlayerNew y " +
                               "arrastra su Animator al campo 'Animator' del componente PlayerAnimationController.", this);
            }
            else
            {
                bool isChild = _animator.transform.IsChildOf(transform);
                if (!isChild)
                {
                    Debug.LogWarning($"[Anim] El Animator detectado pertenece a '{_animator.gameObject.name}', " +
                                     "que NO es hijo de este GameObject. Las animaciones no se controlarán correctamente. " +
                                     "Arrastra el modelo dentro de PlayerNew en la jerarquía.", _animator);
                }
                else
                {
                    Debug.Log($"[Anim] Animator conectado correctamente → '{_animator.gameObject.name}' " +
                              $"(Avatar: {(_animator.avatar != null ? _animator.avatar.name : "NINGUNO")}, " +
                              $"Humanoid: {_animator.isHuman}, " +
                              $"Apply Root Motion: {_animator.applyRootMotion})", _animator);

                    if (_animator.applyRootMotion)
                    {
                        Debug.LogWarning("[Anim] 'Apply Root Motion' está ACTIVADO en el Animator. " +
                                         "Desactívalo para que el movimiento lo controle solo el CharacterController, " +
                                         "si no el personaje se moverá de forma extraña.", _animator);
                    }
                }
            }

            _hasAnimator = _animator != null;
            AssignAnimationIDs();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDCrouch = Animator.StringToHash("Crouch");
            _animIDAttack = Animator.StringToHash("Attack");
            _animIDComboStep = Animator.StringToHash("ComboStep");
            _animIDBlocked = Animator.StringToHash("Blocked");
            _animIDFeed = Animator.StringToHash("Feed");
            _animIDDash = Animator.StringToHash("Dash");
            _animIDBlockedHit = Animator.StringToHash("BlockedHit");
            _animIDDeath = Animator.StringToHash("Death");
            _animIDCrouchStart = Animator.StringToHash("CrouchStart");
            _animIDCrouchEnd = Animator.StringToHash("CrouchEnd");
            _animIDScream = Animator.StringToHash("Scream");
        }

        public void SetMoveSpeed(float blend, float inputMagnitude)
        {
            if (!_hasAnimator) return;
            _animator.SetFloat(_animIDSpeed, blend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }

        public void SetGrounded(bool grounded)
        {
            if (!_hasAnimator) return;
            _animator.SetBool(_animIDGrounded, grounded);
        }

        public void SetJump(bool jumping)
        {
            if (!_hasAnimator) return;
            _animator.SetBool(_animIDJump, jumping);
        }

        public void SetFreeFall(bool falling)
        {
            if (!_hasAnimator) return;
            _animator.SetBool(_animIDFreeFall, falling);
        }

        public void SetCrouch(bool crouching)
        {
            if (!_hasAnimator) return;
            _animator.SetBool(_animIDCrouch, crouching);
        }

        public void SetBlocking(bool blocking)
        {
            if (!_hasAnimator) return;
            _animator.SetBool(_animIDBlocked, blocking);
        }

        public void TriggerBlockedHit()
        {
            if (!_hasAnimator) return;
            _animator.SetTrigger(_animIDBlockedHit);
        }

        public void SetAttack(bool attacking, int comboStep = 0)
        {
            if (!_hasAnimator) return;
            _animator.SetInteger(_animIDComboStep, comboStep);
            _animator.SetBool(_animIDAttack, attacking);
        }

        public void SetDash(bool dashing)
        {
            if (!_hasAnimator) return;
            _animator.SetBool(_animIDDash, dashing);
        }

        public void SetFeed(bool feeding)
        {
            if (!_hasAnimator) return;
            _animator.SetBool(_animIDFeed, feeding);
        }

        public void TriggerDeath()
        {
            if (!_hasAnimator) return;
            _animator.SetBool(_animIDDeath, true);
        }

        public void TriggerCrouchStart()
        {
            if (!_hasAnimator) return;
            _animator.SetTrigger(_animIDCrouchStart);
        }

        public void TriggerCrouchEnd()
        {
            if (!_hasAnimator) return;
            _animator.SetTrigger(_animIDCrouchEnd);
        }

        public void SetScream(bool active)
        {
            if (!_hasAnimator) return;
            _animator.SetBool(_animIDScream, active);
        }
    }
}
