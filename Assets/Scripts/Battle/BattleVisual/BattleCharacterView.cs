using UnityEngine;

namespace Game.Battle.Visual
{
    public sealed class BattleCharacterView : MonoBehaviour
    {
        private const float DefaultIdleFramesPerSecond = 12f;

        private bool hasPendingAnim;
        private BattleVisualAnimId pendingAnimId;
        private System.Action pendingAnimFinished;
        private BattleVisualAnimId currentOneShotAnimId;
        private System.Action currentOneShotFinished;


        [Header("Rendering")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool flipX;

        [Header("Animations")]
        [SerializeField] private SpriteFrameAnimator animator;
        [SerializeField] private IdleAnimation idleAnimation;
        [SerializeField] private bool randomizeIdle;
        [SerializeField] private IdleAnimation[] idleVariations;
        [SerializeField] private bool avoidImmediateIdleRepeat = true;
        [SerializeField, Min(1)] private int minIdleVariationRepeats = 1;
        [SerializeField, Min(1)] private int maxIdleVariationRepeats = 1;
        [SerializeField] private Sprite[] idleFrames;

        [Header("Visual Profile (Optional)")]
        [SerializeField] private CharacterVisualProfile visualProfile;
        [SerializeField] private string outfitId = "outfit_01";

        private int idleToken;
        private int lastIdleVariationIndex = -1;
        private IdleAnimation[] resolvedIdleVariations;

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

            if (animator != null)
                animator.OnLooped += HandleAnimatorLooped;
        }

        private void OnDestroy()
        {
            if (animator != null)
                animator.OnLooped -= HandleAnimatorLooped;
        }

        private void OnDisable()
        {
            Stop();
        }

        public void PlayIdle()
        {
            if (animator == null)
                return;

            var variations = ResolveIdleVariations();
            int validVariationCount = CountValid(variations);

            // Auto-behavior:
            // - 0 variations: fallback to single idle / idleAnimation / idleFrames
            // - 1 variation: use it as the idle (no random)
            // - 2+ variations: randomize (unless explicitly disabled)
            bool shouldRandomize = (randomizeIdle || validVariationCount >= 2) && validVariationCount >= 2;
            if (shouldRandomize)
            {
                StartRandomIdleLoop();
                return;
            }

            if (validVariationCount == 1)
            {
                var only = PickFirstValid(variations);
                if (only != null)
                {
                    animator.SetFramesPerSecond(only.FrameRate);
                    animator.PlayLoop(only.FramesArray);
                    return;
                }
            }

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

            if (animId == BattleVisualAnimId.Idle)
            {
                PlayIdle();
                return;
            }

            TryPlayOnceInternal(animId, finished: null);
        }

        /// <summary>
        /// Requests an animation to play after the current animation finishes.
        /// If we are looping (idle), it starts on the next loop boundary.
        /// </summary>
        public bool RequestPlayAfterCurrent(BattleVisualAnimId animId, System.Action onFinished = null)
        {
            if (animator == null)
            {
                onFinished?.Invoke();
                return false;
            }

            if (animId == BattleVisualAnimId.Idle)
            {
                PlayIdle();
                onFinished?.Invoke();
                return true;
            }

            hasPendingAnim = true;
            pendingAnimId = animId;
            pendingAnimFinished = onFinished;

            // If nothing is playing, start immediately.
            if (!animator.IsPlaying)
                TryPlayPendingNow();

            return true;
        }

        public void SetIdleAnimation(IdleAnimation animation)
        {
            idleAnimation = animation;
        }

        public void SetIdleVariations(IdleAnimation[] variations, bool randomize = true)
        {
            idleVariations = variations;
            randomizeIdle = randomize;
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

            // If we fall back to raw frames (no IdleAnimation asset), ensure FPS is still consistent.
            if (idleFrames != null && idleFrames.Length > 0)
                fps = DefaultIdleFramesPerSecond;

            return idleFrames;
        }

        private IdleAnimation ResolveAnimation(BattleVisualAnimId animId)
        {
            if (visualProfile == null)
                return null;

            var outfit = visualProfile.ResolveOutfit(outfitId);
            if (outfit == null)
                return null;

            var variations = outfit.GetVariationsOrNull(animId);
            return PickFirstValid(variations);
        }

        private IdleAnimation ResolveAnimationOrVariation(BattleVisualAnimId animId)
        {
            if (visualProfile == null)
                return null;

            var outfit = visualProfile.ResolveOutfit(outfitId);
            if (outfit == null)
                return null;

            var variations = outfit.GetVariationsOrNull(animId);
            var picked = PickFromVariations(variations);
            if (picked != null)
                return picked;

            return null;
        }

        public void Stop()
        {
            idleToken++;
            resolvedIdleVariations = null;
            hasPendingAnim = false;
            pendingAnimFinished = null;
            currentOneShotFinished = null;
            animator?.Stop();
        }

        private void HandleAnimatorLooped()
        {
            // Only trigger pending anim at a clean boundary of a looping clip (idle).
            if (!hasPendingAnim)
                return;

            // If we are currently not looping anymore (race), ignore.
            if (animator == null || !animator.IsLooping)
                return;

            TryPlayPendingNow();
        }

        private void HandleNonLoopFinished(BattleVisualAnimId finishedId)
        {
            if (finishedId == currentOneShotAnimId)
            {
                var cb = currentOneShotFinished;
                currentOneShotFinished = null;
                cb?.Invoke();
            }

            if (TryPlayPendingNow())
                return;

            PlayIdle();
        }

        private bool TryPlayPendingNow()
        {
            if (!hasPendingAnim)
                return false;

            var id = pendingAnimId;
            var finished = pendingAnimFinished;
            hasPendingAnim = false;
            pendingAnimFinished = null;

            // Try start immediately; if missing animation, invoke callback and return to idle.
            if (!TryPlayOnceInternal(id, finished))
            {
                finished?.Invoke();
                return false;
            }

            return true;
        }

        private bool TryPlayOnceInternal(BattleVisualAnimId animId, System.Action finished)
        {
            if (animator == null)
                return false;

            if (animId == BattleVisualAnimId.Idle)
            {
                PlayIdle();
                finished?.Invoke();
                return true;
            }

            var anim = ResolveAnimationOrVariation(animId);
            if (anim == null || !anim.IsValid())
                return false;

            currentOneShotAnimId = animId;
            currentOneShotFinished = finished;

            animator.SetFramesPerSecond(anim.FrameRate);
            animator.PlayOnce(anim.FramesArray, finished: () => HandleNonLoopFinished(animId));
            return true;
        }

        private bool HasAnyValidIdleVariation()
        {
            var variations = ResolveIdleVariations();
            if (variations == null || variations.Length == 0)
                return false;

            for (int i = 0; i < variations.Length; i++)
            {
                if (variations[i] != null && variations[i].IsValid())
                    return true;
            }

            return false;
        }

        private void StartRandomIdleLoop()
        {
            idleToken++;
            lastIdleVariationIndex = -1;
            resolvedIdleVariations = ResolveIdleVariations();
            PlayNextRandomIdle(token: idleToken);
        }

        private void PlayNextRandomIdle(int token)
        {
            if (token != idleToken)
                return;

            var idx = PickIdleVariationIndex();
            if (idx < 0)
            {
                // Fallback to the default idle behavior.
                randomizeIdle = false;
                PlayIdle();
                return;
            }

            lastIdleVariationIndex = idx;
            var anim = resolvedIdleVariations[idx];

            int minRepeats = Mathf.Max(1, minIdleVariationRepeats);
            int maxRepeats = Mathf.Max(minRepeats, maxIdleVariationRepeats);
            int repeatsLeft = UnityEngine.Random.Range(minRepeats, maxRepeats + 1);

            PlayIdleVariationRepeated(token, anim, repeatsLeft);
        }

        private void PlayIdleVariationRepeated(int token, IdleAnimation anim, int repeatsLeft)
        {
            if (token != idleToken)
                return;

            if (animator == null)
                return;

            if (anim == null || !anim.IsValid())
            {
                PlayNextRandomIdle(token);
                return;
            }

            animator.SetFramesPerSecond(anim.FrameRate);
            animator.PlayOnce(anim.FramesArray, finished: () =>
            {
                if (token != idleToken)
                    return;

                if (TryPlayPendingNow())
                    return;

                if (repeatsLeft > 1)
                {
                    PlayIdleVariationRepeated(token, anim, repeatsLeft - 1);
                }
                else
                {
                    PlayNextRandomIdle(token);
                }
            });
        }

        private int PickIdleVariationIndex()
        {
            if (resolvedIdleVariations == null || resolvedIdleVariations.Length == 0)
                return -1;

            // Try a few random picks first.
            int attempts = Mathf.Clamp(resolvedIdleVariations.Length * 2, 4, 20);
            for (int a = 0; a < attempts; a++)
            {
                int idx = UnityEngine.Random.Range(0, resolvedIdleVariations.Length);
                var anim = resolvedIdleVariations[idx];
                if (anim == null || !anim.IsValid())
                    continue;
                if (avoidImmediateIdleRepeat && idx == lastIdleVariationIndex && resolvedIdleVariations.Length > 1)
                    continue;
                return idx;
            }

            // Fallback to first valid.
            for (int i = 0; i < resolvedIdleVariations.Length; i++)
            {
                var anim = resolvedIdleVariations[i];
                if (anim == null || !anim.IsValid())
                    continue;
                if (avoidImmediateIdleRepeat && i == lastIdleVariationIndex && resolvedIdleVariations.Length > 1)
                    continue;
                return i;
            }

            return -1;
        }

        private IdleAnimation[] ResolveIdleVariations()
        {
            if (idleVariations != null && idleVariations.Length > 0)
                return idleVariations;

            // Pull from OutfitVisuals if configured there.
            var outfit = visualProfile != null ? visualProfile.ResolveOutfit(outfitId) : null;
            if (outfit != null)
            {
                var variations = outfit.GetIdleVariationsOrNull();
                if (variations != null && variations.Length > 0)
                    return variations;
            }

            return null;
        }

        private static int CountValid(IdleAnimation[] list)
        {
            if (list == null || list.Length == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < list.Length; i++)
            {
                var a = list[i];
                if (a != null && a.IsValid())
                    count++;
            }
            return count;
        }

        private static IdleAnimation PickFirstValid(IdleAnimation[] list)
        {
            if (list == null || list.Length == 0)
                return null;

            for (int i = 0; i < list.Length; i++)
            {
                var a = list[i];
                if (a != null && a.IsValid())
                    return a;
            }
            return null;
        }

        private static IdleAnimation PickFromVariations(IdleAnimation[] list)
        {
            if (list == null || list.Length == 0)
                return null;

            // Collect valid indices (small lists expected, keep it simple).
            int validCount = 0;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] != null && list[i].IsValid())
                    validCount++;
            }

            if (validCount == 0)
                return null;

            if (validCount == 1)
                return PickFirstValid(list);

            // 2+ valid: random pick.
            int attempts = Mathf.Clamp(list.Length * 2, 4, 20);
            for (int a = 0; a < attempts; a++)
            {
                int idx = UnityEngine.Random.Range(0, list.Length);
                var anim = list[idx];
                if (anim != null && anim.IsValid())
                    return anim;
            }

            return PickFirstValid(list);
        }
    }
}
