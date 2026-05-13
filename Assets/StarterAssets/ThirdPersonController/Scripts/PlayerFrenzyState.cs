using UnityEngine;
using FeedTheNight.Systems;

namespace StarterAssets
{
    public class PlayerFrenzyState : MonoBehaviour
    {
        private HungerSystem _hunger;
        private PlayerCombat _combat;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private ThirdPersonController _mainController;
        private PlayerInteraction _interaction;
        private PlayerAnimationController _anim;

        private float _frenzyAttackTimer;

        private void Awake()
        {
            _hunger = GetComponent<HungerSystem>();
            _combat = GetComponent<PlayerCombat>();
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _mainController = GetComponent<ThirdPersonController>();
            _interaction = GetComponent<PlayerInteraction>();
            _anim = GetComponentInChildren<PlayerAnimationController>();
            Debug.Log($"[Frenzy Debug] PlayerFrenzyState Awake. Anim: {_anim != null}, Interaction: {_interaction != null}");
        }

        public void UpdateFrenzy(float verticalVelocity)
        {
            _input.jump = false;
            HandleFrenzyState(verticalVelocity);
        }

        private void HandleFrenzyState(float verticalVelocity)
        {
            if (_interaction != null && _interaction.IsFeeding)
            {
                Debug.Log("[Frenzy Debug] Feeding - stopping locomotion animations.");
                _frenzyAttackTimer = 0f;
                _anim?.SetMoveSpeed(0f, 0f);
                return;
            }

            GameObject nearestNPC = FindNearestNPC();
            Vector3 move = Vector3.zero;

            if (nearestNPC != null)
            {
                float dist = Vector3.Distance(nearestNPC.transform.position, transform.position);
                Debug.Log($"[Frenzy Debug] Nearest: {nearestNPC.name}, Dist: {dist:F2}, Attacking: {_combat.IsAttacking}, Grounded: {_mainController.Grounded}");
                
                Vector3 direction = (nearestNPC.transform.position - transform.position);
                direction.y = 0;
                if (direction.magnitude > 1.2f) move = direction.normalized;
            }

            float frenzySpeed = _mainController.SprintSpeed * 0.8f;
            _controller.Move(move * frenzySpeed * Time.deltaTime + new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);

            if (_anim != null)
            {
                // Si estamos atacando, forzamos velocidad 0 para que no intente mezclar Correr con Ataque
                float targetSpeed = (move.magnitude > 0.1f && !_combat.IsAttacking) ? frenzySpeed : 0f;
                if (move.magnitude > 0.1f && !_combat.IsAttacking) Debug.Log($"[Frenzy Debug] Running Animation. Speed: {targetSpeed}");
                _anim.SetMoveSpeed(targetSpeed, move.magnitude);
            }

            if (move.magnitude > 0.1f)
            {
                float targetRotation = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0.0f, Mathf.LerpAngle(transform.eulerAngles.y, targetRotation, Time.deltaTime * 10f), 0.0f);
            }

            _frenzyAttackTimer += Time.deltaTime;
            if (_frenzyAttackTimer >= 0.5f)
            {
                _frenzyAttackTimer = 0f;
                // SOLO atacar si estamos en rango (aumentado a 2.5m porque los colliders a veces impiden acercarse más)
                float distToTarget = nearestNPC != null ? Vector3.Distance(nearestNPC.transform.position, transform.position) : float.MaxValue;
                if (_combat.CanAttack && distToTarget <= 2.5f)
                {
                    _combat.ExecuteFrenzyAttack(1);
                }
            }
        }

        private GameObject FindNearestNPC()
        {
            GameObject[] npcs = GameObject.FindGameObjectsWithTag("npc");
            GameObject nearest = null;
            float minDist = Mathf.Infinity;
            foreach (GameObject npc in npcs)
            {
                // Ignorar NPCs que ya estén muertos para no intentar "cazarlos" (eso es para comer)
                var npcScript = npc.GetComponentInParent<FeedTheNight.NPCs.NPCCivil>();
                if (npcScript != null && npcScript.IsDead) continue;

                float dist = Vector3.Distance(npc.transform.position, transform.position);
                if (dist < minDist) { nearest = npc; minDist = dist; }
            }
            return nearest;
        }
    }
}
