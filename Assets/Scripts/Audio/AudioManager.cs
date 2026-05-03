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
        public enum BattleCueRoute
        {
            Sfx = 0,
            Character = 1,
            Environment = 2,
            Combat = 3,
        }

        public static AudioManager Instance;

        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
        public float SfxVolume01 => sfxVolume;

        private const string MusicVolumeParam = "MusicVolume";
        private const string SfxVolumeParam = "SFXVolume";
        private const string AmbientVolumeParam = "AmbientVolume";
        private const string UiVolumeParam = "UIVolume";

        [Header("Музыка Сцены (Опционально)")]
        [SerializeField] private SceneMusicConfig sceneMusicConfig;

        private AudioCue nextSceneMusicCue;

        private Coroutine scenePlaylistCoroutine;
        private int sceneMusicSessionId;

        /* ===================== AUDIO MIXER ===================== */

        [Header("Аудио Микшер")]
        [SerializeField] private UnityEngine.Audio.AudioMixer audioMixer;
        [Header("Кривая Слайдера Громкости")]
        [SerializeField, Range(-60f, -20f)] private float sliderMinDb = -40f;
        [SerializeField, Range(0.5f, 2.5f)] private float sliderCurve = 1f;
        [SerializeField, Range(-80f, 0f)] private float sfxSliderMinDb = -80f;

        /* ===================== MUSIC ===================== */

        [Header("Музыка")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup musicGroup;
        [Header("Переходы Музыки")]
        [SerializeField, Min(0f)] private float defaultMusicFadeInSeconds = 0.3f;
        [SerializeField, Min(0f)] private float defaultMusicFadeOutSeconds = 0.12f;

        private AudioClip currentClip;
        private Coroutine musicFadeCoroutine;

        // Target music dB for fade logic
        private float targetMusicDb = 0f;
        private bool hasSfxVolumeParam;
        private bool hasAmbientVolumeParam;

        /* ===================== SFX ===================== */

        [Header("SFX")]
        [SerializeField] private AudioSource sfxPrefab;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxGroup;
        [SerializeField] private int sfxPoolSize = 10;
        [SerializeField] private Transform sfxParent;

        private AudioSource[] sfxPool;
        private int sfxIndex;
        private float sfxVolume = 1f;

        [Header("Амбиент")]
        [SerializeField] private AudioSource ambientPrefab;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup ambientGroup;
        [SerializeField] private int ambientPoolSize = 6;
        [SerializeField] private Transform ambientParent;
        [SerializeField, Range(0f, 1f)] private float ambientGlobalVolumeScale = 0.35f;
        [SerializeField, Range(-80f, -20f)] private float ambientCeilingDb = -20f;

        private AudioSource[] ambientPool;
        private Coroutine[] ambientPanCoroutines;
        private float[] ambientBaseVolumes;
        private int ambientIndex;

        private float sliderCurveSafe = 1f;
        private float ambientCeilingGain = 0.1f;
        private float battleCharacterCeilingGain = 0.03162278f;
        private float battleCombatCeilingGain = 0.03162278f;

        /* ===================== UI ===================== */

        [Header("UI Звук")]
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup uiGroup;
        [SerializeField] private AudioCue uiClickCue;

        /* ===================== CHARACTER ===================== */
        [Header("Звук Персонажа")]
        [SerializeField] private AudioSource characterSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup characterGroup;

        /* ===================== ENVIRONMENT ===================== */
        [Header("Звук Окружения")]
        [SerializeField] private AudioSource environmentSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup environmentGroup;

        /* ===================== COMBAT ===================== */
        [Header("Боевой Звук")]
        [SerializeField] private AudioSource combatSource;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup combatGroup;

        [Header("Громкость Маршрутов Боевых Cue")]
        [SerializeField, Range(0f, 1f)] private float battleSfxRouteVolumeScale = 1f;
        [SerializeField, Range(0f, 1f)] private float battleCharacterRouteVolumeScale = 0.6f;
        [SerializeField, Range(0f, 1f)] private float battleEnvironmentRouteVolumeScale = 0.75f;
        [SerializeField, Range(0f, 1f)] private float battleCombatRouteVolumeScale = 1f;
        [SerializeField, Range(-80f, 0f)] private float battleCharacterCeilingDb = -20f;
        [SerializeField, Range(-80f, 0f)] private float battleCombatCeilingDb = -20f;

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

            RefreshDerivedAudioParams();

            EnsurePersistentAudioObjects();

            // Fail-fast mixer validation
            if (audioMixer == null)
            {
                Debug.LogWarning("AudioManager: audioMixer не назначен. Громкость через микшер работать не будет.");
            }
            else
            {
                CheckParam(MusicVolumeParam);
                hasSfxVolumeParam = audioMixer.GetFloat(SfxVolumeParam, out _);
                if (!hasSfxVolumeParam)
                    Debug.LogWarning("AudioManager: AudioMixer exposed parameter 'SFXVolume' not found. Using source-volume fallback for SFX.");
                hasAmbientVolumeParam = audioMixer.GetFloat(AmbientVolumeParam, out _);
                if (!hasAmbientVolumeParam)
                    Debug.LogWarning("AudioManager: AudioMixer exposed parameter 'AmbientVolume' not found. Using source-volume fallback for ambient.");
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

            // Load settings snapshot with fallback to persisted values when context is not initialized yet.
            var s = ResolveSettingsSnapshot();
            SetMusicVolume(s.musicVolume);
            SetSfxVolume(s.sfxVolume);
            SetUiVolume(s.uiVolume);

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

            if (ambientParent != null && !ambientParent.IsChildOf(transform))
                ambientParent.SetParent(transform, false);

            if (sfxParent == null)
            {
                var go = new GameObject("SFX");
                go.transform.SetParent(transform, false);
                sfxParent = go.transform;
            }

            if (ambientParent == null)
            {
                var go = new GameObject("Ambient");
                go.transform.SetParent(transform, false);
                ambientParent = go.transform;
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

        private void EnsureAmbientPool()
        {
            if (ambientPrefab == null)
                return;

            EnsurePersistentAudioObjects();

            if (ambientPool == null || ambientPool.Length != ambientPoolSize)
            {
                InitAmbientPool();
                return;
            }

            for (int i = 0; i < ambientPool.Length; i++)
            {
                if (ambientPool[i] == null)
                {
                    InitAmbientPool();
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

            // Pull latest snapshot immediately in case ApplyAll happened before subscriptions.
            ApplySettingsSnapshotFromContext();
        }

        private void OnValidate()
        {
            RefreshDerivedAudioParams();
        }

        private void Start()
        {
            // A second pass after scene startup protects against late Current assignment on bootstrap.
            StartCoroutine(ApplySettingsSnapshotNextFrame());
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

        private IEnumerator ApplySettingsSnapshotNextFrame()
        {
            yield return null;
            ApplySettingsSnapshotFromContext();
        }

        private void ApplySettingsSnapshotFromContext()
        {
            var s = ResolveSettingsSnapshot();

            SetMusicVolume(s.musicVolume);
            SetSfxVolume(s.sfxVolume);
            SetUiVolume(s.uiVolume);
        }

        private static UDA2.Core.SettingsState ResolveSettingsSnapshot()
        {
            var current = UDA2.Core.SettingsContext.Current;
            if (current != null)
                return current;

            var loaded = UDA2.Core.SettingsManager.Load();
            UDA2.Core.SettingsContext.Current = loaded;
            return loaded;
        }

        /* ===================== SCENE MUSIC ===================== */

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // LoadingScene is a persistent UI overlay loaded additively at startup and
            // kept alive for the entire session. It is NOT a gameplay scene, so it must
            // never reset scene sounds or trigger music changes.
            if (scene.name == "LoadingScene")
                return;

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

        public bool WillPlayMusicForScene(string sceneName)
        {
            if (nextSceneMusicCue != null && nextSceneMusicCue.Clip != null)
                return true;

            if (sceneMusicConfig != null && sceneMusicConfig.TryGet(sceneName, out var entry))
            {
                if (HasAnyValidCue(entry.playlist))
                    return true;

                if (entry.musicCue != null && entry.musicCue.Clip != null)
                    return true;
            }

            return false;
        }

        public void PreloadSceneMusicAudioData(string sceneName)
        {
            if (nextSceneMusicCue != null && nextSceneMusicCue.Clip != null)
            {
                nextSceneMusicCue.Clip.LoadAudioData();
                return;
            }

            if (sceneMusicConfig == null || string.IsNullOrWhiteSpace(sceneName))
                return;

            if (!sceneMusicConfig.TryGet(sceneName, out var entry))
                return;

            if (entry.musicCue != null && entry.musicCue.Clip != null)
                entry.musicCue.Clip.LoadAudioData();

            if (entry.playlist == null || entry.playlist.Length == 0)
                return;

            for (int i = 0; i < entry.playlist.Length; i++)
            {
                var cue = entry.playlist[i];
                if (cue == null || cue.Clip == null)
                    continue;

                cue.Clip.LoadAudioData();
            }
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
                uiSource.PlayOneShot(cue.Clip, cue.EffectiveVolume);
                uiSource.pitch = prevPitch;
                return;
            }

            // Gameplay sounds (Sound) and legacy Sfx both use the pooled SFX player.
            PlaySfx(cue.Clip, cue.EffectiveVolume, pitch);
        }

        // Battle-cues should always follow the SFX bus/slider regardless of cue category.
        public void PlayBattleCueAsSfx(AudioCue cue, float volumeScale = 1f, BattleCueRoute route = BattleCueRoute.Sfx)
        {
            if (cue == null || cue.Clip == null)
                return;

            float pitch = GetRandomCuePitch(cue);

            float routeScale = ResolveBattleCueRouteVolumeScale(route);
            float scaledVolume = cue.EffectiveVolume * Mathf.Clamp01(volumeScale) * routeScale;
            scaledVolume = ApplyBattleRouteCeiling(route, scaledVolume);
            PlaySfx(cue.Clip, scaledVolume, pitch, ResolveBattleCueGroup(route));
        }

        public void ConfigureAsSfxSource(AudioSource source, BattleCueRoute route = BattleCueRoute.Sfx)
        {
            if (source == null)
                return;

            source.outputAudioMixerGroup = ResolveBattleCueGroup(route);
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        public bool PlayBattleCueOnSource(AudioCue cue, AudioSource source, bool restartIfAlreadyPlaying = true, float volumeScale = 1f, BattleCueRoute route = BattleCueRoute.Sfx)
        {
            if (cue == null || cue.Clip == null || source == null)
                return false;

            if (!restartIfAlreadyPlaying && source.isPlaying && source.clip == cue.Clip)
                return true;

            ConfigureAsSfxSource(source, route);

            source.Stop();
            source.clip = cue.Clip;
            source.pitch = GetRandomCuePitch(cue);
            float routeScale = ResolveBattleCueRouteVolumeScale(route);
            float finalVolume = cue.EffectiveVolume * Mathf.Clamp01(volumeScale) * routeScale;
            finalVolume = ApplyBattleRouteCeiling(route, finalVolume);
            source.volume = GetScaledSfxVolume(finalVolume);
            source.Play();
            return true;
        }

        private float ApplyBattleRouteCeiling(BattleCueRoute route, float volume01)
        {
            float ceilingGain;
            switch (route)
            {
                case BattleCueRoute.Character:
                    ceilingGain = battleCharacterCeilingGain;
                    break;
                case BattleCueRoute.Combat:
                    ceilingGain = battleCombatCeilingGain;
                    break;
                default:
                    return Mathf.Clamp01(volume01);
            }
            return Mathf.Min(Mathf.Clamp01(volume01), ceilingGain);
        }

        private UnityEngine.Audio.AudioMixerGroup ResolveBattleCueGroup(BattleCueRoute route)
        {
            switch (route)
            {
                case BattleCueRoute.Character:
                    return characterGroup != null ? characterGroup : sfxGroup;
                case BattleCueRoute.Environment:
                    return environmentGroup != null ? environmentGroup : sfxGroup;
                case BattleCueRoute.Combat:
                    return combatGroup != null ? combatGroup : sfxGroup;
                default:
                    return sfxGroup;
            }
        }

        private float ResolveBattleCueRouteVolumeScale(BattleCueRoute route)
        {
            switch (route)
            {
                case BattleCueRoute.Character:
                    return Mathf.Clamp01(battleCharacterRouteVolumeScale);
                case BattleCueRoute.Environment:
                    return Mathf.Clamp01(battleEnvironmentRouteVolumeScale);
                case BattleCueRoute.Combat:
                    return Mathf.Clamp01(battleCombatRouteVolumeScale);
                default:
                    return Mathf.Clamp01(battleSfxRouteVolumeScale);
            }
        }

        public float GetScaledSfxVolume(float baseVolume)
        {
            return ComputeSfxSourceVolume(baseVolume);
        }

        public float GetRandomCuePitch(AudioCue cue)
        {
            if (cue == null)
                return 1f;

            float min = cue.PitchRange.x;
            float max = cue.PitchRange.y;
            if (max > min)
                return UnityEngine.Random.Range(min, max);

            return min;
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

            musicFadeCoroutine = StartCoroutine(TransitionToMusicRoutine(
                clip,
                loop,
                startTimeSeconds: 0f,
                fadeInSeconds: defaultMusicFadeInSeconds,
                fadeOutSeconds: defaultMusicFadeOutSeconds
            ));
        }

        public bool TryGetCurrentMusicState(out AudioClip clip, out float timeSeconds, out bool loop)
        {
            clip = null;
            timeSeconds = 0f;
            loop = false;

            if (musicSource == null || musicSource.clip == null)
                return false;

            clip = musicSource.clip;
            loop = musicSource.loop;

            float maxTime = clip.length > 0f ? clip.length : 0f;
            timeSeconds = Mathf.Clamp(musicSource.time, 0f, maxTime);
            return true;
        }

        public void PlayMusicFromTime(AudioClip clip, float timeSeconds, bool loop)
        {
            PlayMusicFromTime(clip, timeSeconds, loop, fadeInSeconds: defaultMusicFadeInSeconds);
        }

        public void PlayMusicFromTime(AudioClip clip, float timeSeconds, bool loop, float fadeInSeconds)
        {
            if (clip == null)
                return;

            if (musicSource == null)
            {
                Debug.LogWarning("AudioManager: musicSource не назначен — PlayMusicFromTime пропущен.");
                return;
            }

            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
                musicFadeCoroutine = null;
            }

            musicFadeCoroutine = StartCoroutine(TransitionToMusicRoutine(
                clip,
                loop,
                startTimeSeconds: timeSeconds,
                fadeInSeconds: Mathf.Max(0f, fadeInSeconds),
                fadeOutSeconds: defaultMusicFadeOutSeconds
            ));
        }

        private IEnumerator TransitionToMusicRoutine(AudioClip clip, bool loop, float startTimeSeconds, float fadeInSeconds, float fadeOutSeconds)
        {
            if (musicSource == null || clip == null)
            {
                musicFadeCoroutine = null;
                yield break;
            }

            bool hasCurrentMusic = musicSource.isPlaying && musicSource.clip != null;

            if (hasCurrentMusic && audioMixer != null && fadeOutSeconds > 0f)
            {
                float currentDb = targetMusicDb;
                if (!audioMixer.GetFloat(MusicVolumeParam, out currentDb))
                    currentDb = targetMusicDb;

                float fadeOutDuration = Mathf.Max(0.001f, fadeOutSeconds);
                float outT = 0f;
                while (outT < 1f)
                {
                    outT += Time.unscaledDeltaTime / fadeOutDuration;
                    audioMixer.SetFloat(MusicVolumeParam, Mathf.Lerp(currentDb, -80f, outT));
                    yield return null;
                }

                audioMixer.SetFloat(MusicVolumeParam, -80f);
            }

            musicSource.Stop();

            currentClip = clip;
            musicSource.clip = clip;
            musicSource.loop = loop;

            float maxTime = clip.length > 0f ? Mathf.Max(0f, clip.length - 0.01f) : 0f;
            musicSource.time = Mathf.Clamp(startTimeSeconds, 0f, maxTime);

            if (audioMixer != null)
            {
                if (fadeInSeconds > 0f)
                    audioMixer.SetFloat(MusicVolumeParam, -80f);
                else
                    audioMixer.SetFloat(MusicVolumeParam, targetMusicDb);
            }

            musicSource.Play();

            if (audioMixer != null && fadeInSeconds > 0f)
            {
                float fadeInDuration = Mathf.Max(0.001f, fadeInSeconds);
                float inT = 0f;
                while (inT < 1f)
                {
                    inT += Time.unscaledDeltaTime / fadeInDuration;
                    audioMixer.SetFloat(MusicVolumeParam, Mathf.Lerp(-80f, targetMusicDb, inT));
                    yield return null;
                }

                audioMixer.SetFloat(MusicVolumeParam, targetMusicDb);
            }

            musicFadeCoroutine = null;
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
            volume = Mathf.Clamp01(volume);
            targetMusicDb = SliderToDb(volume);
            if (audioMixer != null)
                audioMixer.SetFloat(MusicVolumeParam, targetMusicDb);

            if (audioMixer == null && musicSource != null)
                musicSource.volume = SliderToGain(volume);
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

        private void InitAmbientPool()
        {
            if (ambientPrefab == null)
                return;

            ambientPool = new AudioSource[ambientPoolSize];
            ambientPanCoroutines = new Coroutine[ambientPoolSize];
            ambientBaseVolumes = new float[ambientPoolSize];

            for (int i = 0; i < ambientPoolSize; i++)
            {
                var src = Instantiate(ambientPrefab, ambientParent);
                src.outputAudioMixerGroup = ambientGroup;
                src.playOnAwake = false;
                src.volume = 1f;
                src.panStereo = 0f;
                ambientPool[i] = src;
                ambientBaseVolumes[i] = 1f;
            }
        }

        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            PlaySfx(clip, volume, pitch, null);
        }

        private void PlaySfx(AudioClip clip, float volume, float pitch, UnityEngine.Audio.AudioMixerGroup outputGroupOverride)
        {
            if (clip == null)
                return;

            EnsureSfxPool();

            if (sfxPool == null)
                return;

            var src = sfxPool[sfxIndex];
            sfxIndex = (sfxIndex + 1) % sfxPool.Length;

            src.Stop();
            src.outputAudioMixerGroup = outputGroupOverride != null ? outputGroupOverride : sfxGroup;
            src.clip = clip;
            src.pitch = pitch;
            src.volume = ComputeSfxSourceVolume(volume);
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
            if (audioMixer != null && hasSfxVolumeParam)
                audioMixer.SetFloat(SfxVolumeParam, SfxSliderToDb(sfxVolume));

            if (audioMixer != null && hasAmbientVolumeParam)
                audioMixer.SetFloat(AmbientVolumeParam, ToDb(GetEffectiveAmbientVolume()));

            RefreshAmbientSourceVolumes();
        }

        public void SetAmbientVolume(float volume)
        {
            // Ambient is explicitly tied to SFX master in this project.
            if (audioMixer != null && hasAmbientVolumeParam)
                audioMixer.SetFloat(AmbientVolumeParam, ToDb(GetEffectiveAmbientVolume()));

            RefreshAmbientSourceVolumes();
        }

        public void PlayAmbient(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            PlayAmbient(clip, volume, pitch, 0f, 0f, 0f);
        }

        public void PlayAmbient(
            AudioClip clip,
            float volume,
            float pitch,
            float panStart,
            float panEnd,
            float panSweepDurationSeconds)
        {
            if (clip == null)
                return;

            EnsureAmbientPool();

            if (ambientPool == null)
            {
                // Fallback keeps ambient audible if pool is not configured yet.
                float fallbackAmbientVolume = Mathf.Clamp01(volume) * Mathf.Clamp01(ambientGlobalVolumeScale);
                PlaySfx(clip, fallbackAmbientVolume, pitch);
                return;
            }

            int sourceIndex = ambientIndex;
            var src = ambientPool[sourceIndex];
            ambientIndex = (ambientIndex + 1) % ambientPool.Length;

            if (ambientPanCoroutines != null && sourceIndex >= 0 && sourceIndex < ambientPanCoroutines.Length)
            {
                if (ambientPanCoroutines[sourceIndex] != null)
                {
                    StopCoroutine(ambientPanCoroutines[sourceIndex]);
                    ambientPanCoroutines[sourceIndex] = null;
                }
            }

            float clampedPanStart = Mathf.Clamp(panStart, -1f, 1f);
            float clampedPanEnd = Mathf.Clamp(panEnd, -1f, 1f);
            float safeSweepDuration = Mathf.Max(0f, panSweepDurationSeconds);

            src.Stop();
            src.clip = clip;
            src.pitch = pitch;
            float baseVolume = Mathf.Clamp01(volume);
            if (ambientBaseVolumes != null && sourceIndex >= 0 && sourceIndex < ambientBaseVolumes.Length)
                ambientBaseVolumes[sourceIndex] = baseVolume;

            // If ambient is controlled by mixer param, avoid applying ambient scalar on source too.
            src.volume = ComputeAmbientSourceVolume(baseVolume);
            src.panStereo = clampedPanStart;
            src.Play();

            if (safeSweepDuration > 0f && !Mathf.Approximately(clampedPanStart, clampedPanEnd))
            {
                if (ambientPanCoroutines != null && sourceIndex >= 0 && sourceIndex < ambientPanCoroutines.Length)
                    ambientPanCoroutines[sourceIndex] = StartCoroutine(SweepAmbientPanRoutine(src, clampedPanStart, clampedPanEnd, safeSweepDuration));
            }
        }

        private static IEnumerator SweepAmbientPanRoutine(AudioSource src, float fromPan, float toPan, float durationSeconds)
        {
            if (src == null || durationSeconds <= 0f)
                yield break;

            float t = 0f;
            src.panStereo = fromPan;

            while (src != null && src.isActiveAndEnabled && src.isPlaying && t < durationSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / durationSeconds);
                src.panStereo = Mathf.Lerp(fromPan, toPan, k);
                yield return null;
            }

            if (src != null)
                src.panStereo = toPan;
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
            volume = Mathf.Clamp01(volume);

            if (audioMixer != null)
                audioMixer.SetFloat(UiVolumeParam, SliderToDb(volume));

            if (audioMixer == null && uiSource != null)
                uiSource.volume = SliderToGain(volume);
        }

        public void StopAmbient()
        {
            if (ambientPool == null)
                return;

            for (int i = 0; i < ambientPool.Length; i++)
            {
                var src = ambientPool[i];
                if (src == null)
                    continue;

                if (ambientPanCoroutines != null && i < ambientPanCoroutines.Length && ambientPanCoroutines[i] != null)
                {
                    StopCoroutine(ambientPanCoroutines[i]);
                    ambientPanCoroutines[i] = null;
                }

                src.Stop();
                src.clip = null;
                src.panStereo = 0f;
            }
        }

        public void StopCharacterAndCombat()
        {
            StopAndClearSource(characterSource);
            StopAndClearSource(combatSource);
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

            StopAmbient();

            StopCharacterAndCombat();

            if (environmentSource != null)
                environmentSource.Stop();
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

        private static void StopAndClearSource(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
        }

        private float SliderToDb(float slider01)
        {
            slider01 = Mathf.Clamp01(slider01);
            if (slider01 <= 0f)
                return -80f;

            float curved = Mathf.Pow(slider01, sliderCurveSafe);
            return Mathf.Lerp(sliderMinDb, 0f, curved);
        }

        private float SliderToGain(float slider01)
        {
            float db = SliderToDb(slider01);
            return DbToGain(db);
        }

        private float SfxSliderToDb(float slider01)
        {
            slider01 = Mathf.Clamp01(slider01);
            if (slider01 <= 0f)
                return -80f;

            float curved = Mathf.Pow(slider01, sliderCurveSafe);
            float minDb = Mathf.Clamp(sfxSliderMinDb, -80f, 0f);
            return Mathf.Lerp(minDb, 0f, curved);
        }

        private float SfxSliderToGain(float slider01)
        {
            float db = SfxSliderToDb(slider01);
            return DbToGain(db);
        }

        private float GetEffectiveAmbientVolume()
        {
            float ambientGain = Mathf.Clamp01(SfxSliderToGain(sfxVolume) * Mathf.Clamp01(ambientGlobalVolumeScale));
            return Mathf.Min(ambientGain, ambientCeilingGain);
        }

        private static float DbToGain(float db)
        {
            if (db <= -80f)
                return 0f;

            return Mathf.Pow(10f, db * 0.05f);
        }

        private void RefreshDerivedAudioParams()
        {
            sliderCurveSafe = Mathf.Max(0.01f, sliderCurve);
            ambientCeilingGain = DbToGain(ambientCeilingDb);
            battleCharacterCeilingGain = DbToGain(battleCharacterCeilingDb);
            battleCombatCeilingGain = DbToGain(battleCombatCeilingDb);
        }

        private float ComputeSfxSourceVolume(float baseVolume)
        {
            float scalar = (audioMixer != null && hasSfxVolumeParam) ? 1f : SfxSliderToGain(sfxVolume);
            return Mathf.Clamp01(baseVolume) * scalar;
        }

        private float ComputeAmbientSourceVolume(float baseVolume)
        {
            float scalar = (audioMixer != null && hasAmbientVolumeParam) ? 1f : GetEffectiveAmbientVolume();
            return Mathf.Clamp01(baseVolume) * scalar;
        }

        private void RefreshAmbientSourceVolumes()
        {
            if (ambientPool == null || ambientPool.Length == 0)
                return;

            for (int i = 0; i < ambientPool.Length; i++)
            {
                var src = ambientPool[i];
                if (src == null)
                    continue;

                float baseVolume = 1f;
                if (ambientBaseVolumes != null && i >= 0 && i < ambientBaseVolumes.Length)
                    baseVolume = ambientBaseVolumes[i];

                src.volume = ComputeAmbientSourceVolume(baseVolume);
            }
        }
    }
}
