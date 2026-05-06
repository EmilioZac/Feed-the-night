using UnityEngine;
using FeedTheNight.Systems;

namespace StarterAssets
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Feeding Settings")]
        public float FeedRange = 2.0f;
        
        public bool IsFeeding { get; private set; }
        public bool IsCamouflaged { get; set; }

        private bool _canFeed;
        private GameObject _closestDeadNPC;
        private float _continuousFeedTimer;
        private float _continuousFeedTickTimer;

        private PlayerAnimationController _anim;
        private StarterAssetsInputs _input;
        private HungerSystem _hunger;

        private void Start()
        {
            _input = GetComponent<StarterAssetsInputs>();
            _hunger = GetComponent<HungerSystem>();
            _anim = GetComponentInChildren<PlayerAnimationController>();
        }

        public void HandleInteractions()
        {
            // --- CAMOUFLAGE ---
            IsCamouflaged = _input.camouflage;

            // --- FEEDING ---
            if (_input.feed && _canFeed && _closestDeadNPC != null && !IsCamouflaged)
            {
                IsFeeding = true;
                _continuousFeedTimer += Time.deltaTime;
                _continuousFeedTickTimer += Time.deltaTime;

                if (_continuousFeedTimer <= 8.0f)
                {
                    if (_continuousFeedTickTimer >= 1.0f)
                    {
                        if (_hunger != null) _hunger.ModifyHunger(2.5f);
                        _continuousFeedTickTimer -= 1.0f;
                    }
                }
                else
                {
                    Destroy(_closestDeadNPC);
                    _closestDeadNPC = null;
                    _canFeed = false;
                    IsFeeding = false;
                    _input.feed = false;
                }
            }
            else
            {
                IsFeeding = false;
                _continuousFeedTimer = 0f;
                _continuousFeedTickTimer = 0f;
            }

            if (_anim != null)
            {
                _anim.SetFeed(IsFeeding);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("npc"))
            {
                var npcScript = other.gameObject.GetComponentInParent<FeedTheNight.NPCs.NPCCivil>();
                if (npcScript != null && npcScript.IsDead)
                {
                    _canFeed = true;
                    _closestDeadNPC = npcScript.gameObject;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("npc"))
            {
                var npcScript = other.gameObject.GetComponentInParent<FeedTheNight.NPCs.NPCCivil>();
                if (npcScript != null && _closestDeadNPC == npcScript.gameObject)
                {
                    _canFeed = false;
                    _closestDeadNPC = null;
                }
            }
        }
    }
}
