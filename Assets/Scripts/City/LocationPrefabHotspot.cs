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
        [Tooltip("Optional frame prefab. If empty, only content prefab is spawned (legacy mode). If assigned, frame is spawned and content is inserted into frame content root.")]
        [SerializeField] private GameObject framePrefab;
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
        private bool _ownsGlobalUiHideRequest;

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

            ReleaseGlobalUiHideIfOwned();
        }

        private void OnDisable()
        {
            ReleaseGlobalUiHideIfOwned();
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
                RequestHideGlobalUiIfNeeded();
                EnsureOpenedLifecycleWatcher(_openedInstance);
                _openedInstance.SetActive(true);
                _openedInstance.transform.SetAsLastSibling();
                return;
            }

            Transform parent = ResolveContentParent();
            _openedInstance = SpawnOpenedInstance(parent);
            if (_openedInstance != null)
            {
                RequestHideGlobalUiIfNeeded();
                EnsureOpenedLifecycleWatcher(_openedInstance);
                _openedInstance.transform.SetAsLastSibling();
            }
        }

        private GameObject SpawnOpenedInstance(Transform parent)
        {
            if (framePrefab == null)
                return Instantiate(contentPrefab, parent, worldPositionStays: false);

            var frameInstance = Instantiate(framePrefab, parent, worldPositionStays: false);
            if (frameInstance == null)
                return null;

            var frameComponent = ResolveFrameComponent(frameInstance);
            var contentRoot = ResolveFrameContentRoot(frameInstance, frameComponent);
            if (contentRoot == null)
                contentRoot = frameInstance.transform;

            var contentInstance = Instantiate(contentPrefab, contentRoot, worldPositionStays: false);
            if (contentInstance != null)
            {
                contentInstance.transform.SetAsLastSibling();
                BindFrameHeaderFromContent(frameComponent, contentInstance);
            }

            SetupFrameClose(frameInstance, frameComponent);
            return frameInstance;
        }

        private static LocationWindowFrame ResolveFrameComponent(GameObject frameInstance)
        {
            if (frameInstance == null)
                return null;

            var frame = frameInstance.GetComponent<LocationWindowFrame>();
            if (frame == null)
                frame = frameInstance.GetComponentInChildren<LocationWindowFrame>(includeInactive: true);

            return frame;
        }

        private static Transform ResolveFrameContentRoot(GameObject frameInstance, LocationWindowFrame frame)
        {
            if (frame != null)
                return frame.ContentRoot;

            if (frameInstance != null)
            {
                Debug.LogWarning($"[LocationPrefabHotspot] Frame prefab '{frameInstance.name}' has no LocationWindowFrame component. Content will be spawned on frame root.");
            }

            return frameInstance.transform;
        }

        private static void BindFrameHeaderFromContent(LocationWindowFrame frame, GameObject contentInstance)
        {
            if (frame == null || contentInstance == null)
                return;

            var contentMeta = contentInstance.GetComponent<LocationWindowContentMeta>();
            if (contentMeta == null)
                contentMeta = contentInstance.GetComponentInChildren<LocationWindowContentMeta>(includeInactive: true);

            if (contentMeta == null)
            {
                // Optional by design: keep frame header as-is when content meta is absent.
                return;
            }

            frame.SetHeaderTitle(contentMeta.TitleLocalizationKey, contentMeta.TitleFallbackText);
            contentMeta.ApplyTitleToContentIfAssigned();
        }

        private void SetupFrameClose(GameObject frameInstance, LocationWindowFrame frame)
        {
            if (frameInstance == null)
                return;

            if (frame == null)
            {
                Debug.LogWarning($"[LocationPrefabHotspot] Close is not wired because '{frameInstance.name}' has no LocationWindowFrame component.");
                return;
            }

            if (frame.CloseButton == null)
            {
                Debug.LogWarning($"[LocationPrefabHotspot] Close is not wired because LocationWindowFrame on '{frameInstance.name}' has no closeButton assigned.");
                return;
            }

            frame.CloseButton.onClick.AddListener(CloseOpened);
        }

        private void EnsureOpenedLifecycleWatcher(GameObject opened)
        {
            if (opened == null)
                return;

            var watcher = opened.GetComponent<OpenedContentLifecycleWatcher>();
            if (watcher == null)
                watcher = opened.AddComponent<OpenedContentLifecycleWatcher>();

            watcher.Bind(this, opened);
        }

        private void OnOpenedContentClosed(GameObject opened)
        {
            if (_openedInstance == opened)
                _openedInstance = null;

            ReleaseGlobalUiHideIfOwned();
        }

        private void RequestHideGlobalUiIfNeeded()
        {
            if (_ownsGlobalUiHideRequest)
                return;

            LocationGlobalUiVisibility.RequestHide(this);
            _ownsGlobalUiHideRequest = true;
        }

        private void ReleaseGlobalUiHideIfOwned()
        {
            if (!_ownsGlobalUiHideRequest)
                return;

            _ownsGlobalUiHideRequest = false;
            LocationGlobalUiVisibility.ReleaseHide(this);
        }

        private sealed class OpenedContentLifecycleWatcher : MonoBehaviour
        {
            private LocationPrefabHotspot _owner;
            private GameObject _tracked;
            private bool _notified;

            public void Bind(LocationPrefabHotspot owner, GameObject tracked)
            {
                _owner = owner;
                _tracked = tracked;
                _notified = false;
            }

            private void OnDisable()
            {
                NotifyOwner();
            }

            private void OnDestroy()
            {
                NotifyOwner();
            }

            private void NotifyOwner()
            {
                if (_notified)
                    return;

                _notified = true;
                if (_owner != null)
                    _owner.OnOpenedContentClosed(_tracked != null ? _tracked : gameObject);
            }
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