using UnityEngine;
using System.Collections;
using UDA2.Audio;

namespace Game.Battle
{
    /// <summary>
    /// Applies battle environment visuals and audio based on BattleLocationData.
    /// </summary>
    public class BattleEnvironmentController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private SpriteRenderer backgroundRenderer;

        private Coroutine musicPlaylistRoutine;
        private Coroutine[] ambientRoutines;


        public void Apply(BattleLocationData location)
        {
            if (location == null)
            {
                Debug.LogError("BattleEnvironmentController: Location is null");
                return;
            }

            ApplyBackground(location);
            ApplyMusic(location);
            ApplyAmbient(location);
        }

        private void OnDisable()
        {
            StopAllLocationAudioRoutines();
        }

        private void ApplyBackground(BattleLocationData location)
        {
            if (backgroundRenderer == null)
                return;

            backgroundRenderer.sprite = location.background;
        }

        private void ApplyMusic(BattleLocationData location)
        {
            // Используем глобальный AudioManager для музыки
            if (UDA2.Audio.AudioManager.Instance == null)
            {
                Debug.LogWarning("BattleEnvironmentController: AudioManager.Instance is null. Skipping music setup (likely running battle scene directly). ");
                return;
            }

            StopMusicPlaylistRoutine();

            if (location.HasMusicPlaylist())
            {
                musicPlaylistRoutine = StartCoroutine(MusicPlaylistRoutine(location));
                return;
            }

            if (location.musicCue != null)
            {
                UDA2.Audio.AudioManager.Instance.Play(location.musicCue);
                return;
            }

            if (location.music != null)
            {
                UDA2.Audio.AudioManager.Instance.PlayMusic(location.music);
                return;
            }

            UDA2.Audio.AudioManager.Instance.StopMusic();
        }

        private void ApplyAmbient(BattleLocationData location)
        {
            StopAmbientRoutines();

            var groups = location.ambientSoundGroups;
            if (groups == null || groups.Length == 0)
                return;

            ambientRoutines = new Coroutine[groups.Length];

            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                if (group == null || !group.HasAnyPlayable())
                    continue;

                if (!group.randomLoop)
                {
                    PlayAmbientOne(group);
                    continue;
                }

                ambientRoutines[i] = StartCoroutine(AmbientGroupRoutine(group));
            }
        }

        private void StopAllLocationAudioRoutines()
        {
            StopMusicPlaylistRoutine();
            StopAmbientRoutines();
        }

        private void StopMusicPlaylistRoutine()
        {
            if (musicPlaylistRoutine == null)
                return;

            StopCoroutine(musicPlaylistRoutine);
            musicPlaylistRoutine = null;
        }

        private void StopAmbientRoutines()
        {
            if (ambientRoutines == null)
                return;

            for (int i = 0; i < ambientRoutines.Length; i++)
            {
                if (ambientRoutines[i] != null)
                    StopCoroutine(ambientRoutines[i]);
            }

            ambientRoutines = null;
        }

        private IEnumerator MusicPlaylistRoutine(BattleLocationData location)
        {
            var am = AudioManager.Instance;
            if (am == null)
                yield break;

            int index = 0;
            if (location.randomStartMusicPlaylist)
            {
                int total = GetMusicPlaylistCount(location);
                if (total > 1)
                    index = Random.Range(0, total);
            }

            while (enabled && gameObject.activeInHierarchy)
            {
                if (!TryPlayMusicAt(location, index, out _))
                    yield break;

                while (enabled && gameObject.activeInHierarchy)
                {
                    var current = AudioManager.Instance;
                    if (current == null || !current.IsMusicPlaying)
                        break;
                    yield return null;
                }

                float delay = GetDelay(location.musicTrackStartDelaySeconds);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
                else
                    yield return null;

                index++;
                int count = GetMusicPlaylistCount(location);
                if (count <= 0)
                    yield break;

                if (index >= count)
                {
                    if (!location.loopMusicPlaylist)
                        yield break;

                    if (location.randomStartMusicPlaylist && count > 1)
                        index = Random.Range(0, count);
                    else
                        index = 0;
                }
            }
        }

        private static int GetMusicPlaylistCount(BattleLocationData location)
        {
            int cueCount = location.musicPlaylist != null ? location.musicPlaylist.Length : 0;
            int clipCount = location.musicPlaylistClips != null ? location.musicPlaylistClips.Length : 0;
            return cueCount + clipCount;
        }

        private static bool TryPlayMusicAt(BattleLocationData location, int logicalIndex, out float duration)
        {
            duration = 0f;
            var am = AudioManager.Instance;
            if (am == null)
                return false;

            int cueCount = location.musicPlaylist != null ? location.musicPlaylist.Length : 0;

            if (logicalIndex < cueCount)
            {
                var cue = location.musicPlaylist[logicalIndex];
                if (cue == null || cue.Clip == null)
                    return false;

                am.PlayMusic(cue.Clip, loop: false);
                duration = Mathf.Max(0f, cue.Clip.length);
                return true;
            }

            int clipIndex = logicalIndex - cueCount;
            if (location.musicPlaylistClips == null || clipIndex < 0 || clipIndex >= location.musicPlaylistClips.Length)
                return false;

            var clip = location.musicPlaylistClips[clipIndex];
            if (clip == null)
                return false;

            am.PlayMusic(clip, loop: false);
            duration = Mathf.Max(0f, clip.length);
            return true;
        }

        private IEnumerator AmbientGroupRoutine(BattleLocationData.AmbientSoundGroup group)
        {
            while (enabled && gameObject.activeInHierarchy)
            {
                float playedDuration = PlayAmbientOne(group);
                float delay = GetDelay(group.intervalSeconds);
                float wait = Mathf.Max(0f, playedDuration + delay);

                if (wait > 0f)
                    yield return new WaitForSeconds(wait);
                else
                    yield return null;
            }
        }

        private static float PlayAmbientOne(BattleLocationData.AmbientSoundGroup group)
        {
            var am = AudioManager.Instance;
            if (am == null)
                return 0f;

            var cue = PickRandomCue(group.cues);
            if (cue != null)
            {
                am.Play(cue);
                return cue.Clip != null ? Mathf.Max(0f, cue.Clip.length) : 0f;
            }

            var clip = PickRandomClip(group.clips);
            if (clip == null)
                return 0f;

            float minPitch = group.clipPitchRange.x;
            float maxPitch = group.clipPitchRange.y;
            if (maxPitch < minPitch)
                (minPitch, maxPitch) = (maxPitch, minPitch);

            float pitch = Mathf.Approximately(minPitch, maxPitch)
                ? minPitch
                : Random.Range(minPitch, maxPitch);

            am.PlaySfx(clip, group.clipVolume, pitch);
            return Mathf.Max(0f, clip.length);
        }

        private static AudioCue PickRandomCue(AudioCue[] list)
        {
            if (list == null || list.Length == 0)
                return null;

            int start = Random.Range(0, list.Length);
            for (int i = 0; i < list.Length; i++)
            {
                var cue = list[(start + i) % list.Length];
                if (cue != null && cue.Clip != null)
                    return cue;
            }

            return null;
        }

        private static AudioClip PickRandomClip(AudioClip[] list)
        {
            if (list == null || list.Length == 0)
                return null;

            int start = Random.Range(0, list.Length);
            for (int i = 0; i < list.Length; i++)
            {
                var clip = list[(start + i) % list.Length];
                if (clip != null)
                    return clip;
            }

            return null;
        }

        private static float GetDelay(Vector2 range)
        {
            float min = Mathf.Max(0f, Mathf.Min(range.x, range.y));
            float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
            if (Mathf.Approximately(min, max))
                return min;
            return Random.Range(min, max);
        }
    }
}
