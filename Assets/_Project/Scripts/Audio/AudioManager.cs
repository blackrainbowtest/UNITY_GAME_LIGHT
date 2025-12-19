using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace UDA2.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [Header("Music Source")]
        [SerializeField] private AudioSource musicSource;

        [Header("Music Clips")]
        public AudioClip introMusic;
        public AudioClip mainMenuMusic;
        public AudioClip gameplayMusic;

        private AudioClip currentClip;


        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

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
            switch (scene.name)
            {
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
                PlayMusic(nextClip);
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

        private void PlayMusic(AudioClip clip)
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
                fadeCoroutine = StartCoroutine(FadeInMusicAndLoop(clip, 0f));
                return;
            }

            currentClip = clip;
            musicSource.clip = clip;
            musicSource.loop = false; // Управляем циклом вручную
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeInMusicAndLoop(clip, targetVolume));
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
    }
}
