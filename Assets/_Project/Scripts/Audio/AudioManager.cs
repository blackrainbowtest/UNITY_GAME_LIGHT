using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace UDA2.Audio
{
    public class AudioManager : MonoBehaviour
    {
        private float sfxVolume = 1f;
        private Coroutine sfxVolumeFadeCoroutine;
        public static AudioManager Instance;

        [Header("Music Source")]
        [SerializeField] private AudioSource musicSource;

        [Header("SFX Settings")]
        [SerializeField] private AudioSource sfxPrefab;
        [SerializeField] private int sfxPoolSize = 10;
        [SerializeField] private Transform sfxParent;
        private AudioSource[] sfxPool;
        private int sfxPoolIndex = 0;

        [Header("Music Clips")]
        public AudioClip logoMusic;
        public AudioClip introMusic;
        public AudioClip mainMenuMusic;
        public AudioClip gameplayMusic;

        private AudioClip currentClip;
        private void Awake()
        {
            // Установить громкость SFX из настроек
            if (UDA2.Core.SettingsContext.Current != null)
                sfxVolume = UDA2.Core.SettingsContext.Current.sfxVolume;
            if (sfxPool != null)
                foreach (var src in sfxPool)
                    if (src != null) src.volume = sfxVolume;
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // SFX Pool init
            if (sfxPrefab != null)
            {
                sfxPool = new AudioSource[sfxPoolSize];
                for (int i = 0; i < sfxPoolSize; i++)
                {
                    var src = Instantiate(sfxPrefab, sfxParent);
                    src.playOnAwake = false;
                    sfxPool[i] = src;
                }
            }

            // Установить громкость музыки из настроек
            float volume = 1f;
            if (UDA2.Core.SettingsContext.Current != null)
                volume = UDA2.Core.SettingsContext.Current.musicVolume;
            if (musicSource != null)
                musicSource.volume = volume;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AudioClip nextClip = null;
            float fadeIn = 1.5f;
            float fadeOut = 1.0f;
            bool isLogo = false;
            switch (scene.name)
            {
                case "SplashScene":
                    nextClip = logoMusic;
                    fadeIn = 1f;
                    fadeOut = 1f;
                    isLogo = true;
                    break;
                case "IntroScene":
                    nextClip = introMusic;
                    break;
                case "MainMenuScene":
                    nextClip = mainMenuMusic;
                    break;
                case "MainScene":
                    nextClip = gameplayMusic;
                    break;
            }
            if (nextClip != null)
            {
                PlayMusic(nextClip, fadeIn, fadeOut);
            }
            else
            {
                // Если для сцены нет музыки — затухание и остановка
                if (fadeCoroutine != null)
                    StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeOutAndStop());
            }
        }

        private IEnumerator FadeOutAndStop(float fadeOut = 1.0f)
        {
            if (musicSource == null || !musicSource.isPlaying)
                yield break;
            float startVolume = musicSource.volume;
            float t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeOut);
                yield return null;
            }
            musicSource.volume = 0f;
            musicSource.Stop();
            currentClip = null;
        }

        private Coroutine fadeCoroutine;

        private void PlayMusic(AudioClip clip, float fadeIn = 1.5f, float fadeOut = 1.0f)
        {
            if (clip == null || clip == currentClip)
                return;

            // Не играть музыку при громкости 0
            float targetVolume = 1f;
            if (UDA2.Core.SettingsContext.Current != null)
                targetVolume = UDA2.Core.SettingsContext.Current.musicVolume;

            // Если громкость 0 — играем трек без звука
            if (targetVolume <= 0.001f)
            {
                currentClip = clip;
                musicSource.clip = clip;
                musicSource.loop = false;
                if (fadeCoroutine != null)
                    StopCoroutine(fadeCoroutine);
                musicSource.volume = 0f;
                musicSource.Play();
                fadeCoroutine = StartCoroutine(FadeInMusicAndLoop(clip, 0f, fadeIn, fadeOut));
                return;
            }

            currentClip = clip;
            musicSource.clip = clip;
            musicSource.loop = false; // Управляем циклом вручную
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeInMusicAndLoop(clip, targetVolume, fadeIn, fadeOut));
        }

        private IEnumerator FadeInMusicAndLoop(AudioClip clip, float targetVolume, float fadeIn = 1.5f, float fadeOut = 1.0f)
        {
            // Получить актуальное значение громкости из настроек
            float realTargetVolume = targetVolume;
            if (UDA2.Core.SettingsContext.Current != null)
                realTargetVolume = UDA2.Core.SettingsContext.Current.musicVolume;

            // Fade-in всегда от 0
            musicSource.volume = 0f;
            musicSource.Play();
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, realTargetVolume, t / fadeIn);
                yield return null;
            }
            musicSource.volume = realTargetVolume;

            // Ждём окончания трека
            while (musicSource.isPlaying && musicSource.time < musicSource.clip.length - 0.05f)
                yield return null;

            // Fade-out
            t = 0f;
            float startVolume = musicSource.volume;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeOut);
                yield return null;
            }
            musicSource.volume = 0f;
            musicSource.Stop();

            // Если громкость не 0, повторяем с fade-in
            if (musicSource != null && musicSource.clip == clip && musicSource.volume >= 0f && realTargetVolume > 0.001f)
            {
                fadeCoroutine = StartCoroutine(FadeInMusicAndLoop(clip, realTargetVolume, fadeIn, fadeOut));
            }
        }

        // Старый fade-in больше не используется
		/// <summary>
        /// Установить громкость музыки в реальном времени
        /// </summary>
        private Coroutine volumeFadeCoroutine;

        public void SetMusicVolume(float volume, float fadeTime = 0.5f)
        {
            if (musicSource == null)
                return;
            if (volumeFadeCoroutine != null)
                StopCoroutine(volumeFadeCoroutine);
            volumeFadeCoroutine = StartCoroutine(FadeVolumeTo(volume, fadeTime));
        }

        private IEnumerator FadeVolumeTo(float target, float duration)
        {
            float start = musicSource.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            musicSource.volume = target;
        }

        /// <summary>
        /// Воспроизвести SFX через пул AudioSource
        /// </summary>
        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || sfxPool == null || sfxPool.Length == 0)
                return;
            var src = sfxPool[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;
            src.Stop();
            src.clip = clip;
            src.volume = sfxVolume * volume;
            src.pitch = pitch;
            src.Play();
        }

        /// <summary>
        /// Установить громкость SFX в реальном времени
        /// </summary>
        public void SetSfxVolume(float volume, float fadeTime = 0.5f)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxVolumeFadeCoroutine != null)
                StopCoroutine(sfxVolumeFadeCoroutine);
            sfxVolumeFadeCoroutine = StartCoroutine(FadeSfxVolumeTo(sfxVolume, fadeTime));
        }

        private IEnumerator FadeSfxVolumeTo(float target, float duration)
        {
            if (sfxPool == null) yield break;
            float[] startVolumes = new float[sfxPool.Length];
            for (int i = 0; i < sfxPool.Length; i++)
                startVolumes[i] = sfxPool[i].volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float v = Mathf.Lerp(0f, target, t / duration);
                for (int i = 0; i < sfxPool.Length; i++)
                    sfxPool[i].volume = v;
                yield return null;
            }
            for (int i = 0; i < sfxPool.Length; i++)
                sfxPool[i].volume = target;
        }
    }
}
