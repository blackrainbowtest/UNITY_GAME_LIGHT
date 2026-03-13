using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BackgroundSpriteAnimationPlayer : MonoBehaviour
{
    [System.Serializable]
    private struct AnimationEntry
    {
        public IdleAnimation animation;
        [Min(0f)] public float weight;
    }

    [Header("Target")]
    [SerializeField] private SpriteRenderer spriteRendererTarget;
    [SerializeField] private Image imageTarget;

    [Header("Playback")]
    [SerializeField] private AnimationEntry[] animations;
    [SerializeField, Min(0f)] private float replayDelaySeconds = 0f;
    [SerializeField] private bool randomOrder;
    [SerializeField] private bool avoidImmediateRepeat = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine playRoutine;
    private int orderedIndex;
    private int lastRandomIndex = -1;

    private void Reset()
    {
        if (spriteRendererTarget == null)
            spriteRendererTarget = GetComponent<SpriteRenderer>();

        if (imageTarget == null)
            imageTarget = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        Stop();

        if (!HasValidTarget())
        {
            Debug.LogWarning("BackgroundSpriteAnimationPlayer: Assign SpriteRenderer or UI Image target.", this);
            return;
        }

        if (!HasAnyValidAnimation())
        {
            Debug.LogWarning("BackgroundSpriteAnimationPlayer: No valid animations assigned.", this);
            return;
        }

        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        if (playRoutine == null)
            return;

        StopCoroutine(playRoutine);
        playRoutine = null;
    }

    private IEnumerator PlayRoutine()
    {
        while (true)
        {
            var anim = GetNextAnimation();
            if (anim == null)
                yield break;

            yield return PlayAnimationOnce(anim);

            if (replayDelaySeconds > 0f)
            {
                if (useUnscaledTime)
                    yield return new WaitForSecondsRealtime(replayDelaySeconds);
                else
                    yield return new WaitForSeconds(replayDelaySeconds);
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator PlayAnimationOnce(IdleAnimation animation)
    {
        if (animation == null || !animation.IsValid())
            yield break;

        var frames = animation.FramesArray;
        if (frames == null || frames.Length == 0)
            yield break;

        float fps = Mathf.Max(0.01f, animation.FrameRate);
        float frameDuration = 1f / fps;

        for (int i = 0; i < frames.Length; i++)
        {
            SetSprite(frames[i]);

            if (i >= frames.Length - 1)
                continue;

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(frameDuration);
            else
                yield return new WaitForSeconds(frameDuration);
        }
    }

    private IdleAnimation GetNextAnimation()
    {
        if (animations == null || animations.Length == 0)
            return null;

        if (!randomOrder)
        {
            int checkedCount = 0;
            while (checkedCount < animations.Length)
            {
                int index = orderedIndex % animations.Length;
                orderedIndex = (orderedIndex + 1) % animations.Length;
                checkedCount++;

                var candidate = animations[index].animation;
                if (candidate != null && candidate.IsValid())
                    return candidate;
            }

            return null;
        }

        return GetRandomAnimation();
    }

    private IdleAnimation GetRandomAnimation()
    {
        int validCount = CountValidAnimations();
        if (validCount == 0)
            return null;

        int chosenIndex = WeightedPickIndex(skipIndex: avoidImmediateRepeat ? lastRandomIndex : -1);

        if (chosenIndex < 0 && avoidImmediateRepeat)
            chosenIndex = WeightedPickIndex(skipIndex: -1);

        if (chosenIndex < 0)
            return null;

        lastRandomIndex = chosenIndex;
        return animations[chosenIndex].animation;
    }

    private int WeightedPickIndex(int skipIndex)
    {
        if (animations == null || animations.Length == 0)
            return -1;

        float totalWeight = 0f;
        for (int i = 0; i < animations.Length; i++)
        {
            if (i == skipIndex)
                continue;

            var candidate = animations[i].animation;
            if (candidate == null || !candidate.IsValid())
                continue;

            float w = animations[i].weight > 0f ? animations[i].weight : 1f;
            totalWeight += w;
        }

        if (totalWeight <= 0f)
            return -1;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < animations.Length; i++)
        {
            if (i == skipIndex)
                continue;

            var candidate = animations[i].animation;
            if (candidate == null || !candidate.IsValid())
                continue;

            float w = animations[i].weight > 0f ? animations[i].weight : 1f;
            cumulative += w;
            if (roll <= cumulative)
                return i;
        }

        return GetFirstValidIndex(skipIndex);
    }

    private int CountValidAnimations()
    {
        if (animations == null)
            return 0;

        int count = 0;
        for (int i = 0; i < animations.Length; i++)
        {
            var anim = animations[i].animation;
            if (anim != null && anim.IsValid())
                count++;
        }

        return count;
    }

    private int GetFirstValidIndex(int skipIndex = -1)
    {
        if (animations == null)
            return -1;

        for (int i = 0; i < animations.Length; i++)
        {
            if (i == skipIndex)
                continue;

            var anim = animations[i].animation;
            if (anim != null && anim.IsValid())
                return i;
        }

        return -1;
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRendererTarget != null)
            spriteRendererTarget.sprite = sprite;

        if (imageTarget != null)
            imageTarget.sprite = sprite;
    }

    private bool HasValidTarget()
    {
        return spriteRendererTarget != null || imageTarget != null;
    }

    private bool HasAnyValidAnimation()
    {
        return CountValidAnimations() > 0;
    }
}
