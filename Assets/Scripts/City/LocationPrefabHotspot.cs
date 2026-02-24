using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System.Collections.Generic;

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
        private bool _ownsGlobalUiHideRequest;

        private static int s_globalUiHideRequestCount;
        private static readonly Dictionary<GameObject, bool> s_globalUiOriginalStates = new Dictionary<GameObject, bool>(8);

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
            _openedInstance = Instantiate(contentPrefab, parent, worldPositionStays: false);
            if (_openedInstance != null)
            {
                RequestHideGlobalUiIfNeeded();
                EnsureOpenedLifecycleWatcher(_openedInstance);
                _openedInstance.transform.SetAsLastSibling();
            }
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

            if (s_globalUiHideRequestCount == 0)
            {
                s_globalUiOriginalStates.Clear();
                var roots = FindGlobalUiRoots();
                for (int i = 0; i < roots.Count; i++)
                {
                    var root = roots[i];
                    if (root == null)
                        continue;

                    if (!s_globalUiOriginalStates.ContainsKey(root))
                        s_globalUiOriginalStates[root] = root.activeSelf;

                    root.SetActive(false);
                }
            }

            s_globalUiHideRequestCount++;
            _ownsGlobalUiHideRequest = true;
        }

        private void ReleaseGlobalUiHideIfOwned()
        {
            if (!_ownsGlobalUiHideRequest)
                return;

            _ownsGlobalUiHideRequest = false;
            s_globalUiHideRequestCount = Mathf.Max(0, s_globalUiHideRequestCount - 1);

            if (s_globalUiHideRequestCount > 0)
                return;

            foreach (var pair in s_globalUiOriginalStates)
            {
                var root = pair.Key;
                if (root == null)
                    continue;

                root.SetActive(pair.Value);
            }

            s_globalUiOriginalStates.Clear();
        }

        private static List<GameObject> FindGlobalUiRoots()
        {
            var roots = new List<GameObject>(4);
            MonoBehaviour[] all;
#if UNITY_2023_1_OR_NEWER || UNITY_2022_2_OR_NEWER
            all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            all = Object.FindObjectsOfType<MonoBehaviour>(true);
#pragma warning restore CS0618
#endif

            if (all == null || all.Length == 0)
                return roots;

            for (int i = 0; i < all.Length; i++)
            {
                var component = all[i];
                if (component == null)
                    continue;

                var type = component.GetType();
                if (type == null)
                    continue;

                if (!string.Equals(type.FullName, "UDA2.UI.Game.GlobalUISceneBinder", System.StringComparison.Ordinal))
                    continue;

                var go = component.gameObject;
                var root = go != null && go.transform != null && go.transform.root != null
                    ? go.transform.root.gameObject
                    : go;

                if (root == null)
                    continue;

                bool exists = false;
                for (int j = 0; j < roots.Count; j++)
                {
                    if (roots[j] == root)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    roots.Add(root);
            }

            return roots;
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