using UnityEngine;

namespace Game.Battle.Visual
{
    public sealed class BattleCharacterView : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool flipX;

        [Header("Animations")]
        [SerializeField] private SpriteFrameAnimator animator;
        [SerializeField] private IdleAnimation idleAnimation;
        [SerializeField] private Sprite[] idleFrames;

        private void Reset()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<SpriteFrameAnimator>();
        }

        private void Awake()
        {
            if (spriteRenderer != null)
                spriteRenderer.flipX = flipX;

            if (animator != null && spriteRenderer != null)
                animator.SetTarget(spriteRenderer);
        }

        public void PlayIdle()
        {
            if (animator == null)
                return;

            var frames = ResolveIdleFrames(out float fps);
            if (frames == null || frames.Length == 0)
                return;

            if (fps > 0f)
                animator.SetFramesPerSecond(fps);

            animator.PlayLoop(frames);
        }

        public void SetIdleAnimation(IdleAnimation animation)
        {
            idleAnimation = animation;
        }

        private Sprite[] ResolveIdleFrames(out float fps)
        {
            fps = 0f;

            if (idleAnimation != null && idleAnimation.IsValid())
            {
                fps = idleAnimation.FrameRate;
                return idleAnimation.FramesArray;
            }

            return idleFrames;
        }

        public void Stop()
        {
            animator?.Stop();
        }
    }
}
