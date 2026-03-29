using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace UDA2.SceneFlow
{
	public interface ILoadingScreen
	{
		void Show();
		void Hide();
		void SetProgress(float progress);
	}

    public class SceneFlowManager : MonoBehaviour
	{
		public static SceneFlowManager Instance { get; private set; }

		[Header("Audio Sync")]
		[SerializeField] private bool waitForMusicBeforeHideLoading = true;
		[SerializeField, Min(0f)] private float musicReadyTimeoutSeconds = 2f;
		[SerializeField] private bool skipMusicWaitIfSceneHasNoMusic = true;

		private bool _sceneReady;
		private ILoadingScreen loadingScreen;
		private Coroutine _loadCoroutine;

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		private void OnDisable()
		{
			if (_loadCoroutine != null)
			{
				StopCoroutine(_loadCoroutine);
				_loadCoroutine = null;
			}
		}

		private void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}

		public void RegisterLoadingScreen(ILoadingScreen screen)
		{
			loadingScreen = screen;
		}

		public void UnregisterLoadingScreen(ILoadingScreen screen)
		{
			if (loadingScreen == screen)
				loadingScreen = null;
		}

		public void NotifySceneReady()
		{
			_sceneReady = true;
		}


		private const float DefaultMinLoadingTime = 1.0f; // по умолчанию 1 секунда


		// Имя вашей сцены загрузки
		private const string LoadingSceneName = "LoadingScene";

		// Перегрузка с минимальным временем загрузки
		public void LoadScene(string sceneName, SceneTransitionData data = null, float? minLoadingTime = null)
		{
			float minTime = minLoadingTime ?? DefaultMinLoadingTime;
			if (_loadCoroutine != null)
			{
				StopCoroutine(_loadCoroutine);
				_loadCoroutine = null;
			}
			_loadCoroutine = StartCoroutine(LoadSceneWithLoadingScreen(sceneName, data, minTime));
		}

		// Новый flow: всегда через LoadingScene
		private IEnumerator LoadSceneWithLoadingScreen(string targetScene, SceneTransitionData data, float minLoadingTime)
		{
			// Если уже в LoadingScene, просто грузим целевую сцену
			if (SceneManager.GetActiveScene().name == LoadingSceneName)
			{
				yield return StartCoroutine(LoadSceneRoutine(targetScene, data, minLoadingTime));
				yield break;
			}

			// 1. Загружаем LoadingScene
			_sceneReady = false;
			loadingScreen = null;
			AsyncOperation loadingOp = SceneManager.LoadSceneAsync(LoadingSceneName);
			while (!loadingOp.isDone)
				yield return null;

			// 2. Ждём, пока LoadingScreenController зарегистрируется
			float waitTime = 0f;
			while (loadingScreen == null && waitTime < 5f) // fail-safe 5 сек
			{
				waitTime += Time.unscaledDeltaTime;
				yield return null;
			}

			// 3. Показываем loading (на всякий случай)
			if (loadingScreen != null)
				loadingScreen.Show();

			// 4. Грузим целевую сцену с задержкой
			yield return StartCoroutine(LoadSceneRoutine(targetScene, data, minLoadingTime));
			_loadCoroutine = null;
		}

		// Обычная загрузка целевой сцены с ожиданием ready и минимального времени
		private IEnumerator LoadSceneRoutine(string sceneName, SceneTransitionData data, float minLoadingTime)
		{
			_sceneReady = false;

			if (loadingScreen != null)
				loadingScreen.Show();

			float timer = 0f;
			var asyncOp = SceneManager.LoadSceneAsync(sceneName);

			while (!_sceneReady)
			{
				timer += Time.unscaledDeltaTime;
				yield return null;
			}

			// Ждём, если минимальное время не прошло
			while (timer < minLoadingTime)
			{
				timer += Time.unscaledDeltaTime;
				yield return null;
			}

			yield return WaitForMusicReady(sceneName);

			if (loadingScreen != null)
				loadingScreen.Hide();
		}

		private IEnumerator WaitForMusicReady(string sceneName)
		{
			if (!waitForMusicBeforeHideLoading)
				yield break;

			var timeout = Mathf.Max(0f, musicReadyTimeoutSeconds);
			if (timeout <= 0f)
				yield break;

			float waited = 0f;
			while (!TryGetAudioManagerSingleton(out var audio) && waited < timeout)
			{
				waited += Time.unscaledDeltaTime;
				yield return null;
			}

			if (!TryGetAudioManagerSingleton(out var audioManager))
				yield break;

			if (skipMusicWaitIfSceneHasNoMusic && !WillPlayMusicForScene(audioManager, sceneName))
				yield break;

			waited = 0f;
			while (!IsMusicPlaying(audioManager) && waited < timeout)
			{
				waited += Time.unscaledDeltaTime;
				yield return null;
			}
		}

		private static bool TryGetAudioManagerSingleton(out object audioManager)
		{
			audioManager = null;

			var type = System.Type.GetType("UDA2.Audio.AudioManager, UDA2.Audio");
			if (type == null)
				return false;

			var instanceField = type.GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			if (instanceField == null)
				return false;

			audioManager = instanceField.GetValue(null);
			return audioManager != null;
		}

		private static bool IsMusicPlaying(object audioManager)
		{
			if (audioManager == null)
				return false;

			var property = audioManager.GetType().GetProperty("IsMusicPlaying", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (property == null || property.PropertyType != typeof(bool))
				return false;

			var value = property.GetValue(audioManager);
			return value is bool isPlaying && isPlaying;
		}

		private static bool WillPlayMusicForScene(object audioManager, string sceneName)
		{
			if (audioManager == null)
				return false;

			var method = audioManager.GetType().GetMethod(
				"WillPlayMusicForScene",
				System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
				null,
				new[] { typeof(string) },
				null);

			if (method == null)
				return false;

			var result = method.Invoke(audioManager, new object[] { sceneName });
			return result is bool hasMusic && hasMusic;
		}
	}
}
