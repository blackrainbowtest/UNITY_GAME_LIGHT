using UnityEngine;
using UnityEngine.UI;

public sealed class BattleActionTooltipModalController : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Full-screen backdrop object (usually a Button/Image) that catches clicks to close tooltip.")]
    [SerializeField] private Button backdropButton;
    [SerializeField] private RectTransform panel;

    [Header("Main Text")]
    [SerializeField] private LocalizedGlobalComponent titleLocalized;
    [SerializeField] private LocalizedGlobalComponent descriptionLocalized;

    [Header("Optional Rows")]
    [Tooltip("Optional group root that contains Damage + Heal texts.")]
    [SerializeField] private GameObject damageHealRowRoot;
    [Tooltip("Optional group root that contains MP/SP/LP cost texts.")]
    [SerializeField] private GameObject spellCostRowRoot;

    [SerializeField] private GameObject damageRow;
    [SerializeField] private LocalizedGlobalComponent damageLocalized;

    [SerializeField] private GameObject healRow;
    [SerializeField] private LocalizedGlobalComponent healLocalized;

    [SerializeField] private GameObject mpCostRow;
    [SerializeField] private LocalizedGlobalComponent mpCostLocalized;

    [SerializeField] private GameObject spCostRow;
    [SerializeField] private LocalizedGlobalComponent spCostLocalized;

    [SerializeField] private GameObject lpCostRow;
    [SerializeField] private LocalizedGlobalComponent lpCostLocalized;

    [Header("Layout")]
    [SerializeField] private bool clampToSafeArea = true;
    [SerializeField] private float safeMargin = 16f;
    [SerializeField] private float verticalAnchorOffset = 130f;
    [SerializeField] private float horizontalAnchorOffset = 24f;
    [SerializeField] private bool debugLocalization;
    [SerializeField] private bool debugPositioning;

    public bool IsVisible => gameObject.activeSelf;

    private bool warnedTitleMissing;
    private bool warnedDescriptionMissing;

    // If the tooltip is shown while the pointer is already down (long-press),
    // the newly-activated backdrop can receive a PointerDown and close immediately.
    // We ignore close input for a short window after Show().
    private float ignoreBackdropCloseUntilUnscaledTime;
    private float lastShowUnscaledTime;
    private bool showRequested;

    private void Awake()
    {
        AutoAssignLocalizedFields();

        // Close should happen on NEXT tap/click, not on the release of the long-press.
        // Using onClick may close immediately when user lifts finger after hold.
        if (backdropButton != null)
        {
            // Ensure no accidental double wiring.
            backdropButton.onClick.RemoveListener(Hide);

            var catcher = backdropButton.GetComponent<BattleTooltipBackdropCloseCatcher>();
            if (catcher == null)
                catcher = backdropButton.gameObject.AddComponent<BattleTooltipBackdropCloseCatcher>();
            catcher.Bind(this);
        }

        // If this modal is initially inactive in the scene, Awake() will run on the first Show().
        // In that case we must NOT immediately Hide(), otherwise the first long-press will appear to fail.
        if (!showRequested)
            Hide();
    }

    private void OnDestroy()
    {
        if (backdropButton != null)
            backdropButton.onClick.RemoveListener(Hide);
    }

    public void Show(in BattleButtonTooltipData data, Vector2 screenPoint)
    {
        showRequested = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        ignoreBackdropCloseUntilUnscaledTime = Time.unscaledTime + 0.05f;
        lastShowUnscaledTime = Time.unscaledTime;

        if (debugPositioning)
        {
            UDA2.Logging.Logger.LogInfo($"[BattleTooltip] Activated: activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}", UDA2.Logging.LogChannel.UI, this);
            if (!gameObject.activeInHierarchy)
            {
                var t = transform;
                int depth = 0;
                while (t != null && depth < 12)
                {
                    UDA2.Logging.Logger.LogInfo($"[BattleTooltip] Parent[{depth}]: '{t.name}' activeSelf={t.gameObject.activeSelf}", UDA2.Logging.LogChannel.UI, this);
                    t = t.parent;
                    depth++;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(data.TitleKey) && debugLocalization)
            Debug.LogWarning("BattleActionTooltipModalController: TitleKey is empty.", this);
        if (string.IsNullOrWhiteSpace(data.DescriptionKey) && debugLocalization)
            Debug.LogWarning("BattleActionTooltipModalController: DescriptionKey is empty.", this);

        ApplyLocalized(titleLocalized, data.TitleKey);
        ApplyLocalized(descriptionLocalized, data.DescriptionKey);

        bool damageVisible = ApplyRow(damageRow, damageLocalized, data.DamageFormatKey, data.Damage);
        bool healVisible = ApplyRow(healRow, healLocalized, data.HealFormatKey, data.Heal);
        bool mpVisible = ApplyRow(mpCostRow, mpCostLocalized, data.MpCostFormatKey, data.MpCost);
        bool spVisible = ApplyRow(spCostRow, spCostLocalized, data.SpCostFormatKey, data.SpCost);
        bool lpVisible = ApplyRow(lpCostRow, lpCostLocalized, data.LpCostFormatKey, data.LpCost);

        if (damageHealRowRoot != null)
            damageHealRowRoot.SetActive(damageVisible || healVisible);
        if (spellCostRowRoot != null)
            spellCostRowRoot.SetActive(mpVisible || spVisible || lpVisible);

        // Important: if this panel was inactive, layout may not be calculated yet.
        // Without a rebuild, panel.rect.size can be zero on the first show, producing wrong positioning.
        Canvas.ForceUpdateCanvases();
        if (panel != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

        if (debugPositioning && panel != null)
            UDA2.Logging.Logger.LogInfo($"[BattleTooltip] Show: screen={screenPoint}, panelSize={panel.rect.size}, pivot={panel.pivot}", UDA2.Logging.LogChannel.UI, this);

        PositionAt(screenPoint);

        showRequested = false;
    }

    public void OnBackdropPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (Time.unscaledTime < ignoreBackdropCloseUntilUnscaledTime)
        {
            if (debugPositioning)
                UDA2.Logging.Logger.LogInfo("[BattleTooltip] Backdrop close ignored (arming delay)", UDA2.Logging.LogChannel.UI, this);
            return;
        }

        Hide();

        // Consume so it doesn't click through.
        if (eventData != null)
            eventData.Use();
    }

    public void Hide()
    {
        if (debugPositioning)
        {
            float dt = Time.unscaledTime - lastShowUnscaledTime;
            if (dt >= 0f && dt < 0.25f)
                UDA2.Logging.Logger.LogInfo($"[BattleTooltip] Hide called after {dt:0.###}s\n{System.Environment.StackTrace}", UDA2.Logging.LogChannel.UI, this);
        }
        gameObject.SetActive(false);
    }

    private void AutoAssignLocalizedFields()
    {
        if (titleLocalized == null)
        {
            var localized = FindLocalizedByName("title");
            if (localized != null)
                titleLocalized = localized;
        }

        if (descriptionLocalized == null)
        {
            var localized = FindLocalizedByName("description");
            if (localized != null)
                descriptionLocalized = localized;
        }

        if (damageLocalized == null)
            damageLocalized = FindLocalizedByName("damage");
        if (healLocalized == null)
            healLocalized = FindLocalizedByName("heal");
        if (mpCostLocalized == null)
            mpCostLocalized = FindLocalizedByName("mp");
        if (spCostLocalized == null)
            spCostLocalized = FindLocalizedByName("sp");
        if (lpCostLocalized == null)
            lpCostLocalized = FindLocalizedByName("lp");

        if (titleLocalized == null && !warnedTitleMissing)
        {
            warnedTitleMissing = true;
            Debug.LogWarning("BattleActionTooltipModalController: Title LocalizedGlobalComponent is not assigned/found.", this);
        }

        if (descriptionLocalized == null && !warnedDescriptionMissing)
        {
            warnedDescriptionMissing = true;
            Debug.LogWarning("BattleActionTooltipModalController: Description LocalizedGlobalComponent is not assigned/found.", this);
        }
    }

    private LocalizedGlobalComponent FindLocalizedByName(string contains)
    {
        if (string.IsNullOrWhiteSpace(contains))
            return null;

        var all = GetComponentsInChildren<LocalizedGlobalComponent>(includeInactive: true);
        for (int i = 0; i < all.Length; i++)
        {
            var candidate = all[i];
            if (candidate == null)
                continue;

            if (candidate.name.IndexOf(contains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return candidate;
        }

        return null;
    }

    private static void ApplyLocalized(LocalizedGlobalComponent localized, string key, params object[] args)
    {
        if (localized == null)
            return;

        localized.Key = key;
        if (args != null && args.Length > 0)
            localized.SetArgs(args);
        else
            localized.ClearArgs();
    }

    private static bool ApplyRow(GameObject rowRoot, LocalizedGlobalComponent localized, string key, int value)
    {
        bool visible = value > 0 && !string.IsNullOrWhiteSpace(key) && localized != null;

        if (rowRoot != null)
            rowRoot.SetActive(visible);

        if (localized != null)
            localized.gameObject.SetActive(visible);

        if (!visible)
            return false;

        ApplyLocalized(localized, key, value);
        return true;
    }

    private void PositionAt(Vector2 screenPoint)
    {
        if (panel == null)
            return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        var canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        var cameraForCanvas = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cameraForCanvas, out var localPoint))
            return;

        var clampRect = GetClampRectLocal(canvas, canvasRect);
        clampRect.xMin += safeMargin;
        clampRect.xMax -= safeMargin;
        clampRect.yMin += safeMargin;
        clampRect.yMax -= safeMargin;

        var panelSize = panel.rect.size;
        var pivot = panel.pivot;

        float xMin = clampRect.xMin + panelSize.x * pivot.x;
        float xMax = clampRect.xMax - panelSize.x * (1f - pivot.x);
        float yMin = clampRect.yMin + panelSize.y * pivot.y;
        float yMax = clampRect.yMax - panelSize.y * (1f - pivot.y);

        float xSign = localPoint.x > clampRect.center.x ? -1f : 1f;
        float targetX = localPoint.x + Mathf.Abs(horizontalAnchorOffset) * xSign;

        float stepY = Mathf.Max(0f, verticalAnchorOffset);
        float upY = localPoint.y + stepY;
        float downY = localPoint.y - stepY;

        bool upFits = upY >= yMin && upY <= yMax;
        bool downFits = downY >= yMin && downY <= yMax;

        float targetY;
        if (upFits && downFits)
        {
            targetY = localPoint.y <= clampRect.center.y ? upY : downY;
        }
        else if (upFits)
        {
            targetY = upY;
        }
        else if (downFits)
        {
            targetY = downY;
        }
        else
        {
            float upDelta = Mathf.Abs(Mathf.Clamp(upY, yMin, yMax) - upY);
            float downDelta = Mathf.Abs(Mathf.Clamp(downY, yMin, yMax) - downY);
            targetY = upDelta <= downDelta ? upY : downY;
        }

        var p = new Vector2(targetX, targetY);
        p.x = Mathf.Clamp(p.x, xMin, xMax);
        p.y = Mathf.Clamp(p.y, yMin, yMax);
        panel.anchoredPosition = p;
    }

    private Rect GetClampRectLocal(Canvas canvas, RectTransform canvasRect)
    {
        if (!clampToSafeArea)
            return canvasRect.rect;

        var safe = Screen.safeArea;
        var min = safe.min;
        var max = safe.max;

        var cameraForCanvas = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, min, cameraForCanvas, out var localMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, max, cameraForCanvas, out var localMax);

        return Rect.MinMaxRect(
            Mathf.Min(localMin.x, localMax.x),
            Mathf.Min(localMin.y, localMax.y),
            Mathf.Max(localMin.x, localMax.x),
            Mathf.Max(localMin.y, localMax.y));
    }
}

public struct BattleButtonTooltipData
{
    public string TitleKey;
    public string DescriptionKey;

    public string DamageFormatKey;
    public int Damage;

    public string HealFormatKey;
    public int Heal;

    public string MpCostFormatKey;
    public int MpCost;

    public string SpCostFormatKey;
    public int SpCost;

    public string LpCostFormatKey;
    public int LpCost;
}
