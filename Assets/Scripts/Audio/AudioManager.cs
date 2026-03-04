//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Audio\AudioManager.cs                                                             */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:37:11 by UDA                                                                    */
/*   Updated: 2026/01/23 01:37:11 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace UDA2.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;

        private const string MusicVolumeParam = "MusicVolume";
        private const string SfxVolumeParam = "SFXVolume";
        private const string UiVolumeParam = "UIVolume";

        [Header("Scene Music (Optional)")]
        [SerializeField] private SceneMusicConfig sceneMusicConfig;

        private AudioCue nextSceneMusicCue;

        private Coroutine scenePlaylistCoroutine;
        private int sceneMusicSessionId;

        /* ===================== AUDIO MIXER ===================== */

        [Header("Audio Mixer")]
        [SerializeField] private UnityEngine.Audio.AudioMixer audioMixer;

        /* ===================== MUSIC ===================== */

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup musicGroup;

        private AudioClip currentClip;
        private Coroutine musicFadeCoroutine;

        // Target music dB for fade logic
        private float targetMusicDb = 0f;

        /* ===================== SFX ===================== */

        [Header("SFX")]
        [SerializeField] private AudioSource sfxPrefab;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxGroup;
        [SerializeField] private int sfxPoolSize = 10;
        [SerializeField] private Transform sfxParent;

        private AudioSource[] sfxPool;
        private int sfxIndex;
        private float sfxVolume = 1f;

        /* ===================== UI ===================== */

        [Header("UI Audio")]
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup uiGroup;
        [SerializeField] private AudioCue uiClickCue;

        /* ===================== CHARACTER ===================== */
        [Header("Character Audio")]
        [SerializeField] private AudioSource characterSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup characterGroup;

        /* ===================== ENVIRONMENT ===================== */
        [Header("Environment Audio")]
        [SerializeField] private AudioSource environmentSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup environmentGroup;

        /* ===================== COMBAT ===================== */
        [Header("Combat Audio")]
        [SerializeField] private AudioSource combatSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup combatGroup;

        /* ===================== UNITY ===================== */

        private void Awake()
        {
            // Singleton
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsurePersistentAudioObjects();

            // Fail-fast mixer validation
            if (audioMixer == null)
            {
                Debug.LogWarning("AudioManager: audioMixer не назначен. Громкость через микшер работать не будет.");
            }
            else
            {
                CheckParam(MusicVolumeParam);
                CheckParam(SfxVolumeParam);
                CheckParam(UiVolumeParam);
            }

            // Music source
            if (musicSource != null && musicGroup != null)
            {
                musicSource.outputAudioMixerGroup = musicGroup;
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.volume = 1f;
            }

            // UI source (один, без пула)
            if (uiSource != null && uiGroup != null)
            {
                uiSource.outputAudioMixerGroup = uiGroup;
                uiSource.playOnAwake = false;
                uiSource.volume = 1f;
            }

            // SFX pool
            InitSfxPool();

            // Load settings
            var s = UDA2.Core.SettingsContext.Current;
            SetMusicVolume(s != null ? s.musicVolume : 1f);
            SetSfxVolume(s != null ? s.sfxVolume : 1f);
            SetUiVolume(s != null ? s.uiVolume : 1f);

            if (musicSource == null)
                Debug.LogWarning("AudioManager: musicSource не назначен в инспекторе — музыку не будет слышно.");
        }

        private void EnsurePersistentAudioObjects()
        {
            // If these references point to objects from a loaded scene, they will be destroyed on scene change,
            // while AudioManager persists (DontDestroyOnLoad). Keep them under AudioManager to avoid stale refs.
            if (musicSource != null && !musicSource.transform.IsChildOf(transform))
                musicSource.transform.SetParent(transform, false);

            if (uiSource != null && !uiSource.transform.IsChildOf(transform))
                uiSource.transform.SetParent(transform, false);

            if (sfxParent != null && !sfxParent.IsChildOf(transform))
                sfxParent.SetParent(transform, false);

            if (sfxParent == null)
            {
                var go = new GameObject("SFX");
                go.transform.SetParent(transform, false);
                sfxParent = go.transform;
            }
        }

        private void EnsureSfxPool()
        {
            if (sfxPrefab == null)
                return;

            EnsurePersistentAudioObjects();

            if (sfxPool == null || sfxPool.Length != sfxPoolSize)
            {
                InitSfxPool();
                return;
            }

            for (int i = 0; i < sfxPool.Length; i++)
            {
                if (sfxPool[i] == null)
                {
                    InitSfxPool();
                    return;
                }
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            UDA2.Core.SettingsContext.OnMusicVolumeChanged += SetMusicVolume;
            UDA2.Core.SettingsContext.OnSfxVolumeChanged += SetSfxVolume;
            UDA2.Core.SettingsContext.OnUiVolumeChanged += SetUiVolume;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            UDA2.Core.SettingsContext.OnMusicVolumeChanged -= SetMusicVolume;
            UDA2.Core.SettingsContext.OnSfxVolumeChanged -= SetSfxVolume;
            UDA2.Core.SettingsContext.OnUiVolumeChanged -= SetUiVolume;
        }

        private void OnDestroy()
        {
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
                musicFadeCoroutine = null;
            }

            if (Instance == this)
                Instance = null;
        }

        /* ===================== SCENE MUSIC ===================== */

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StopSceneSounds();
            ApplySceneMusic(scene);
        }

        private void ApplySceneMusic(Scene scene)
        {
            // New scene = new music session. Cancels any running playlist.
            sceneMusicSessionId++;
            StopScenePlaylist();

            // 1) Highest priority: explicit override set by loader BEFORE scene load.
            if (nextSceneMusicCue != null)
            {
                var cue = nextSceneMusicCue;
                nextSceneMusicCue = null;
                Play(cue);
                return;
            }

            // 2) Config asset mapping scene -> cue
            if (sceneMusicConfig != null && sceneMusicConfig.TryGet(scene.name, out var entry))
            {
                if (HasAnyValidCue(entry.playlist))
                {
                    StartScenePlaylist(entry.playlist, entry.loopPlaylist, sceneMusicSessionId, entry.musicCue);
                    return;
                }

                if (entry.musicCue != null)
                {
                    Play(entry.musicCue);
                    return;
                }
            }

            StopMusic();
        }

        private void StartScenePlaylist(AudioCue[] playlist, bool loop, int sessionId, AudioCue fallbackCue)
        {
            if (playlist == null || playlist.Length == 0)
                return;

            if (musicSource == null)
            {
                Debug.LogWarning("AudioManager: musicSource не назначен — playlist пропущен.");
                return;
            }

            StopScenePlaylist();
            scenePlaylistCoroutine = StartCoroutine(ScenePlaylistRoutine(playlist, loop, sessionId, fallbackCue));
        }

        private void StopScenePlaylist()
        {
            if (scenePlaylistCoroutine == null)
                return;

            StopCoroutine(scenePlaylistCoroutine);
            scenePlaylistCoroutine = null;
        }

        private IEnumerator ScenePlaylistRoutine(AudioCue[] playlist, bool loop, int sessionId, AudioCue fallbackCue)
        {
            int index = 0;

            // If the scene has more than one music track, start from a random one.
            // (Avoids hearing the same first track every time the scene loads.)
            if (playlist != null && playlist.Length > 1)
                index = UnityEngine.Random.Range(0, playlist.Length);

            while (sessionId == sceneMusicSessionId)
            {
                // Find next valid cue.
                AudioCue cue = null;
                int attempts = 0;
                while (attempts < playlist.Length)
                {
                    var candidate = playlist[index];
                    if (candidate != null && candidate.Clip != null)
                    {
                        cue = candidate;
                        break;
                    }

                    index = (index + 1) % playlist.Length;
                    attempts++;
                }

                if (cue == null)
                {
                    if (fallbackCue != null && fallbackCue.Clip != null)
                        Play(fallbackCue);
                    yield break;
                }

                PlayMusic(cue.Clip, loop: false);

                // Give AudioSource at least one frame to transition into playing state.
                // Without this, focus/background edge-cases can cause a tight loop with high CPU.
                yield return null;

                // Wait for clip to end (or until cancelled).
                while (sessionId == sceneMusicSessionId && musicSource != null && musicSource.isPlaying)
                    yield return null;

                // If clip did not start playing at all (e.g., app unfocused / audio suspended),
                // throttle retries to avoid spinning the playlist loop.
                if (sessionId == sceneMusicSessionId && musicSource != null && !musicSource.isPlaying)
                    yield return new WaitForSecondsRealtime(0.2f);

                if (sessionId != sceneMusicSessionId)
                    yield break;

                index++;
                if (index >= playlist.Length)
                {
                    if (!loop)
                        yield break;
                    index = 0;
                }
            }
        }

        public void SetNextSceneMusic(AudioCue cue)
        {
            nextSceneMusicCue = cue;
        }

        public void Play(AudioCue cue)
        {
            if (cue == null || cue.Clip == null)
                return;

            if (cue.Category == AudioCategory.Music)
            {
                PlayMusic(cue.Clip);
                return;
            }

            float pitch = cue.PitchRange.x;
            if (cue.PitchRange.y > cue.PitchRange.x)
                pitch = UnityEngine.Random.Range(cue.PitchRange.x, cue.PitchRange.y);

            if (cue.Category == AudioCategory.Ui)
            {
                if (uiSource == null)
                {
                    Debug.LogWarning("AudioManager: uiSource не назначен — UI звук пропущен.");
                    return;
                }

                float prevPitch = uiSource.pitch;
                uiSource.pitch = pitch;
                uiSource.PlayOneShot(cue.Clip, cue.DefaultVolume);
                uiSource.pitch = prevPitch;
                return;
            }

            // Gameplay sounds (Sound) and legacy Sfx both use the pooled SFX player.
            PlaySfx(cue.Clip, cue.DefaultVolume, pitch);
        }

        /* ===================== MUSIC ===================== */

        public void PlayMusic(AudioClip clip)
        {
            PlayMusic(clip, loop: true);
        }

        public void PlayMusic(AudioClip clip, bool loop)
        {
            if (clip == null)
                return;

            // If we are already playing this clip, do nothing. But if the clip finished (or was stopped),
            // allow restarting it (important for scene playlists that loop back to the same track).
            if (clip == currentClip && musicSource != null && musicSource.isPlaying)
                return;

            if (musicSource == null)
            {
                Debug.LogWarning("AudioManager: musicSource не назначен — PlayMusic пропущен.");
                return;
            }

            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);

            currentClip = clip;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicFadeCoroutine = StartCoroutine(FadeMusicIn());
        }

        private static bool HasAnyValidCue(AudioCue[] cues)
        {
            if (cues == null || cues.Length == 0)
                return false;

            for (int i = 0; i < cues.Length; i++)
            {
                var cue = cues[i];
                if (cue != null && cue.Clip != null)
                    return true;
            }

            return false;
        }

        private IEnumerator FadeMusicIn()
        {
            float startDb = -80f;
            if (audioMixer != null)
                audioMixer.SetFloat(MusicVolumeParam, startDb);
            musicSource.Play();

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                if (audioMixer != null)
                    audioMixer.SetFloat(MusicVolumeParam, Mathf.Lerp(startDb, targetMusicDb, t));
                yield return null;
            }
            // Ensure final value is set exactly
            if (audioMixer != null)
                audioMixer.SetFloat(MusicVolumeParam, targetMusicDb);
        }

        public void StopMusic()
        {
            StopScenePlaylist();

            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);

            musicSource.Stop();
            currentClip = null;
        }

        // 🔹 SettingsController СОВМЕСТИМОСТЬ
        public void SetMusicVolume(float volume, float fadeTime)
        {
            SetMusicVolume(volume);
        }

        public void SetMusicVolume(float volume)
        {
            targetMusicDb = ToDb(volume);
            if (audioMixer != null)
                audioMixer.SetFloat(MusicVolumeParam, targetMusicDb);
        }

        /* ===================== SFX ===================== */

        private void InitSfxPool()
        {
            if (sfxPrefab == null)
                return;

            sfxPool = new AudioSource[sfxPoolSize];

            for (int i = 0; i < sfxPoolSize; i++)
            {
                var src = Instantiate(sfxPrefab, sfxParent);
                src.outputAudioMixerGroup = sfxGroup;
                src.playOnAwake = false;
                src.volume = 1f;
                sfxPool[i] = src;
            }
        }

        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null)
                return;

            EnsureSfxPool();

            if (sfxPool == null)
                return;

            var src = sfxPool[sfxIndex];
            sfxIndex = (sfxIndex + 1) % sfxPool.Length;

            src.Stop();
            src.clip = clip;
            src.pitch = pitch;
            src.volume = Mathf.Clamp01(volume) * sfxVolume;
            src.Play();
        }

        // 🔹 SettingsController СОВМЕСТИМОСТЬ
        public void SetSfxVolume(float volume, float fadeTime)
        {
            SetSfxVolume(volume);
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (audioMixer != null)
                audioMixer.SetFloat(SfxVolumeParam, ToDb(sfxVolume));
        }

        /* ===================== UI ===================== */

        public void PlayUiClick()
        {
            if (uiClickCue != null)
            {
                Play(uiClickCue);
                return;
            }

            Debug.LogWarning("AudioManager: uiClickCue не назначен (UI click звук пропущен).");
        }

        public void SetUiVolume(float volume)
        {
            if (audioMixer != null)
                audioMixer.SetFloat(UiVolumeParam, ToDb(volume));
        }

        public void StopSceneSounds()
        {
            if (sfxPool != null)
            {
                for (int i = 0; i < sfxPool.Length; i++)
                {
                    var src = sfxPool[i];
                    if (src == null)
                        continue;

                    src.Stop();
                    src.clip = null;
                }
            }

            if (characterSource != null)
                characterSource.Stop();

            if (environmentSource != null)
                environmentSource.Stop();

            if (combatSource != null)
                combatSource.Stop();
        }

        /* ===================== UTILS ===================== */

        private void CheckParam(string name)
        {
            if (audioMixer == null)
                return;

            if (!audioMixer.GetFloat(name, out _))
                Debug.LogError($"AudioMixer missing exposed parameter: {name}");
        }

        private float ToDb(float v)
        {
            return Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f;
        }
    }
}
