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
            Debug.Log($"[Interaction Debug] PlayerInteraction Start. Anim: {_anim != null}");
        }

        public void HandleInteractions()
        {
            // --- CAMOUFLAGE ---
            IsCamouflaged = _input.camouflage;

            // --- FEEDING ---
            bool isFrenzy = (_hunger != null && _hunger.IsFrenzy);
            bool wantToFeed = (_input.feed || (isFrenzy && _canFeed)) && !IsCamouflaged;

            // Si ya estamos comiendo, ignoramos el 'wantToFeed' y seguimos hasta terminar los 8s
            if (IsFeeding || (wantToFeed && _canFeed && _closestDeadNPC != null))
            {
                if (!IsFeeding) Debug.Log("[Interaction Debug] Started feeding session.");
                
                IsFeeding = true;
                _continuousFeedTimer += Time.deltaTime;
                _continuousFeedTickTimer += Time.deltaTime;

                if (_continuousFeedTimer <= 8.0f)
                {
                    if (_continuousFeedTickTimer >= 1.0f)
                    {
                        if (_hunger != null) {
                            _hunger.ModifyHunger(5.0f); // DUPLICADO de 2.5f a 5.0f
                            Debug.Log($"[Interaction Debug] Feeding... Current Hunger: {_hunger.Hunger}");
                        }
                        _continuousFeedTickTimer -= 1.0f;
                    }
                }
                else
                {
                    Debug.Log($"[Interaction Debug] Feeding finished. Final Hunger: {(_hunger != null ? _hunger.Hunger : 0)}");
                    Destroy(_closestDeadNPC);
                    _closestDeadNPC = null;
                    _canFeed = false;
                    IsFeeding = false;
                    _input.feed = false;
                    _continuousFeedTimer = 0f;
                    _continuousFeedTickTimer = 0f;
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
                if (IsFeeding) Debug.Log("[Interaction Debug] Setting Animator 'Feed' to true");
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
