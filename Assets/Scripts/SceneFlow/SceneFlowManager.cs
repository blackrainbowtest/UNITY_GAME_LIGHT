using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UDA2.SceneFlow;

namespace UDA2.SceneFlow
{
    public class SceneFlowManager : MonoBehaviour
	{
		public static SceneFlowManager Instance { get; private set; }

		private bool _sceneReady;
		private UI.LoadingScreenController loadingScreen;

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

		public void RegisterLoadingScreen(UI.LoadingScreenController screen)
		{
			loadingScreen = screen;
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
			StartCoroutine(LoadSceneWithLoadingScreen(sceneName, data, minTime));
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

			if (loadingScreen != null)
				loadingScreen.Hide();
		}
	}
}
