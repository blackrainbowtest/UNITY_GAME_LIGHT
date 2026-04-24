using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UDA2.Audio;

namespace Game.Battle.Visual
{
    public sealed class BattleCharacterView : MonoBehaviour
    {
        private const float DefaultIdleFramesPerSecond = 12f;

        public event System.Action<BattleVisualAnimId, IdleAnimation> OnOneShotStarted;

        private bool hasPendingAnim;
        private BattleVisualAnimId pendingAnimId;
        private System.Action pendingAnimFinished;
        private System.Action pendingAnimImpact;
        private int pendingAnimImpactFrameIndexOverride;
        private BattleVisualAnimId currentOneShotAnimId;
        private System.Action currentOneShotFinished;


        [Header("Рендер")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool flipX;

        [Header("Анимации")]
        [SerializeField] private SpriteFrameAnimator animator;
        [SerializeField] private IdleAnimation idleAnimation;
        [SerializeField] private bool randomizeIdle;
        [SerializeField] private IdleAnimation[] idleVariations;
        [Header("Фоновый Idle (Опционально)")]
        [Tooltip("Если включено, idleVariations[0] используется как основной idle-цикл, а idleVariations[1..] считаются фоновыми idle и проигрываются время от времени (с возвратом к основному idle).")]
        [SerializeField] private bool useAmbientIdle;
        [Tooltip("Как часто (в секундах) пытаться запускать фоновый idle во время основного idle-цикла.")]
        [SerializeField, Min(0.1f)] private float ambientIdleIntervalSeconds = 4f;
        [Tooltip("Дополнительный коэффициент громкости для настроенных небоевых звуков анимаций (idle/state).")]
        [SerializeField, Range(0f, 1f)] private float configuredAnimationCueVolumeScale = 0.15f;
        [SerializeField] private bool avoidImmediateIdleRepeat = true;
        [SerializeField, Min(1)] private int minIdleVariationRepeats = 1;
        [SerializeField, Min(1)] private int maxIdleVariationRepeats = 1;
        [SerializeField] private Sprite[] idleFrames;

        [Header("Визуальный Профиль (Опционально)")]
        [SerializeField] private CharacterVisualProfile visualProfile;
        [SerializeField] private string outfitId = "outfit_01";

        private int idleToken;
        private int lastIdleVariationIndex = -1;
        private IdleAnimation[] resolvedIdleVariations;
        private bool autoIdleFallbackEnabled = true;

        private Coroutine ambientIdleRoutine;
        private int ambientIdleToken;
        private IdleAnimation defaultIdleAnim;
        private IdleAnimation currentLoopingIdleAnimation;
        private List<IdleAnimation> ambientIdleBag;
        private int ambientIdleBagPos;
        private IdleAnimation lastAmbientIdlePlayed;
        private int animationCueToken;
        private BattleVisualAnimId lastConfiguredCueAnimId;
        private IdleAnimation lastConfiguredCueAnimation;
        private AudioSource loopedAnimationCueSource;
        private Coroutine loopedAnimationCueRoutine;
        private int loopedAnimationCueToken;
        private BattleVisualAnimId loopedAnimationCueAnimId;
        private AudioCue loopedAnimationCue;

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

            EnsureLoopedAnimationCueSource();
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

            if (useAmbientIdle && TryStartDefaultPlusAmbientIdle())
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
                    currentLoopingIdleAnimation = only;
                    TryPlayConfiguredAnimationCue(BattleVisualAnimId.Idle, only);
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

            currentLoopingIdleAnimation = null;
            animator.PlayLoop(frames);
        }

        private bool TryStartDefaultPlusAmbientIdle()
        {
            var variations = ResolveIdleVariations();
            if (variations == null || variations.Length == 0)
                return false;

            // Default idle must be the first item in the list (as requested).
            defaultIdleAnim = variations[0] != null && variations[0].IsValid() ? variations[0] : PickFirstValid(variations);
            if (defaultIdleAnim == null || !defaultIdleAnim.IsValid())
                return false;

            // Build ambient pool from the rest of the list.
            EnsureAmbientBagInitialized(variations);

            TryPlayConfiguredAnimationCue(BattleVisualAnimId.Idle, defaultIdleAnim);
            animator.SetFramesPerSecond(defaultIdleAnim.FrameRate);
            currentLoopingIdleAnimation = defaultIdleAnim;
            animator.PlayLoop(defaultIdleAnim.FramesArray);

            StartAmbientIdleLoop();
            return true;
        }

        private void EnsureAmbientBagInitialized(IdleAnimation[] variations)
        {
            if (ambientIdleBag == null)
                ambientIdleBag = new List<IdleAnimation>(8);

            ambientIdleBag.Clear();
            ambientIdleBagPos = 0;
            lastAmbientIdlePlayed = null;

            if (variations == null)
                return;

            for (int i = 1; i < variations.Length; i++)
            {
                var a = variations[i];
                if (a != null && a.IsValid())
                    ambientIdleBag.Add(a);
            }

            ShuffleAmbientBag();
        }

        private void ShuffleAmbientBag()
        {
            if (ambientIdleBag == null || ambientIdleBag.Count <= 1)
                return;

            // Fisher-Yates shuffle.
            for (int i = ambientIdleBag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (ambientIdleBag[i], ambientIdleBag[j]) = (ambientIdleBag[j], ambientIdleBag[i]);
            }

            // Avoid repeating the last played ambient when starting a new cycle.
            if (lastAmbientIdlePlayed != null && ambientIdleBag.Count > 1 && ambientIdleBag[0] == lastAmbientIdlePlayed)
            {
                int swapIdx = Random.Range(1, ambientIdleBag.Count);
                (ambientIdleBag[0], ambientIdleBag[swapIdx]) = (ambientIdleBag[swapIdx], ambientIdleBag[0]);
            }
        }

        private void StartAmbientIdleLoop()
        {
            // Stop any previous routine.
            if (ambientIdleRoutine != null)
            {
                StopCoroutine(ambientIdleRoutine);
                ambientIdleRoutine = null;
            }

            // Only run if we have ambient idles to play.
            if (ambientIdleBag == null || ambientIdleBag.Count == 0)
                return;

            ambientIdleToken++;
            int token = ambientIdleToken;
            ambientIdleRoutine = StartCoroutine(AmbientIdleRoutine(token));
        }

        private IEnumerator AmbientIdleRoutine(int token)
        {
            while (token == ambientIdleToken)
            {
                yield return new WaitForSeconds(ambientIdleIntervalSeconds);

                if (token != ambientIdleToken)
                    yield break;

                if (!useAmbientIdle)
                    yield break;

                // Only play ambient when we are safely looping idle and nothing is pending.
                if (animator == null || !animator.IsPlaying || !animator.IsLooping)
                    continue;

                if (hasPendingAnim)
                    continue;

                var ambient = NextAmbientIdleOrNull();
                if (ambient == null || !ambient.IsValid())
                    continue;

                lastAmbientIdlePlayed = ambient;
                TryPlayConfiguredAnimationCue(BattleVisualAnimId.Idle, ambient);

                // Play ambient once, then return to default idle (or pending combat anim, if any).
                animator.SetFramesPerSecond(ambient.FrameRate);
                animator.PlayOnce(ambient.FramesArray, finished: () =>
                {
                    if (token != ambientIdleToken)
                        return;

                    if (TryPlayPendingNow())
                        return;

                    if (defaultIdleAnim != null && defaultIdleAnim.IsValid())
                    {
                        animator.SetFramesPerSecond(defaultIdleAnim.FrameRate);
                        currentLoopingIdleAnimation = defaultIdleAnim;
                        animator.PlayLoop(defaultIdleAnim.FramesArray);
                    }
                    else
                    {
                        PlayIdle();
                    }
                });
            }
        }

        private IdleAnimation NextAmbientIdleOrNull()
        {
            if (ambientIdleBag == null || ambientIdleBag.Count == 0)
                return null;

            if (ambientIdleBagPos >= ambientIdleBag.Count)
            {
                ambientIdleBagPos = 0;
                ShuffleAmbientBag();
            }

            // If we only have 1 ambient idle, repeats are unavoidable.
            if (ambientIdleBagPos < ambientIdleBag.Count)
            {
                var anim = ambientIdleBag[ambientIdleBagPos];
                ambientIdleBagPos++;
                return anim;
            }

            return null;
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

        public void PlayImmediate(BattleVisualAnimId animId, System.Action onFinished = null)
        {
            PlayImmediate(animId, onFinished, onImpact: null, impactFrameIndexOverride: -1);
        }

        public void PlayImmediate(
            BattleVisualAnimId animId,
            System.Action onFinished,
            System.Action onImpact,
            int impactFrameIndexOverride = -1)
        {
            if (animator == null)
            {
                onFinished?.Invoke();
                return;
            }

            if (animId == BattleVisualAnimId.Idle)
            {
                PlayIdle();
                onFinished?.Invoke();
                return;
            }

            // Immediate one-shot play (interrupts current playback).
            TryPlayOnceInternal(
                animId,
                finished: onFinished,
                onImpact: onImpact,
                impactFrameIndexOverride: impactFrameIndexOverride);
        }

        /// <summary>
        /// Requests an animation to play after the current animation finishes.
        /// If we are looping (idle), it starts on the next loop boundary.
        /// </summary>
        public bool RequestPlayAfterCurrent(
            BattleVisualAnimId animId,
            System.Action onFinished = null,
            System.Action onImpact = null,
            int impactFrameIndexOverride = -1)
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
            pendingAnimImpact = onImpact;
            pendingAnimImpactFrameIndexOverride = impactFrameIndexOverride;

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

        public OutfitVisuals ResolveOutfitVisuals()
        {
            if (visualProfile == null)
                return null;
            return visualProfile.ResolveOutfit(outfitId);
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
            pendingAnimImpact = null;
            currentOneShotFinished = null;

            ambientIdleToken++;
            if (ambientIdleRoutine != null)
            {
                StopCoroutine(ambientIdleRoutine);
                ambientIdleRoutine = null;
            }

            StopLoopedAnimationCue();
            animationCueToken++;
            lastConfiguredCueAnimId = BattleVisualAnimId.Idle;
            lastConfiguredCueAnimation = null;
            currentLoopingIdleAnimation = null;

            animator?.Stop();
        }

        public void SetAutoIdleFallbackEnabled(bool enabled)
        {
            autoIdleFallbackEnabled = enabled;
        }

        private void HandleAnimatorLooped()
        {
            // Only trigger pending anim at a clean boundary of a looping clip (idle).
            if (animator == null || !animator.IsLooping)
                return;

            if (hasPendingAnim)
            {
                TryPlayPendingNow();
                return;
            }

            if (currentLoopingIdleAnimation != null && currentLoopingIdleAnimation.IsValid())
                TryPlayConfiguredAnimationCue(BattleVisualAnimId.Idle, currentLoopingIdleAnimation);
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

            if (autoIdleFallbackEnabled)
                PlayIdle();
        }

        private bool TryPlayPendingNow()
        {
            if (!hasPendingAnim)
                return false;

            var id = pendingAnimId;
            var finished = pendingAnimFinished;
            var impact = pendingAnimImpact;
            var impactFrameIndexOverride = pendingAnimImpactFrameIndexOverride;
            hasPendingAnim = false;
            pendingAnimFinished = null;
            pendingAnimImpact = null;
            pendingAnimImpactFrameIndexOverride = -1;

            // Try start immediately; if missing animation, invoke callback and return to idle.
            if (!TryPlayOnceInternal(id, finished, impact, impactFrameIndexOverride))
            {
                finished?.Invoke();
                return false;
            }

            return true;
        }

        private bool TryPlayOnceInternal(
            BattleVisualAnimId animId,
            System.Action finished,
            System.Action onImpact = null,
            int impactFrameIndexOverride = -1)
        {
            if (animator == null)
                return false;

            if (animId == BattleVisualAnimId.Idle)
            {
                PlayIdle();
                finished?.Invoke();
                return true;
            }

            StopLoopedAnimationCue();

            var anim = ResolveAnimationOrVariation(animId);
            if (anim == null || !anim.IsValid())
            {
                Debug.LogWarning(
                    $"[BattleVisual] Missing animation '{animId}' " +
                    $"for outfit '{outfitId}' on '{name}'. Falling back to Idle.",
                    this
                );

                // Stop any currently playing one-shot to avoid visual sticking
                animator.Stop();

                // Safe fallback
                PlayIdle();

                // Ensure callbacks are not lost
                finished?.Invoke();
                return false;
            }

            OnOneShotStarted?.Invoke(animId, anim);
            TryPlayConfiguredAnimationCue(animId, anim);

            currentOneShotAnimId = animId;
            currentOneShotFinished = finished;

            int impactFrameIndex1Based = -1;
            if (onImpact != null)
            {
                if (impactFrameIndexOverride > 0)
                {
                    impactFrameIndex1Based = impactFrameIndexOverride;
                }
                else if (anim != null)
                {
                    anim.TryGetImpactFrameIndex(out impactFrameIndex1Based);
                }
            }

            animator.SetFramesPerSecond(anim.FrameRate);
            animator.PlayOnce(
                anim.FramesArray,
                finished: () => HandleNonLoopFinished(animId),
                impactFrameIndex: impactFrameIndex1Based,
                onImpact: onImpact);
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
            TryPlayConfiguredAnimationCue(BattleVisualAnimId.Idle, anim);
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

        private void TryPlayConfiguredAnimationCue(BattleVisualAnimId animId, IdleAnimation animation)
        {
            bool cueContextChanged = lastConfiguredCueAnimId != animId || lastConfiguredCueAnimation != animation;
            if (cueContextChanged)
            {
                animationCueToken++;
                lastConfiguredCueAnimId = animId;
                lastConfiguredCueAnimation = animation;

                // Cancel pending delayed cues and any looped state cue from previous animation context.
                if (loopedAnimationCueRoutine != null)
                    StopLoopedAnimationCue();
            }

            if (IsCombatAudioDrivenByExecutor(animId))
                return;

            var outfit = ResolveOutfitVisuals();
            if (outfit == null)
                return;

            if (!outfit.TryGetAnimationCueEvents(animId, animation, out var cueEvents, out var loopWhileStateActive))
                return;

            if (cueEvents == null || cueEvents.Length == 0)
                return;

            if (AudioManager.Instance == null)
                return;

            int token = animationCueToken;

            if (loopWhileStateActive)
            {
                var cue = cueEvents[0].cue;
                if (cue == null || cue.Clip == null)
                    return;

                StartLoopedAnimationCue(animId, animation, cue);
                return;
            }

            if (loopedAnimationCueRoutine != null)
                StopLoopedAnimationCue();

            for (int i = 0; i < cueEvents.Length; i++)
            {
                var cue = cueEvents[i].cue;
                if (cue == null || cue.Clip == null)
                    continue;

                int frameToPlay = cueEvents[i].cueAtFrame <= 0 ? 1 : cueEvents[i].cueAtFrame;

                if (animation != null && animation.FrameCount > 0)
                    frameToPlay = Mathf.Clamp(frameToPlay, 1, animation.FrameCount);

                if (frameToPlay <= 1 || animation == null || animation.FrameRate <= 0f)
                {
                    if (token == animationCueToken)
                        AudioManager.Instance.PlayBattleCueAsSfx(cue, configuredAnimationCueVolumeScale, AudioManager.BattleCueRoute.Character);

                    continue;
                }

                float delay = (frameToPlay - 1) / animation.FrameRate;
                StartCoroutine(PlayConfiguredCueAfterDelay(cue, delay, token));
            }
        }

        private void EnsureLoopedAnimationCueSource()
        {
            if (loopedAnimationCueSource != null)
                return;

            var go = new GameObject("LoopedAnimationCue");
            go.transform.SetParent(transform, false);
            loopedAnimationCueSource = go.AddComponent<AudioSource>();
            loopedAnimationCueSource.playOnAwake = false;
            loopedAnimationCueSource.loop = false;
            loopedAnimationCueSource.spatialBlend = 0f;

            if (AudioManager.Instance != null)
                AudioManager.Instance.ConfigureAsSfxSource(loopedAnimationCueSource);
        }

        private void StartLoopedAnimationCue(BattleVisualAnimId animId, IdleAnimation animation, AudioCue cue)
        {
            EnsureLoopedAnimationCueSource();

            if (loopedAnimationCueRoutine != null && loopedAnimationCueAnimId == animId && loopedAnimationCue == cue)
                return;

            StopLoopedAnimationCue();

            loopedAnimationCueAnimId = animId;
            loopedAnimationCue = cue;
            int token = ++loopedAnimationCueToken;
            loopedAnimationCueRoutine = StartCoroutine(LoopedAnimationCueRoutine(token, cue));
        }

        private void StopLoopedAnimationCue()
        {
            loopedAnimationCueToken++;

            if (loopedAnimationCueRoutine != null)
            {
                StopCoroutine(loopedAnimationCueRoutine);
                loopedAnimationCueRoutine = null;
            }

            loopedAnimationCueAnimId = BattleVisualAnimId.Idle;
            loopedAnimationCue = null;

            if (loopedAnimationCueSource != null)
            {
                loopedAnimationCueSource.Stop();
                loopedAnimationCueSource.clip = null;
            }
        }

        private IEnumerator LoopedAnimationCueRoutine(int token, AudioCue cue)
        {
            while (token == loopedAnimationCueToken)
            {
                if (AudioManager.Instance == null || cue == null || cue.Clip == null || loopedAnimationCueSource == null)
                    yield break;

                AudioManager.Instance.PlayBattleCueOnSource(cue, loopedAnimationCueSource, restartIfAlreadyPlaying: true, volumeScale: configuredAnimationCueVolumeScale, route: AudioManager.BattleCueRoute.Character);

                while (token == loopedAnimationCueToken && loopedAnimationCueSource != null && loopedAnimationCueSource.isPlaying)
                    yield return null;

                if (token != loopedAnimationCueToken)
                    yield break;

                yield return null;
            }
        }

        private IEnumerator PlayConfiguredCueAfterDelay(AudioCue cue, float delay, int token)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (token != animationCueToken)
                yield break;

            if (AudioManager.Instance == null)
                yield break;

            AudioManager.Instance.PlayBattleCueAsSfx(cue, configuredAnimationCueVolumeScale, AudioManager.BattleCueRoute.Character);
        }

        private static bool IsCombatAudioDrivenByExecutor(BattleVisualAnimId animId)
        {
            switch (animId)
            {
                case BattleVisualAnimId.FastAttack:
                case BattleVisualAnimId.NormalAttack:
                case BattleVisualAnimId.HeavyAttack:
                case BattleVisualAnimId.CounterAttack:
                case BattleVisualAnimId.Cast:
                case BattleVisualAnimId.FireSpell:
                case BattleVisualAnimId.IceSpell:
                case BattleVisualAnimId.HolySpell:
                case BattleVisualAnimId.DarkSpell:
                case BattleVisualAnimId.Hit:
                case BattleVisualAnimId.LustHit:
                    return true;
                default:
                    return false;
            }
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
