using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UDA2.City
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class GoToShelterSceneButton : MonoBehaviour
    {
        [Tooltip("Target scene for Home button.")]
        [SerializeField] private string shelterSceneName = "PlayerShelterScene";

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

            SceneManager.LoadSceneAsync(targetScene);
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
                var lastShelter = save.sceneState.lastShelterSceneName;
                if (!string.IsNullOrEmpty(lastShelter)
                    && !string.Equals(lastShelter, currentSceneName, System.StringComparison.Ordinal))
                {
                    return lastShelter;
                }
            }

            if (!string.IsNullOrEmpty(shelterSceneName)
                && !string.Equals(shelterSceneName, currentSceneName, System.StringComparison.Ordinal))
            {
                return shelterSceneName;
            }

            return null;
        }
    }
}
