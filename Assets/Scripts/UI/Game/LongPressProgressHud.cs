using UnityEngine;

namespace UDA2.UI.Game
{
    public static class LongPressProgressHud
    {
        private const string ResourcesPath = "Prefabs/UI/Common/LongPressProgressRoot";

        private static LongPressProgressView _view;
        private static bool _prefabLoaded;
        private static LongPressProgressView _prefab;

        private static int _ownerId;
        private static bool _hasOwner;
        private static bool _isVisible;

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
            if (!_hasOwner || ownerId != _ownerId)
                return;

            if (!_isVisible)
                return;

            if (_view != null)
                _view.SetProgress(progress01);
        }

        public static void End(int ownerId)
        {
            if (!_hasOwner || ownerId != _ownerId)
                return;

            _hasOwner = false;
            _ownerId = 0;
            _isVisible = false;

            if (_view != null)
                _view.Hide();
        }

        private static void EnsureInstance()
        {
            if (_view != null)
                return;

            var parent = FindBestCanvasTransform();
            if (parent == null)
                return;

            var prefab = GetPrefab();
            if (prefab == null)
                return;

            _view = Object.Instantiate(prefab, parent);
            _view.gameObject.name = "LongPressProgressRoot (Runtime)";
            _view.Hide();
        }

        private static LongPressProgressView GetPrefab()
        {
            if (_prefabLoaded)
                return _prefab;

            _prefabLoaded = true;
            _prefab = Resources.Load<LongPressProgressView>(ResourcesPath);
            if (_prefab == null)
            {
                // Fallback: load GameObject and grab component (in case the component type isn't in the generic load cache).
                var go = Resources.Load<GameObject>(ResourcesPath);
                if (go != null)
                    _prefab = go.GetComponent<LongPressProgressView>();
            }

            return _prefab;
        }

        private static Transform FindBestCanvasTransform()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas best = null;
            int bestOrder = int.MinValue;

            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null || !c.gameObject.activeInHierarchy)
                    continue;

                if (c.renderMode == RenderMode.WorldSpace)
                    continue;

                var root = c.rootCanvas != null ? c.rootCanvas : c;
                int order = root.sortingOrder;
                if (best == null || order >= bestOrder)
                {
                    best = root;
                    bestOrder = order;
                }
            }

            return best != null ? best.transform : null;
        }
    }
}
