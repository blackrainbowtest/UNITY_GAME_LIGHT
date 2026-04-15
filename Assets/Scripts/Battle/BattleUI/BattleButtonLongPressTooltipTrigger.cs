using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BattleButtonLongPressTooltipTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float holdDuration = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging;
    [SerializeField] private string debugLabel;

    [Header("Optional Progress View")]
    [SerializeField] private float progressShowDelay = 0.15f;

    private bool isPointerDown;
    private bool longPressCompleted;
    private bool suppressClickOnRelease;
    private float elapsed;
    private Vector2 downScreenPosition;

    private bool waitingToShowProgress;
    private float progressTimer;

    private bool didLogProgressShown;
    private bool didLogCompleted;

    private BattleActionTooltipModalController modal;
    private BattleButtonTooltipData tooltipData;

    private LongPressProgressView progressView;

    public void SetDebugLogging(bool enabled, string label = null)
    {
        debugLogging = enabled;
        if (!string.IsNullOrWhiteSpace(label))
            debugLabel = label;
    }

    public void Configure(
        BattleActionTooltipModalController modalController,
        in BattleButtonTooltipData data,
        float duration,
        LongPressProgressView sharedProgressView,
        float showDelaySeconds)
    {
        modal = modalController;
        tooltipData = data;
        holdDuration = Mathf.Max(0.01f, duration);
        progressView = sharedProgressView;
        progressShowDelay = Mathf.Max(0f, showDelaySeconds);

        Log($"Configure: duration={holdDuration:0.###}, showDelay={progressShowDelay:0.###}, modal={(modal != null ? modal.name : "NULL")}, progressView={(progressView != null ? progressView.name : "NULL")}");
    }

    private void Update()
    {
        if (!isPointerDown || longPressCompleted)
            return;

        elapsed += Time.unscaledDeltaTime;

        if (waitingToShowProgress)
        {
            progressTimer += Time.unscaledDeltaTime;
            if (progressTimer >= progressShowDelay)
            {
                waitingToShowProgress = false;
                if (progressView != null)
                {
                    progressView.Show(downScreenPosition);
                    if (!didLogProgressShown)
                    {
                        didLogProgressShown = true;
                        Log($"ProgressView.Show at {downScreenPosition}");
                    }
                }
                else
                {
                    Log("ProgressView is NULL (circle won't show)");
                }
            }
        }

        if (progressView != null)
            progressView.SetProgress(Mathf.Clamp01(elapsed / holdDuration));

        if (elapsed >= holdDuration)
        {
            longPressCompleted = true;
            suppressClickOnRelease = true;

            if (!didLogCompleted)
            {
                didLogCompleted = true;
                Log($"LongPress completed at {elapsed:0.###}s -> showing tooltip");
            }

            if (progressView != null)
                progressView.Hide();

            if (modal != null)
            {
                Log($"Calling modal.Show (wasVisible={modal.IsVisible})");
                modal.Show(tooltipData, downScreenPosition);
                Log($"modal.Show returned (isVisible={modal.IsVisible})");
            }
            else
                Log("Modal is NULL (tooltip won't show)");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        longPressCompleted = false;
        suppressClickOnRelease = false;
        elapsed = 0f;
        progressTimer = 0f;
        waitingToShowProgress = true;
        didLogProgressShown = false;
        didLogCompleted = false;
        downScreenPosition = eventData != null ? eventData.position : (Vector2)Input.mousePosition;

        // DEBUG: capture as much EventSystem state as possible to diagnose first-attempt cancels.
        Log(
            $"PointerDown pos={downScreenPosition}, pointerId={(eventData != null ? eventData.pointerId.ToString() : "null")}, " +
            $"pressRaycast={(eventData != null ? eventData.pointerPressRaycast.gameObject?.name : "null")}, " +
            $"pointerEnter={(eventData != null ? eventData.pointerEnter?.name : "null")}, " +
            $"currentRaycast={(eventData != null ? eventData.pointerCurrentRaycast.gameObject?.name : "null")}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Log(
            $"PointerUp after {elapsed:0.###}s, longPressCompleted={longPressCompleted}, " +
            $"pointerEnter={(eventData != null ? eventData.pointerEnter?.name : "null")}, " +
            $"currentRaycast={(eventData != null ? eventData.pointerCurrentRaycast.gameObject?.name : "null")}");

        if (suppressClickOnRelease && eventData != null)
        {
            eventData.eligibleForClick = false;
            eventData.pointerPress = null;
            eventData.rawPointerPress = null;
            eventData.dragging = false;
            eventData.Use();

            Log("Suppressed click on PointerUp");
        }

        ResetPress();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (suppressClickOnRelease)
        {
            Log(
                $"PointerExit after {elapsed:0.###}s (completed) -> ignore, " +
                $"exitedTo={(eventData != null ? eventData.pointerEnter?.name : "null")}, " +
                $"currentRaycast={(eventData != null ? eventData.pointerCurrentRaycast.gameObject?.name : "null")}");
            return;
        }

        // DEBUG: if first attempt cancels, this should tell us what raycast target stole the pointer.
        Log(
            $"PointerExit after {elapsed:0.###}s -> cancel, " +
            $"exitedTo={(eventData != null ? eventData.pointerEnter?.name : "null")}, " +
            $"currentRaycast={(eventData != null ? eventData.pointerCurrentRaycast.gameObject?.name : "null")}");
        ResetPress();
    }

    private void ResetPress()
    {
        isPointerDown = false;
        elapsed = 0f;
        longPressCompleted = false;
        suppressClickOnRelease = false;
        waitingToShowProgress = false;
        progressTimer = 0f;

        if (progressView != null)
            progressView.Hide();
    }

    private void Log(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!debugLogging)
            return;

        string label = !string.IsNullOrWhiteSpace(debugLabel) ? debugLabel : gameObject.name;
        UDA2.Logging.Logger.LogInfo($"[BattleLongPress] {label}: {message}", UDA2.Logging.LogChannel.UI, this);
#endif
    }
}
