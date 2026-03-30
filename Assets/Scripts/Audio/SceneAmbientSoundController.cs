using System;
using System.Collections;
using System.Collections.Generic;
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

            [Header("Stereo Pan")]
            [Tooltip("Enable stereo pan control for this group (-1 = left, +1 = right).")]
            public bool useStereoPan;

            [Range(-1f, 1f)]
            [Tooltip("Pan value at playback start.")]
            public float panStart = 0f;

            [Range(-1f, 1f)]
            [Tooltip("Pan value at playback end.")]
            public float panEnd = 0f;

            [Tooltip("If enabled, pan sweep duration equals current clip length.")]
            public bool panSweepOverClipDuration;

            [Min(0f)]
            [Tooltip("Manual pan sweep duration in seconds (used when 'panSweepOverClipDuration' is disabled).")]
            public float panSweepDuration = 0f;

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

        [Tooltip("If enabled, random-loop groups start with small stagger to avoid audio spikes on scene entry.")]
        [SerializeField] private bool staggerGroupStartup = true;

        [Min(0f)]
        [Tooltip("Extra delay between random-loop groups startup (seconds).")]
        [SerializeField] private float groupStartupStepSeconds = 0.08f;

        [Tooltip("If enabled, requests audio data load for ambient clips before starting playback.")]
        [SerializeField] private bool preloadAmbientAudioData = true;

        [Min(1)]
        [Tooltip("How many clips to schedule for preloading per frame.")]
        [SerializeField] private int preloadClipsPerFrame = 2;

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

            if (preloadAmbientAudioData)
                yield return PreloadAmbientClipsRoutine();

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

                float startupDelay = 0f;
                if (staggerGroupStartup)
                    startupDelay = Mathf.Max(0f, groupStartupStepSeconds) * i;

                groupRoutines[i] = StartCoroutine(GroupRoutine(group, startupDelay));
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

        private IEnumerator GroupRoutine(AmbientGroup group, float initialDelay)
        {
            if (initialDelay > 0f)
                yield return new WaitForSeconds(initialDelay);

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

                float clipDuration = cue.Clip != null ? Mathf.Max(0f, cue.Clip.length) : 0f;
                float panStart = group.useStereoPan ? Mathf.Clamp(group.panStart, -1f, 1f) : 0f;
                float panEnd = group.useStereoPan ? Mathf.Clamp(group.panEnd, -1f, 1f) : panStart;
                float panSweepDuration = 0f;
                if (group.useStereoPan)
                {
                    panSweepDuration = group.panSweepOverClipDuration
                        ? clipDuration
                        : Mathf.Max(0f, group.panSweepDuration);
                }

                am.PlayAmbient(cue.Clip, cue.DefaultVolume, pitch, panStart, panEnd, panSweepDuration);

                durationSeconds = clipDuration;
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

            float clipDurationFallback = Mathf.Max(0f, clip.length);
            float clipPanStart = group.useStereoPan ? Mathf.Clamp(group.panStart, -1f, 1f) : 0f;
            float clipPanEnd = group.useStereoPan ? Mathf.Clamp(group.panEnd, -1f, 1f) : clipPanStart;
            float clipPanSweepDuration = 0f;
            if (group.useStereoPan)
            {
                clipPanSweepDuration = group.panSweepOverClipDuration
                    ? clipDurationFallback
                    : Mathf.Max(0f, group.panSweepDuration);
            }

            am.PlayAmbient(clip, group.clipVolume, clipPitch, clipPanStart, clipPanEnd, clipPanSweepDuration);
            durationSeconds = clipDurationFallback;
            return false;
        }

        private IEnumerator PreloadAmbientClipsRoutine()
        {
            if (groups == null || groups.Length == 0)
                yield break;

            var unique = new HashSet<AudioClip>();
            int scheduledThisFrame = 0;
            int budget = Mathf.Max(1, preloadClipsPerFrame);

            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                if (group == null)
                    continue;

                if (group.cues != null)
                {
                    for (int c = 0; c < group.cues.Length; c++)
                    {
                        var cue = group.cues[c];
                        if (cue == null || cue.Clip == null)
                            continue;

                        if (!unique.Add(cue.Clip))
                            continue;

                        cue.Clip.LoadAudioData();
                        scheduledThisFrame++;

                        if (scheduledThisFrame >= budget)
                        {
                            scheduledThisFrame = 0;
                            yield return null;
                        }
                    }
                }

                if (group.clips != null)
                {
                    for (int c = 0; c < group.clips.Length; c++)
                    {
                        var clip = group.clips[c];
                        if (clip == null)
                            continue;

                        if (!unique.Add(clip))
                            continue;

                        clip.LoadAudioData();
                        scheduledThisFrame++;

                        if (scheduledThisFrame >= budget)
                        {
                            scheduledThisFrame = 0;
                            yield return null;
                        }
                    }
                }
            }
        }
    }
}
