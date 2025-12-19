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

		// Перегрузка с минимальным временем загрузки
		public void LoadScene(string sceneName, SceneTransitionData data = null, float? minLoadingTime = null)
		{
			float minTime = minLoadingTime ?? DefaultMinLoadingTime;
			StartCoroutine(LoadSceneRoutine(sceneName, data, minTime));
		}

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
