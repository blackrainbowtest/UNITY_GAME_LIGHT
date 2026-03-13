using System;
using System.Collections;
using UnityEngine;

namespace UDA2.Audio
{
    [DisallowMultipleComponent]
    public sealed class SceneAmbientSoundController : MonoBehaviour
    {
        [Serializable]
        public sealed class AmbientGroup
        {
            [Tooltip("Optional label for readability in Inspector.")]
            public string name;

            [Tooltip("If enabled, group keeps playing random sounds forever using interval range.")]
            public bool randomLoop = true;

            [Tooltip("Delay range between plays in seconds (min/max).")]
            public Vector2 intervalSeconds = new Vector2(3f, 5f);

            [Tooltip("Preferred source list. Every cue should usually have Category=Sound.")]
            public AudioCue[] cues = Array.Empty<AudioCue>();

            [Tooltip("Optional fallback list if cues are not used.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();

            [Range(0f, 1f)]
            [Tooltip("Volume multiplier for clip fallback playback.")]
            public float clipVolume = 1f;

            [Tooltip("Pitch range for clip fallback playback.")]
            public Vector2 clipPitchRange = new Vector2(1f, 1f);

            public bool HasAnyPlayable()
            {
                if (cues != null)
                {
                    for (int i = 0; i < cues.Length; i++)
                    {
                        if (cues[i] != null && cues[i].Clip != null)
                            return true;
                    }
                }

                if (clips != null)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        if (clips[i] != null)
                            return true;
                    }
                }

                return false;
            }
        }

        [Header("Behavior")]
        [SerializeField] private bool playOnEnable = true;

        [Tooltip("Deprecated. Music cues are always ignored in ambient groups.")]
        [SerializeField] private bool allowMusicCues;

        [Tooltip("If true, first sound in each group is delayed by its interval range. If false, plays immediately once.")]
        [SerializeField] private bool delayFirstPlay = false;

        [Header("Ambient Groups")]
        [SerializeField] private AmbientGroup[] groups = Array.Empty<AmbientGroup>();

        private Coroutine[] groupRoutines;
        private Coroutine delayedPlayRoutine;

        private void OnEnable()
        {
            if (playOnEnable)
                delayedPlayRoutine = StartCoroutine(PlayNextFrameRoutine());
        }

        private void OnDisable()
        {
            if (delayedPlayRoutine != null)
            {
                StopCoroutine(delayedPlayRoutine);
                delayedPlayRoutine = null;
            }

            Stop();
        }

        private IEnumerator PlayNextFrameRoutine()
        {
            yield return null;

            delayedPlayRoutine = null;

            if (enabled && gameObject.activeInHierarchy)
                Play();
        }

        public void Play()
        {
            Stop();

            if (allowMusicCues)
                Debug.LogWarning("SceneAmbientSoundController: Music cues are ignored in ambient groups to protect the music channel.");

            if (groups == null || groups.Length == 0)
                return;

            groupRoutines = new Coroutine[groups.Length];
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                if (group == null || !group.HasAnyPlayable())
                    continue;

                if (!group.randomLoop)
                {
                    PlayOneAndGetDuration(group, out _);
                    continue;
                }

                groupRoutines[i] = StartCoroutine(GroupRoutine(group));
            }
        }

        public void Stop()
        {
            if (groupRoutines == null)
                return;

            for (int i = 0; i < groupRoutines.Length; i++)
            {
                if (groupRoutines[i] != null)
                    StopCoroutine(groupRoutines[i]);
            }

            groupRoutines = null;
        }

        private IEnumerator GroupRoutine(AmbientGroup group)
        {
            if (delayFirstPlay)
                yield return new WaitForSeconds(GetDelay(group.intervalSeconds));

            while (enabled && gameObject.activeInHierarchy)
            {
                PlayOneAndGetDuration(group, out float playedDuration);

                if (playedDuration > 0f)
                {
                    yield return new WaitForSeconds(playedDuration);
                }

                float startDelay = GetDelay(group.intervalSeconds);
                if (startDelay > 0f)
                    yield return new WaitForSeconds(startDelay);
                else
                    yield return null;
            }
        }

        private static float GetDelay(Vector2 range)
        {
            float min = Mathf.Max(0f, Mathf.Min(range.x, range.y));
            float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
            if (Mathf.Approximately(min, max))
                return min;
            return UnityEngine.Random.Range(min, max);
        }

        private static AudioCue PickRandomCue(AudioCue[] list)
        {
            if (list == null || list.Length == 0)
                return null;

            int start = UnityEngine.Random.Range(0, list.Length);
            for (int i = 0; i < list.Length; i++)
            {
                var cue = list[(start + i) % list.Length];
                if (cue == null || cue.Clip == null)
                    continue;

                if (cue.Category == AudioCategory.Music)
                    continue;

                if (cue != null && cue.Clip != null)
                    return cue;
            }

            return null;
        }

        private static AudioClip PickRandomClip(AudioClip[] list)
        {
            if (list == null || list.Length == 0)
                return null;

            int start = UnityEngine.Random.Range(0, list.Length);
            for (int i = 0; i < list.Length; i++)
            {
                var clip = list[(start + i) % list.Length];
                if (clip != null)
                    return clip;
            }

            return null;
        }

        private static bool PlayOneAndGetDuration(AmbientGroup group, out float durationSeconds)
        {
            durationSeconds = 0f;

            var am = AudioManager.Instance;
            if (am == null)
                return false;

            var cue = PickRandomCue(group.cues);
            if (cue != null)
            {
                float minPitch = cue.PitchRange.x;
                float maxPitch = cue.PitchRange.y;
                if (maxPitch < minPitch)
                    (minPitch, maxPitch) = (maxPitch, minPitch);

                float pitch = Mathf.Approximately(minPitch, maxPitch)
                    ? minPitch
                    : UnityEngine.Random.Range(minPitch, maxPitch);

                am.PlayAmbient(cue.Clip, cue.DefaultVolume, pitch);

                durationSeconds = cue.Clip != null ? Mathf.Max(0f, cue.Clip.length) : 0f;
                return false;
            }

            var clip = PickRandomClip(group.clips);
            if (clip == null)
                return false;

            float clipMinPitch = group.clipPitchRange.x;
            float clipMaxPitch = group.clipPitchRange.y;
            if (clipMaxPitch < clipMinPitch)
                (clipMinPitch, clipMaxPitch) = (clipMaxPitch, clipMinPitch);

            float clipPitch = Mathf.Approximately(clipMinPitch, clipMaxPitch)
                ? clipMinPitch
                : UnityEngine.Random.Range(clipMinPitch, clipMaxPitch);

            am.PlayAmbient(clip, group.clipVolume, clipPitch);
            durationSeconds = Mathf.Max(0f, clip.length);
            return false;
        }
    }
}
