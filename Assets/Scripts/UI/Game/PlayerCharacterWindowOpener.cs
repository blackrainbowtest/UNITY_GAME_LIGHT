using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    public sealed class PlayerCharacterWindowOpener : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("If null, will try to use Button on the same GameObject.")]
        [SerializeField] private Button openButton;

        [SerializeField] private GameObject windowPrefab;

        [Tooltip("Optional parent override (e.g. your HUD Canvas transform).")]
        [SerializeField] private Transform parentOverride;

        [Header("Debug")]
        [SerializeField] private bool logUIInfoOnOpen = true;

        [Tooltip("If the window prefab contains its own Canvas, force it to be on top by overriding sorting.")]
        [SerializeField] private bool forceWindowCanvasOnTop = true;

        [SerializeField] private int forcedSortingOrder = 5000;

        private GameObject _instance;

        private void Awake()
        {
            if (openButton == null)
                openButton = GetComponent<Button>();

            if (openButton != null)
                openButton.onClick.AddListener(Open);
        }

        private void OnDestroy()
        {
            if (openButton != null)
                openButton.onClick.RemoveListener(Open);
        }

        public void Open()
        {
            if (windowPrefab == null)
            {
                Debug.LogWarning("[PlayerCharacterWindowOpener] Window prefab is not assigned.");
                return;
            }

            if (_instance != null)
            {
                _instance.SetActive(true);
                _instance.transform.SetAsLastSibling();
                return;
            }

            Transform parent = parentOverride != null ? parentOverride : FindParentCanvasTransform();
            if (parent == null)
            {
                parent = FindBestCanvasTransform();
            }

            _instance = Instantiate(windowPrefab, parent, worldPositionStays: false);
            if (_instance == null)
            {
                Debug.LogError("[PlayerCharacterWindowOpener] Instantiate returned null.");
                return;
            }

            _instance.SetActive(true);
            _instance.transform.SetAsLastSibling();

            var windowController = _instance.GetComponentInChildren<PlayerCharacterWindowController>(true);
            if (windowController != null)
                windowController.SetOwnerRoot(_instance);

            if (forceWindowCanvasOnTop)
                TryForceCanvasOnTop(_instance);

            if (logUIInfoOnOpen)
                LogUIInfo(_instance, parent);

            Debug.Log($"[PlayerCharacterWindowOpener] Opened '{_instance.name}' under '{(parent != null ? parent.name : "<root>")}'");

            var closeHandler = _instance.GetComponent<global::IMenuCloseHandler>();
            if (closeHandler != null)
                closeHandler.OnMenuClosed += HandleClosed;
        }

        private void HandleClosed()
        {
            if (_instance == null)
                return;

            var closeHandler = _instance.GetComponent<global::IMenuCloseHandler>();
            if (closeHandler != null)
                closeHandler.OnMenuClosed -= HandleClosed;

            _instance = null;
        }

        private Transform FindParentCanvasTransform()
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.transform : null;
        }

        private static Transform FindBestCanvasTransform()
        {
            // Prefer ScreenSpaceOverlay with highest sortingOrder.
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
                var c = canvases[i];
                if (c == null || !c.isActiveAndEnabled)
                    continue;

                int score = 0;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) score += 1000;
                score += c.sortingOrder;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best != null ? best.transform : null;
        }

        private void TryForceCanvasOnTop(GameObject instance)
        {
            if (instance == null)
                return;

            var canvas = instance.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
                return;

            canvas.overrideSorting = true;
            canvas.sortingOrder = forcedSortingOrder;
        }

        private void LogUIInfo(GameObject instance, Transform parent)
        {
            if (instance == null)
                return;

            var rt = instance.transform as RectTransform;
            if (rt != null)
            {
                Debug.Log($"[PlayerCharacterWindowOpener] RectTransform: anchoredPos={rt.anchoredPosition} sizeDelta={rt.sizeDelta} anchorMin={rt.anchorMin} anchorMax={rt.anchorMax} scale={rt.localScale}");
            }

            var instanceCanvas = instance.GetComponentInChildren<Canvas>(true);
            if (instanceCanvas != null)
            {
                Debug.Log($"[PlayerCharacterWindowOpener] Window Canvas: enabled={instanceCanvas.enabled} renderMode={instanceCanvas.renderMode} overrideSorting={instanceCanvas.overrideSorting} sortingOrder={instanceCanvas.sortingOrder} worldCamera={(instanceCanvas.worldCamera != null ? instanceCanvas.worldCamera.name : "null")}");
            }
            else
            {
                Debug.Log("[PlayerCharacterWindowOpener] Window has no Canvas (will rely on parent Canvas).");
            }

            var parentCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            if (parentCanvas != null)
            {
                Debug.Log($"[PlayerCharacterWindowOpener] Parent Canvas: name={parentCanvas.name} renderMode={parentCanvas.renderMode} sortingOrder={parentCanvas.sortingOrder}");
            }
        }
    }
}
