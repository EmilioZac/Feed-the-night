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

        private float _frenzyAttackTimer;

        private void Awake()
        {
            _hunger = GetComponent<HungerSystem>();
            _combat = GetComponent<PlayerCombat>();
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _mainController = GetComponent<ThirdPersonController>();
            _interaction = GetComponent<PlayerInteraction>();
        }

        public void UpdateFrenzy(float verticalVelocity)
        {
            if (_hunger != null && _hunger.IsFrenzy)
            {
                _input.jump = false;
                HandleFrenzyState(verticalVelocity);
            }
        }

        private void HandleFrenzyState(float verticalVelocity)
        {
            if (_interaction != null && _interaction.IsFeeding)
            {
                _frenzyAttackTimer = 0f;
                return;
            }

            GameObject nearestNPC = FindNearestNPC();
            Vector3 move = Vector3.zero;

            if (nearestNPC != null)
            {
                Vector3 direction = (nearestNPC.transform.position - transform.position);
                direction.y = 0;
                if (direction.magnitude > 1.5f) move = direction.normalized;
            }

            float frenzySpeed = _mainController.SprintSpeed * 0.8f;
            _controller.Move(move * frenzySpeed * Time.deltaTime + new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);

            _frenzyAttackTimer += Time.deltaTime;
            if (_frenzyAttackTimer >= 0.5f)
            {
                _frenzyAttackTimer = 0f;
                if (_combat.CanAttack)
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
                float dist = Vector3.Distance(npc.transform.position, transform.position);
                if (dist < minDist) { nearest = npc; minDist = dist; }
            }
            return nearest;
        }
    }
}
