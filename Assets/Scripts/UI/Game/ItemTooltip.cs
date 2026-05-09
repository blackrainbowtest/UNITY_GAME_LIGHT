using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UDA2.UI.Game
{
    /// <summary>
    /// Static facade for the item long-press tooltip.
    ///
    /// Architecture:
    ///   Show() creates a dedicated ScreenSpaceOverlay canvas (sortingOrder=999) so the tooltip
    ///   is guaranteed to render above ALL other UI in the scene — regardless of which canvas the
    ///   item slot lives on or what child-canvases are present.  The modal prefab is instantiated
    ///   directly on this overlay; Awake() runs immediately (canvas is active from the start) so
    ///   positioning and backdrop wiring both work correctly.
    ///   Hide() / NotifyDestroyed() destroy the modal and tear down the overlay canvas.
    /// </summary>
    public static class ItemTooltip
    {
        private const string ResourcesPath = "Prefabs/UI/Common/ItemTooltipModal";
        private const int OverlaySortingOrder = 999;

        private static ItemTooltipModalController _current;
        private static ItemTooltipModalController _cachedPrefab;
        private static bool _prefabLoaded;
        private static Canvas _overlayCanvas;

        public static bool IsVisible =>
            _current != null &&
            _current.gameObject != null &&
            _current.gameObject.activeInHierarchy;

        // ─── Public API ────────────────────────────────────────────────────────

        public static void Show(UnityEngine.Object itemDatabase, string itemId, Vector2 screenPoint)
        {
            Hide();

            var overlay = GetOrCreateOverlay();

            var prefab = GetPrefab();
            ItemTooltipModalController inst = prefab != null
                ? Object.Instantiate(prefab, overlay.transform)
                : CreateFallback(overlay.transform);

            if (inst == null)
                return;

            _current = inst;
            // Awake() has already run (overlay is active), so Show() works correctly.
            inst.Show(itemDatabase, itemId, screenPoint);
        }

        public static void Hide()
        {
            if (_current == null)
                return;

            var go = _current.gameObject;
            _current = null;
            if (go != null)
                Object.Destroy(go);

            DestroyOverlay();
        }

        // Called by ItemTooltipModalController.OnDestroy so the static ref stays clean.
        internal static void NotifyDestroyed(ItemTooltipModalController controller)
        {
            if (controller != null && ReferenceEquals(_current, controller))
                _current = null;

            DestroyOverlay();
        }

        // ─── Overlay canvas ────────────────────────────────────────────────────

        /// <summary>
        /// Returns (creating if necessary) a standalone ScreenSpaceOverlay root canvas at
        /// sortingOrder=999.  It is always active, so the modal's Awake() fires immediately
        /// during Instantiate and PositionAt() has correct canvas dimensions from the start.
        /// </summary>
        private static Canvas GetOrCreateOverlay()
        {
            if (_overlayCanvas != null && _overlayCanvas.gameObject != null)
                return _overlayCanvas;

            var go = new GameObject("ItemTooltipOverlay");

            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = OverlaySortingOrder;

            // Copy CanvasScaler from the game's primary UI canvas so the modal dimensions
            // (sizeDelta, font sizes, offsets) match the rest of the UI.
            CopyCanvasScaler(go, FindBestSceneCanvas());

            go.AddComponent<GraphicRaycaster>();

            _overlayCanvas = c;
            return c;
        }

        private static void DestroyOverlay()
        {
            if (_overlayCanvas == null || _overlayCanvas.gameObject == null)
                return;
            // Only destroy when no modal is alive.
            if (_current != null && _current.gameObject != null)
                return;

            Object.Destroy(_overlayCanvas.gameObject);
            _overlayCanvas = null;
        }

        /// <summary>Finds the active root canvas (non-WorldSpace) with the highest sortingOrder.</summary>
        private static Canvas FindBestSceneCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Canvas best = null;
            int bestOrder = int.MinValue;

            foreach (var c in canvases)
            {
                if (c == null || c.renderMode == RenderMode.WorldSpace)
                    continue;
                if (c.rootCanvas != c)
                    continue; // skip child canvases

                int order = c.sortingOrder;
                if (best == null || order >= bestOrder)
                {
                    best = c;
                    bestOrder = order;
                }
            }
            return best;
        }

        private static void CopyCanvasScaler(GameObject target, Canvas source)
        {
            if (source == null)
                return;
            var srcScaler = source.GetComponent<CanvasScaler>();
            if (srcScaler == null)
                return;

            var dst = target.AddComponent<CanvasScaler>();
            dst.uiScaleMode          = srcScaler.uiScaleMode;
            dst.referenceResolution  = srcScaler.referenceResolution;
            dst.screenMatchMode      = srcScaler.screenMatchMode;
            dst.matchWidthOrHeight   = srcScaler.matchWidthOrHeight;
            dst.referencePixelsPerUnit = srcScaler.referencePixelsPerUnit;
        }

        // ─── Prefab loading ────────────────────────────────────────────────────

        private static ItemTooltipModalController GetPrefab()
        {
            if (_prefabLoaded)
                return _cachedPrefab;

            _prefabLoaded = true;
            _cachedPrefab = Resources.Load<ItemTooltipModalController>(ResourcesPath);
            if (_cachedPrefab == null)
                UDA2.Logging.Logger.LogWarning(
                    $"[ItemTooltip] Prefab not found at 'Resources/{ResourcesPath}'. Runtime fallback will be used.",
                    UDA2.Logging.LogChannel.UI);
            return _cachedPrefab;
        }

        // ─── Legacy stubs (kept so callers referencing old API compile) ────────

        // Removed: FindBestCanvasCached, FindBestCanvas, GetOrCreateOverlayCanvas,
        //          CleanupOverlayRuntimeState, SetOverlayCanvasInteractive.
        // If you see compile errors from old callers, remove those call-sites.

        // ─── Fallback modal (used when the prefab is missing) ─────────────────

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

            var title = CreateText(panelGo.transform, "Title",       new Vector2(130f, -20f),  new Vector2(470f, 40f),  fontSize: 28);
            var desc  = CreateText(panelGo.transform, "Description", new Vector2(18f,  -130f), new Vector2(584f, 150f), fontSize: 20);
            var type  = CreateText(panelGo.transform, "Type",        new Vector2(18f,  -300f), new Vector2(280f, 30f),  fontSize: 18);
            var rarity= CreateText(panelGo.transform, "Rarity",      new Vector2(320f, -300f), new Vector2(282f, 30f),  fontSize: 18);
            var buy   = CreateText(panelGo.transform, "Buy",         new Vector2(18f,  -330f), new Vector2(280f, 30f),  fontSize: 18);
            var sell  = CreateText(panelGo.transform, "Sell",        new Vector2(320f, -330f), new Vector2(282f, 30f),  fontSize: 18);

            var controller = root.AddComponent<ItemTooltipModalController>();

            SetPrivate(controller, "backdropButton",  backdropBtn);
            SetPrivate(controller, "panel",           panelRt);
            SetPrivate(controller, "iconImage",       iconImg);
            SetPrivate(controller, "titleText",       title);
            SetPrivate(controller, "descriptionText", desc);
            SetPrivate(controller, "typeText",        type);
            SetPrivate(controller, "rarityText",      rarity);
            SetPrivate(controller, "buyPriceText",    buy);
            SetPrivate(controller, "sellPriceText",   sell);

            return controller;
        }

        // ─── Removed old methods that are now dead code ────────────────────────
        // FindBestCanvasCached / FindBestCanvas / GetOrCreateOverlayCanvas /
        // CleanupOverlayRuntimeState / SetOverlayCanvasInteractive are gone.
        // The compiler will surface any remaining call-sites.

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
            var f = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            f?.SetValue(target, value);
        }

    }
}
