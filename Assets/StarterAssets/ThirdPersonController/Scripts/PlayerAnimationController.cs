using UnityEngine;

namespace StarterAssets
{
    public class PlayerAnimationController : MonoBehaviour
    {
        private Animator _animator;
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

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
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
            if (!_hasAnimator) { Debug.LogWarning("[Anim Debug] SetAttack called but no Animator found!"); return; }
            Debug.Log($"[Anim Debug] SetAttack: {attacking}, Step: {comboStep}");
            if (attacking) _animator.SetInteger(_animIDComboStep, comboStep);
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
    }
}
