using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UDA2.City
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class BackToPreviousSceneButton : MonoBehaviour
    {
        [Tooltip("Used when previous/main scene is not available in save.")]
        [SerializeField] private string fallbackSceneName = "StartCityScene";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
            UpdateInteractable();
        }

        private void OnEnable()
        {
            UpdateInteractable();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            string targetScene = ResolveTargetSceneName();
            if (string.IsNullOrEmpty(targetScene))
                return;

            var sceneFlow = UDA2.SceneFlow.SceneFlowManager.Instance;
            if (sceneFlow != null)
            {
                sceneFlow.LoadScene(targetScene);
                return;
            }

            SceneManager.LoadScene(targetScene);
        }

        private void UpdateInteractable()
        {
            if (_button == null)
                return;

            _button.interactable = !string.IsNullOrEmpty(ResolveTargetSceneName());
        }

        private string ResolveTargetSceneName()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            var save = global::GameState.Instance.CurrentSave;

            if (save != null && save.sceneState != null)
            {
                var previous = save.sceneState.previousSceneName;
                if (!string.IsNullOrEmpty(previous) && !string.Equals(previous, currentSceneName, System.StringComparison.Ordinal))
                    return previous;

                var lastMain = save.sceneState.lastMainSceneName;
                if (!string.IsNullOrEmpty(lastMain) && !string.Equals(lastMain, currentSceneName, System.StringComparison.Ordinal))
                    return lastMain;
            }

            if (!string.IsNullOrEmpty(fallbackSceneName) && !string.Equals(fallbackSceneName, currentSceneName, System.StringComparison.Ordinal))
                return fallbackSceneName;

            return null;
        }
    }
}
