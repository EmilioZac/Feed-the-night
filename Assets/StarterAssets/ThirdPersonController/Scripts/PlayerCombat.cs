using UnityEngine;
using System.Collections;
using FeedTheNight.Systems;

namespace StarterAssets
{
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Attack Settings")]
        public float AttackDamage = 0.5f;
        public float AttackRange = 1.5f;
        public float AttackDuration = 0.8f;
        public float AttackCooldown = 1.0f;
        public LayerMask HitLayers;
        public int MaxComboSwings = 4;
        public float ComboResetTime = 1.0f;

        [Header("Dash Settings")]
        public float DashDistance = 5f;
        public float DashDuration = 1.2f;
        public float DashCooldown = 1f;

        // State
        public bool IsAttacking { get; private set; }
        public bool CanAttack { get; private set; } = true;
        public bool IsDashing { get; private set; }
        public bool CanDash { get; private set; } = true;
        public bool IsBlocking => (_input != null && _input.block);

        private int _currentComboStep = 0;
        private float _lastClickTime = -999f;
        private bool _comboBuffered = false;

        // Dependencies
        private StarterAssetsInputs _input;
        private Animator _animator;
        private CharacterController _controller;
        private EnergySystem _energySystem;
        private GameObject _mainCamera;

        // Animation IDs (Temporary until Phase 4)
        private int _animIDAttack;
        private int _animIDComboStep;
        private int _animIDDash;
        private int _animIDBlockedHit;

        private void Awake()
        {
            _input = GetComponent<StarterAssetsInputs>();
            _animator = GetComponent<Animator>();
            _controller = GetComponent<CharacterController>();
            _energySystem = GetComponent<EnergySystem>();
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

            _animIDAttack = Animator.StringToHash("Attack");
            _animIDComboStep = Animator.StringToHash("ComboStep");
            _animIDDash = Animator.StringToHash("Dash");
            _animIDBlockedHit = Animator.StringToHash("BlockedHit");
        }

        public void HandleCombatActions()
        {
            if (IsDashing)
            {
                _input.attack = false;
                _input.dash = false;
                return;
            }

            if (IsAttacking)
            {
                _input.dash = false;
                if (_input.attack)
                {
                    _comboBuffered = true;
                    _input.attack = false;
                }
                return;
            }

            if (_comboBuffered)
            {
                _input.attack = true;
                _comboBuffered = false;
            }

            if (_currentComboStep > 0 && Time.time > _lastClickTime + AttackDuration + ComboResetTime)
            {
                _currentComboStep = 0;
            }

            if (_input.attack)
            {
                ExecuteAttack();
                _input.attack = false;
            }

            if (_input.dash)
            {
                ExecuteDash();
                _input.dash = false;
            }
        }

        private void ExecuteAttack()
        {
            bool energyOk = _energySystem == null || _energySystem.Energy >= _energySystem.attackDrainFlat;
            bool timeOk = Time.time >= (_lastClickTime + AttackCooldown);

            if (timeOk && energyOk && CanAttack)
            {
                _currentComboStep++;
                if (_currentComboStep > MaxComboSwings) _currentComboStep = 1;

                _lastClickTime = Time.time;
                StartCoroutine(PerformAttackCoroutine(_currentComboStep));
            }
        }

        private void ExecuteDash()
        {
            bool energyOk = _energySystem == null || _energySystem.Energy >= _energySystem.dashDrainFlat;
            if (CanDash && !IsDashing && energyOk)
            {
                Vector3 moveDir;
                if (_input.move != Vector2.zero)
                {
                    Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                    float targetRot = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    moveDir = Quaternion.Euler(0.0f, targetRot, 0.0f) * Vector3.forward;
                    transform.rotation = Quaternion.Euler(0.0f, targetRot, 0.0f);
                }
                else
                {
                    moveDir = transform.forward;
                }
                StartCoroutine(PerformDashCoroutine(moveDir));
            }
        }

        public void ExecuteFrenzyAttack(int step)
        {
            _lastClickTime = Time.time;
            StartCoroutine(PerformAttackCoroutine(step));
        }

        private IEnumerator PerformAttackCoroutine(int comboStep)
        {
            CanAttack = false;
            IsAttacking = true;

            float currentDamage = AttackDamage;
            float currentDuration = AttackDuration;

            if (comboStep == 4)
            {
                currentDamage *= 2f;
                currentDuration += 0.8f;
            }

            if (_animator != null)
            {
                _animator.SetInteger(_animIDComboStep, comboStep);
                _animator.SetBool(_animIDAttack, true);
            }

            if (_energySystem != null)
            {
                _energySystem.OnAttack();
                _energySystem.ResetRegenDelay(AttackCooldown + 0.1f);
            }

            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1f, AttackRange, HitLayers);
            foreach (var hit in hits)
            {
                if (hit.transform.root == transform.root) continue;

                var npcCivil = hit.GetComponentInParent<FeedTheNight.NPCs.NPCCivil>();
                if (npcCivil != null)
                {
                    npcCivil.TakeDamage(currentDamage);
                    continue;
                }

                HealthSystem targetHealth = hit.GetComponentInParent<HealthSystem>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(currentDamage);
                }
            }

            yield return new WaitForSeconds(currentDuration);

            if (_animator != null) _animator.SetBool(_animIDAttack, false);
            IsAttacking = false;

            yield return new WaitForSeconds(Mathf.Max(AttackCooldown - AttackDuration, 0.05f));
            CanAttack = true;
        }

        private IEnumerator PerformDashCoroutine(Vector3 direction)
        {
            CanDash = false;
            IsDashing = true;

            if (_animator != null) _animator.SetBool(_animIDDash, true);
            if (_energySystem != null) _energySystem.OnDash();

            float startTime = Time.time;
            if (direction.magnitude < 0.1f) direction = transform.forward;

            while (Time.time < startTime + DashDuration)
            {
                _controller.Move(direction * (DashDistance / DashDuration) * Time.deltaTime);
                yield return null;
            }

            if (_animator != null) _animator.SetBool(_animIDDash, false);
            IsDashing = false;
            yield return new WaitForSeconds(DashCooldown);
            CanDash = true;
        }

        public float TryBlock(float damage, float blockDuration)
        {
            if (!IsBlocking) return damage;

            if (_animator != null) _animator.SetTrigger(_animIDBlockedHit);

            bool isExhausted = (_energySystem != null && _energySystem.Energy <= 0) || (blockDuration >= 5.0f);

            if (_energySystem != null)
            {
                _energySystem.ModifyEnergy(-1f);
                _energySystem.ResetRegenDelay(1.5f);
            }

            if (!isExhausted) return 0f;
            else return damage * 0.5f;
        }
    }
}
