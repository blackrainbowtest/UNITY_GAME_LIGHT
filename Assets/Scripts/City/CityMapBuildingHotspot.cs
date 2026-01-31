using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UDA2.City
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class CityMapBuildingHotspot : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private string targetSceneName;

        [Header("Visual")]
        [Tooltip("Optional: a highlight object (e.g. white frame Image) enabled when Inspect Mode is ON.")]
        [SerializeField] private Graphic highlight;

        private Button button;

        public string TargetSceneName => targetSceneName;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);

            if (highlight != null)
                highlight.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void SetHighlight(bool enabled)
        {
            if (highlight != null)
                highlight.gameObject.SetActive(enabled);
        }

        private void HandleClick()
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning($"[CityMapBuildingHotspot] targetSceneName is empty on '{name}'", this);
                return;
            }

            // Prefer SceneFlowManager if present.
            var sceneFlow = UDA2.SceneFlow.SceneFlowManager.Instance;
            if (sceneFlow != null)
            {
                sceneFlow.LoadScene(targetSceneName);
                return;
            }

            SceneManager.LoadScene(targetSceneName);
        }
    }
}
