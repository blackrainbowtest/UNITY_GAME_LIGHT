using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace UDA2.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        /* ===================== AUDIO MIXER ===================== */

        [Header("Audio Mixer")]
        [SerializeField] private UnityEngine.Audio.AudioMixer audioMixer;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup musicGroup;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup sfxGroup;
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup uiGroup;

        /* ===================== MUSIC ===================== */

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;

        public AudioClip logoMusic;
        public AudioClip introMusic;
        public AudioClip mainMenuMusic;
        public AudioClip gameplayMusic;

        private AudioClip currentClip;
        private Coroutine musicFadeCoroutine;

        /* ===================== SFX ===================== */

        [Header("SFX")]
        [SerializeField] private AudioSource sfxPrefab;
        [SerializeField] private int sfxPoolSize = 10;
        [SerializeField] private Transform sfxParent;

        private AudioSource[] sfxPool;
        private int sfxIndex;
        private float sfxVolume = 1f;

        /* ===================== UI ===================== */

        [Header("UI Audio")]
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioClip uiClickClip;

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

            // Fail-fast mixer validation
            CheckParam("MusicVolume");
            CheckParam("SFXVolume");
            CheckParam("UIVolume");

            // Music source
            if (musicSource != null && musicGroup != null)
            {
                musicSource.outputAudioMixerGroup = musicGroup;
                musicSource.playOnAwake = false;
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
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /* ===================== SCENE MUSIC ===================== */

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AudioClip clip = null;

            switch (scene.name)
            {
                case "SplashScene":   clip = logoMusic; break;
                case "IntroScene":    clip = introMusic; break;
                case "MainMenuScene": clip = mainMenuMusic; break;
                case "MainScene":     clip = gameplayMusic; break;
            }

            if (clip != null)
                PlayMusic(clip);
            else
                StopMusic();
        }

        /* ===================== MUSIC ===================== */

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || clip == currentClip)
                return;

            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);

            currentClip = clip;
            musicSource.clip = clip;
            musicFadeCoroutine = StartCoroutine(FadeMusicIn());
        }

        private IEnumerator FadeMusicIn()
        {
            audioMixer.SetFloat("MusicVolume", -80f);
            musicSource.Play();

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                audioMixer.SetFloat("MusicVolume", Mathf.Lerp(-80f, 0f, t));
                yield return null;
            }
        }

        private void StopMusic()
        {
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
            audioMixer.SetFloat("MusicVolume", ToDb(volume));
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
            if (clip == null || sfxPool == null)
                return;

            var src = sfxPool[sfxIndex];
            sfxIndex = (sfxIndex + 1) % sfxPool.Length;

            src.Stop();
            src.clip = clip;
            src.pitch = pitch;
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
            audioMixer.SetFloat("SFXVolume", ToDb(sfxVolume));
        }

        /* ===================== UI ===================== */

        public void PlayUiClick()
        {
            if (uiClickClip != null)
                uiSource.PlayOneShot(uiClickClip);
        }

        public void SetUiVolume(float volume)
        {
            audioMixer.SetFloat("UIVolume", ToDb(volume));
        }

        /* ===================== UTILS ===================== */

        private void CheckParam(string name)
        {
            if (!audioMixer.GetFloat(name, out _))
                Debug.LogError($"AudioMixer missing exposed parameter: {name}");
        }

        private float ToDb(float v)
        {
            return Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f;
        }
    }
}
