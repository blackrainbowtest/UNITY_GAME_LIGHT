using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    /// <summary>
    /// Static service that shows a circular long-press progress indicator.
    ///
    /// Uses a dedicated ScreenSpaceOverlay canvas (sortingOrder=998) — always rendered above
    /// game UI but below the item tooltip overlay (sortingOrder=999).  Because the canvas is a
    /// root canvas, LongPressProgressView.Show() correctly resolves the full-screen rect when
    /// calling GetComponentInParent&lt;Canvas&gt;() for screen-to-canvas coordinate conversion.
    /// </summary>
    public static class LongPressProgressHud
    {
        private const string ResourcesPath = "Prefabs/UI/Common/LongPressProgressRoot";
        private const int CanvasSortingOrder = 998; // just below ItemTooltipOverlay (999)

        private static LongPressProgressView _view;
        private static Canvas _canvas;
        private static bool _prefabLoaded;
        private static LongPressProgressView _prefab;

        private static int _ownerId;
        private static bool _hasOwner;
        private static bool _isVisible;

        // ─── Public API ───────────────────────────────────────────────────────

        public static void Begin(int ownerId)
        {
            _ownerId = ownerId;
            _hasOwner = true;
            _isVisible = false;
            EnsureInstance();
            if (_view != null)
                _view.Hide();
        }

        public static void Show(int ownerId, Vector2 screenPos)
        {
            if (!_hasOwner || ownerId != _ownerId)
                return;

            EnsureInstance();
            if (_view == null)
                return;

            _isVisible = true;
            _view.Show(screenPos);
        }

        public static void SetProgress(int ownerId, float progress01)
        {
            if (!_hasOwner || ownerId != _ownerId || !_isVisible)
                return;

            _view?.SetProgress(progress01);
        }

        public static void End(int ownerId)
        {
            if (!_hasOwner || ownerId != _ownerId)
                return;

            _hasOwner = false;
            _ownerId = 0;
            _isVisible = false;
            _view?.Hide();
        }

        // ─── Internal ─────────────────────────────────────────────────────────

        private static void EnsureInstance()
        {
            if (_view != null)
                return;

            var canvas = GetOrCreateCanvas();
            if (canvas == null)
                return;

            var prefab = GetPrefab();
            if (prefab == null)
                return;

            _view = Object.Instantiate(prefab, canvas.transform);
            _view.gameObject.name = "LongPressProgressRoot (Runtime)";
            _view.Hide();
        }

        /// <summary>
        /// Returns (creating if necessary) a standalone ScreenSpaceOverlay canvas at
        /// sortingOrder=998.  Being a ROOT canvas means GetComponentInParent&lt;Canvas&gt;()
        /// inside LongPressProgressView finds a full-screen rect for correct positioning.
        /// </summary>
        private static Canvas GetOrCreateCanvas()
        {
            if (_canvas != null && _canvas.gameObject != null)
                return _canvas;

            var go = new GameObject("LongPressProgressOverlay");
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = CanvasSortingOrder;

            // Copy CanvasScaler from the game's primary canvas so sizes match.
            CopyCanvasScaler(go, FindBestSceneCanvas());

            // No GraphicRaycaster — the circle is visual only, no input needed.

            _canvas = c;
            return c;
        }

        private static Canvas FindBestSceneCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Canvas best = null;
            int bestOrder = int.MinValue;

            foreach (var c in canvases)
            {
                if (c == null || c.renderMode == RenderMode.WorldSpace || c.rootCanvas != c)
                    continue;

                if (best == null || c.sortingOrder >= bestOrder)
                {
                    best = c;
                    bestOrder = c.sortingOrder;
                }
            }
            return best;
        }

        private static void CopyCanvasScaler(GameObject target, Canvas source)
        {
            if (source == null) return;
            var src = source.GetComponent<CanvasScaler>();
            if (src == null) return;

            var dst = target.AddComponent<CanvasScaler>();
            dst.uiScaleMode          = src.uiScaleMode;
            dst.referenceResolution  = src.referenceResolution;
            dst.screenMatchMode      = src.screenMatchMode;
            dst.matchWidthOrHeight   = src.matchWidthOrHeight;
            dst.referencePixelsPerUnit = src.referencePixelsPerUnit;
        }

        private static LongPressProgressView GetPrefab()
        {
            if (_prefabLoaded)
                return _prefab;

            _prefabLoaded = true;
            _prefab = Resources.Load<LongPressProgressView>(ResourcesPath);
            if (_prefab == null)
            {
                var go = Resources.Load<GameObject>(ResourcesPath);
                if (go != null)
                    _prefab = go.GetComponent<LongPressProgressView>();
            }
            return _prefab;
        }
    }
}
