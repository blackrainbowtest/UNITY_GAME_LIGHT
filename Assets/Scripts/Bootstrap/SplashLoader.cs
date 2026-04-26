using UnityEngine;
using UDA2.SceneFlow;

namespace UDA2.Bootstrap
{
    public class SplashLoader : MonoBehaviour
    {
        public float splashDuration = 2f;
        [Header("Flow")]
        [SerializeField] private string disclaimerSceneName = "DisclaimerScene";
        [SerializeField] private string fallbackNextSceneName = "MainMenuScene";
        [SerializeField, Min(0f)] private float fallbackNextSceneMinLoadTime = 2f;

        private void Start()
        {
            StartCoroutine(ShowSplashAndLoadMenu());
        }

		private void OnDisable()
		{
			StopAllCoroutines();
		}
        private System.Collections.IEnumerator ShowSplashAndLoadMenu()
        {
            yield return new WaitForSeconds(splashDuration);

            // Preferred startup flow: Splash -> Disclaimer -> MainMenu.
            if (!string.IsNullOrWhiteSpace(disclaimerSceneName) && Application.CanStreamedLevelBeLoaded(disclaimerSceneName))
            {
                LoadSceneByName(disclaimerSceneName, 0f);
                yield break;
            }

            // Safety fallback if DisclaimerScene is not in Build Settings yet.
            if (!string.IsNullOrWhiteSpace(fallbackNextSceneName))
                LoadSceneByName(fallbackNextSceneName, fallbackNextSceneMinLoadTime);
        }

        private static void LoadSceneByName(string sceneName, float minLoadTime)
        {
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.LoadScene(sceneName, null, minLoadTime);
            else
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
