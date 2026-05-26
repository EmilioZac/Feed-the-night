using UnityEngine;
using FeedTheNight.Systems;

namespace StarterAssets
{
    [RequireComponent(typeof(ThirdPersonController))]
    [RequireComponent(typeof(StarterAssetsInputs))]
    public class PlayerJump : MonoBehaviour
    {
        [Header("Physics Settings")]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("Time required before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        private float _jumpTimeoutDelta;

        // References to caching components
        private ThirdPersonController _controller;
        private StarterAssetsInputs _input;
        private PlayerAnimationController _anim;
        private PlayerCombat _combat;
        private PlayerInteraction _interaction;

        private void Start()
        {
            _controller = GetComponent<ThirdPersonController>();
            _input = GetComponent<StarterAssetsInputs>();
            
            // Caching components safely
            _anim = GetComponentInChildren<PlayerAnimationController>() ?? GetComponent<PlayerAnimationController>();
            _combat = GetComponent<PlayerCombat>();
            _interaction = GetComponent<PlayerInteraction>();

            _jumpTimeoutDelta = JumpTimeout;
        }

        public bool CanJump()
        {
            // Evita saltar si el jugador está haciendo un Dash, agachado, camuflado o si _combat/dashing está activo
            if (_combat != null && _combat.IsDashing) return false;
            if (_input != null && _input.crouch) return false;
            if (_input != null && _input.camouflage) return false;
            
            // Comprobación de camuflaje en PlayerInteraction
            if (_interaction != null && _interaction.IsCamouflaged) return false;

            // Comprobación de temporizador de salto
            if (_jumpTimeoutDelta > 0) return false;

            // Comprobación de energía mínima para saltar
            if (_controller != null && _controller.EnergySystem != null)
            {
                if (_controller.EnergySystem.Energy < _controller.EnergySystem.jumpDrainFlat)
                {
                    return false;
                }
            }

            return true;
        }

        public void UpdateJump(ref float verticalVelocity, float gravity, bool grounded)
        {
            if (grounded)
            {
                if (_anim != null)
                {
                    _anim.SetJump(false);
                    _anim.SetFreeFall(false);
                }

                if (_input != null && _input.jump && CanJump())
                {
                    // Aplicar la velocidad de salto
                    verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * gravity);
                    
                    if (_anim != null)
                    {
                        _anim.SetJump(true);
                    }

                    if (_controller != null && _controller.EnergySystem != null)
                    {
                        _controller.EnergySystem.OnJump();
                    }
                }
                else if (_input != null)
                {
                    _input.jump = false;
                }

                // Decrementar el temporizador de cooldown
                if (_jumpTimeoutDelta >= 0)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // Resetear el temporizador si estamos en el aire y desactivar la entrada de salto
                _jumpTimeoutDelta = JumpTimeout;
                if (_input != null)
                {
                    _input.jump = false;
                }
            }
        }
    }
}
