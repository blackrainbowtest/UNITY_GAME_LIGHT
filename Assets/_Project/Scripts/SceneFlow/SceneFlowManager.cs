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
        [SerializeField] private UI.LoadingScreenController loadingScreen;

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

        public void NotifySceneReady()
        {
            _sceneReady = true;
        }

        public void LoadScene(string sceneName, SceneTransitionData data = null)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, data));
        }

        public IEnumerator LoadSceneRoutine(string sceneName, SceneTransitionData data = null)
        {
            _sceneReady = false;
            if (loadingScreen != null)
                loadingScreen.Show();

            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = true;

            while (!_sceneReady)
                yield return null;

            if (loadingScreen != null)
                loadingScreen.Hide();
        }
    }
}
