using UnityEngine;

namespace StarterAssets
{
    public class PlayerAudioController : MonoBehaviour
    {
        [Header("Audio Settings")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponentInParent<CharacterController>();
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (FootstepAudioClips == null || FootstepAudioClips.Length == 0) return;

            // Seguridad 1: Solo sonar si la animación es la dominante (peso > 0.5)
            if (animationEvent.animatorClipInfo.weight < 0.5f) return;

            // Seguridad 2: Solo sonar si el personaje realmente se está moviendo horizontalmente
            if (_controller != null)
            {
                float horizontalSpeed = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude;
                if (horizontalSpeed < 0.1f) return;
            }

            var index = Random.Range(0, FootstepAudioClips.Length);
            if (FootstepAudioClips[index] != null)
            {
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (LandingAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}
