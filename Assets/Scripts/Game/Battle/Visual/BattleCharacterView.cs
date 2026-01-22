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

        [Header("Visual Profile (Optional)")]
        [SerializeField] private CharacterVisualProfile visualProfile;
        [SerializeField] private string outfitId = "outfit_01";

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

        public void Play(BattleVisualAnimId animId)
        {
            if (animator == null)
                return;

            var anim = ResolveAnimation(animId);
            if (anim == null || !anim.IsValid())
                return;

            animator.SetFramesPerSecond(anim.FrameRate);
            animator.PlayLoop(anim.FramesArray);
        }

        public void SetIdleAnimation(IdleAnimation animation)
        {
            idleAnimation = animation;
        }

        public void SetVisualProfile(CharacterVisualProfile profile)
        {
            visualProfile = profile;
        }

        public void SetOutfitId(string id)
        {
            outfitId = string.IsNullOrEmpty(id) ? "outfit_01" : id;
        }

        private Sprite[] ResolveIdleFrames(out float fps)
        {
            fps = 0f;

            var idle = ResolveAnimation(BattleVisualAnimId.Idle);
            if (idle != null && idle.IsValid())
            {
                fps = idle.FrameRate;
                return idle.FramesArray;
            }

            if (idleAnimation != null && idleAnimation.IsValid())
            {
                fps = idleAnimation.FrameRate;
                return idleAnimation.FramesArray;
            }

            return idleFrames;
        }

        private IdleAnimation ResolveAnimation(BattleVisualAnimId animId)
        {
            if (visualProfile == null)
                return null;

            var outfit = visualProfile.ResolveOutfit(outfitId);
            if (outfit == null)
                return null;

            return outfit.Get(animId);
        }

        public void Stop()
        {
            animator?.Stop();
        }
    }
}
