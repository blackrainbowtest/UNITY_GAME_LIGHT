using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("highlight")]
        [SerializeField] private GameObject highlightObject;

        [Tooltip("Optional: if you prefer, drag the Image component directly here.")]
        [SerializeField] private Graphic highlightGraphic;

        private Button button;

        public string TargetSceneName => targetSceneName;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);

            // If only object was set, try resolve graphic for future use.
            if (highlightGraphic == null && highlightObject != null)
                highlightGraphic = highlightObject.GetComponent<Graphic>();

            var go = GetHighlightGameObject();
            if (go != null)
                go.SetActive(false);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void SetHighlight(bool enabled)
        {
            var go = GetHighlightGameObject();
            if (go != null)
                go.SetActive(enabled);
        }

        private GameObject GetHighlightGameObject()
        {
            if (highlightGraphic != null)
                return highlightGraphic.gameObject;
            return highlightObject;
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

            SceneManager.LoadSceneAsync(targetSceneName);
        }
    }
}
