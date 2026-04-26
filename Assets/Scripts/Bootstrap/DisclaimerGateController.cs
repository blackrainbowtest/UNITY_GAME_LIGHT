using UDA2.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UDA2.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class DisclaimerGateController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button exitButton;

        [Header("Flow")]
        [SerializeField] private string nextSceneName = "MainMenuScene";
        [SerializeField, Min(0f)] private float nextSceneMinLoadTime = 2f;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirm);

            if (exitButton != null)
                exitButton.onClick.AddListener(HandleExit);
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirm);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(HandleExit);
        }

        private void HandleConfirm()
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
                return;

            if (SceneFlowManager.Instance != null)
            {
                SceneFlowManager.Instance.LoadScene(nextSceneName, null, nextSceneMinLoadTime);
                return;
            }

            SceneManager.LoadSceneAsync(nextSceneName);
        }

        private void HandleExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
