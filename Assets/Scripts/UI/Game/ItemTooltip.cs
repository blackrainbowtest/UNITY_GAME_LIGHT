using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    public static class ItemTooltip
    {
        private const string ResourcesPath = "Prefabs/UI/Common/ItemTooltipModal";
        private static ItemTooltipModalController _current;
        private static ItemTooltipModalController _cachedPrefab;
        private static bool _prefabLoaded;
        private static Canvas _cachedCanvas;
        private static Canvas _overlayCanvas;

        public static void Hide()
        {
            if (_current != null)
            {
                Object.Destroy(_current.gameObject);
                _current = null;
            }
        }

        public static void Show(UnityEngine.Object itemDatabase, string itemId, Vector2 screenPoint)
        {
            Hide();

            var canvas = FindBestCanvasCached();
            if (canvas == null)
            {
                Debug.LogWarning("[ItemTooltip] No Canvas found in scene.");
                return;
            }

            var overlay = GetOrCreateOverlayCanvas(canvas);
            if (overlay == null)
                overlay = canvas;

            var prefab = GetPrefab();
            ItemTooltipModalController inst;

            if (prefab != null)
            {
                inst = Object.Instantiate(prefab, overlay.transform);
            }
            else
            {
                Debug.LogWarning($"[ItemTooltip] Missing Resources prefab at '{ResourcesPath}'. Using runtime fallback UI.");
                inst = CreateFallback(overlay.transform);
            }

            if (inst == null)
                return;

            _current = inst;
            inst.transform.SetAsLastSibling();
            inst.Show(itemDatabase, itemId, screenPoint);
        }

        internal static void NotifyDestroyed(ItemTooltipModalController controller)
        {
            if (controller != null && ReferenceEquals(_current, controller))
                _current = null;
        }

        private static ItemTooltipModalController GetPrefab()
        {
            if (_prefabLoaded)
                return _cachedPrefab;

            _prefabLoaded = true;
            _cachedPrefab = Resources.Load<ItemTooltipModalController>(ResourcesPath);
            return _cachedPrefab;
        }

        private static Canvas FindBestCanvasCached()
        {
            if (_cachedCanvas != null && _cachedCanvas.gameObject != null && _cachedCanvas.gameObject.activeInHierarchy)
            {
                if (_cachedCanvas.renderMode != RenderMode.WorldSpace)
                    return _cachedCanvas;
            }

            _cachedCanvas = FindBestCanvas();
            return _cachedCanvas;
        }

        private static Canvas FindBestCanvas()
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

            return best;
        }

        private static Canvas GetOrCreateOverlayCanvas(Canvas parentRootCanvas)
        {
            if (parentRootCanvas == null)
                return null;

            var root = parentRootCanvas.rootCanvas != null ? parentRootCanvas.rootCanvas : parentRootCanvas;

            if (_overlayCanvas != null && _overlayCanvas.gameObject != null)
            {
                // If overlay is already under the same root canvas, reuse it.
                if (_overlayCanvas.transform.IsChildOf(root.transform))
                    return _overlayCanvas;
            }

            var go = new GameObject("ItemTooltipOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.layer = root.gameObject.layer;
            go.transform.SetParent(root.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var c = go.GetComponent<Canvas>();
            c.renderMode = root.renderMode;
            c.worldCamera = root.worldCamera;
            c.planeDistance = root.planeDistance;
            c.sortingLayerID = root.sortingLayerID;
            c.overrideSorting = true;

            // "z-index": default 100, but never below the root canvas.
            int desiredOrder = Mathf.Max(100, root.sortingOrder + 1);
            c.sortingOrder = Mathf.Clamp(desiredOrder, -32768, 32767);

            _overlayCanvas = c;
            return c;
        }

        private static ItemTooltipModalController CreateFallback(Transform parent)
        {
            var root = new GameObject("ItemTooltipModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);

            var rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var backdropImg = root.GetComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.45f);
            backdropImg.raycastTarget = true;

            var backdropBtn = root.GetComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGo.layer = root.layer;
            panelGo.transform.SetParent(root.transform, false);

            var panelRt = (RectTransform)panelGo.transform;
            panelRt.sizeDelta = new Vector2(620f, 360f);
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;

            var panelImg = panelGo.GetComponent<Image>();
            panelImg.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.layer = root.layer;
            iconGo.transform.SetParent(panelGo.transform, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0f, 1f);
            iconRt.anchorMax = new Vector2(0f, 1f);
            iconRt.pivot = new Vector2(0f, 1f);
            iconRt.anchoredPosition = new Vector2(18f, -18f);
            iconRt.sizeDelta = new Vector2(96f, 96f);
            var iconImg = iconGo.GetComponent<Image>();

            var title = CreateText(panelGo.transform, "Title", new Vector2(130f, -20f), new Vector2(470f, 40f), fontSize: 28);
            var desc = CreateText(panelGo.transform, "Description", new Vector2(18f, -130f), new Vector2(584f, 150f), fontSize: 20);
            var type = CreateText(panelGo.transform, "Type", new Vector2(18f, -300f), new Vector2(280f, 30f), fontSize: 18);
            var rarity = CreateText(panelGo.transform, "Rarity", new Vector2(320f, -300f), new Vector2(282f, 30f), fontSize: 18);
            var buy = CreateText(panelGo.transform, "Buy", new Vector2(18f, -330f), new Vector2(280f, 30f), fontSize: 18);
            var sell = CreateText(panelGo.transform, "Sell", new Vector2(320f, -330f), new Vector2(282f, 30f), fontSize: 18);

            var controller = root.AddComponent<ItemTooltipModalController>();

            SetPrivate(controller, "backdropButton", backdropBtn);
            SetPrivate(controller, "panel", panelRt);
            SetPrivate(controller, "iconImage", iconImg);
            SetPrivate(controller, "titleText", title);
            SetPrivate(controller, "descriptionText", desc);
            SetPrivate(controller, "typeText", type);
            SetPrivate(controller, "rarityText", rarity);
            SetPrivate(controller, "buyPriceText", buy);
            SetPrivate(controller, "sellPriceText", sell);

            backdropBtn.onClick.AddListener(controller.Close);
            return controller;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 anchoredPos, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.color = Color.white;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.text = "";
            return t;
        }

        private static void SetPrivate(Object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f != null)
                f.SetValue(target, value);
        }
    }
}
