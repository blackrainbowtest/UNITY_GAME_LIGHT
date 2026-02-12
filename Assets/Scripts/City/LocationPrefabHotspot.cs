using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace UDA2.City
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class LocationPrefabHotspot : MonoBehaviour
    {
        [Header("Open Prefab")]
        [SerializeField] private GameObject contentPrefab;
        [Tooltip("Optional parent override for spawned content. If empty, parent Canvas is auto-detected.")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private bool reuseOpenedInstance = true;

        [Header("Inspect Mode")]
        [Tooltip("If true, this hotspot can be clicked only while inspect mode is ON.")]
        [SerializeField] private bool interactableOnlyInInspectMode;

        [Header("Visual")]
        [Tooltip("Optional: a highlight object enabled when Inspect Mode is ON.")]
        [FormerlySerializedAs("highlight")]
        [SerializeField] private GameObject highlightObject;
        [Tooltip("Optional: drag Graphic directly here instead of highlightObject.")]
        [SerializeField] private Graphic highlightGraphic;

        private Button _button;
        private GameObject _openedInstance;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);

            if (highlightGraphic == null && highlightObject != null)
                highlightGraphic = highlightObject.GetComponent<Graphic>();

            var highlightGo = GetHighlightGameObject();
            if (highlightGo != null)
                highlightGo.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        public void SetInspectMode(bool enabled)
        {
            SetHighlight(enabled);

            if (_button != null && interactableOnlyInInspectMode)
                _button.interactable = enabled;
        }

        public void SetHighlight(bool enabled)
        {
            var highlightGo = GetHighlightGameObject();
            if (highlightGo != null)
                highlightGo.SetActive(enabled);
        }

        public void CloseOpened()
        {
            if (_openedInstance == null)
                return;

            Destroy(_openedInstance);
            _openedInstance = null;
        }

        private GameObject GetHighlightGameObject()
        {
            if (highlightGraphic != null)
                return highlightGraphic.gameObject;
            return highlightObject;
        }

        private void HandleClick()
        {
            if (contentPrefab == null)
            {
                Debug.LogWarning($"[LocationPrefabHotspot] contentPrefab is not assigned on '{name}'", this);
                return;
            }

            if (reuseOpenedInstance && _openedInstance != null)
            {
                _openedInstance.SetActive(true);
                _openedInstance.transform.SetAsLastSibling();
                return;
            }

            Transform parent = ResolveContentParent();
            _openedInstance = Instantiate(contentPrefab, parent, worldPositionStays: false);
            if (_openedInstance != null)
                _openedInstance.transform.SetAsLastSibling();
        }

        private Transform ResolveContentParent()
        {
            if (contentParent != null)
                return contentParent;

            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
                return parentCanvas.transform;

            return FindBestCanvasTransform();
        }

        private static Transform FindBestCanvasTransform()
        {
            Canvas[] canvases;
#if UNITY_2023_1_OR_NEWER || UNITY_2022_2_OR_NEWER
            canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            canvases = Object.FindObjectsOfType<Canvas>(true);
#pragma warning restore CS0618
#endif

            if (canvases == null || canvases.Length == 0)
                return null;

            Canvas best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null || !canvas.isActiveAndEnabled)
                    continue;

                int score = 0;
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    score += 1000;
                score += canvas.sortingOrder;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = canvas;
                }
            }

            return best != null ? best.transform : null;
        }
    }
}