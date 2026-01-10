using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TODO: fix comments and remove debugs
namespace UDA2.UI.SaveLoad
{
    public class SaveSlotView : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        [Header("Wiring")]
        [SerializeField] private TMP_Text slotTitle;
        [SerializeField] private TMP_Text saveTimeText;
        [SerializeField] private TMP_Text levelGoldText;

        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryButtonText;

        [Header("Optional visuals")]
        [SerializeField] private GameObject lockBadge; // show for autosave if needed
        [SerializeField] private GameObject lockOverlay; // visual lock overlay

        public int SlotId { get; private set; }
        public bool IsAutoSave { get; private set; }
        private bool isEmpty = false;


        [Header("Long Press")]
        [SerializeField] private float longPressDuration = 1.0f;
        private LongPressProgressView progressView;

        private LongPressHandler longPressHandler;
        private bool isPointerDown;
        public void SetProgressView(LongPressProgressView view)
        {
            progressView = view;
        }

        public event Action<int> PrimaryClicked;


        private bool wasLongPressed = false;
        private bool longPressInProgress = false;
        private bool waitingToShowProgress = false;
        private float progressShowDelay = 0.15f; // 150 ms
        private float progressShowTimer = 0f;
        private Vector2 lastPointerDownPosition;
        private float pointerDownTime = 0f;

        public void ResetLongPressFlag()
        {
            wasLongPressed = false;
            longPressInProgress = false;
        }

        private void Awake()
        {
            if (primaryButton != null)
                primaryButton.onClick.RemoveAllListeners(); // Remove all listeners to prevent Unity Button click

            // progressView is assigned after instantiation via SetProgressView

            longPressHandler = new LongPressHandler(longPressDuration);
            longPressHandler.OnStarted += HandleLongPressStarted;
            longPressHandler.OnProgress += HandleLongPressProgress;
            longPressHandler.OnCompleted += HandleLongPressCompleted;
            longPressHandler.OnCanceled += HandleLongPressCanceled;
        }

        // Update is used only to forward unscaled delta time to LongPressHandler.
        // No timing logic or decisions are made here.
        private void Update()
        {
            if (isPointerDown)
            {
                longPressHandler.Update(Time.unscaledDeltaTime);
                if (waitingToShowProgress)
                {
                    progressShowTimer += Time.unscaledDeltaTime;
                    if (progressShowTimer >= progressShowDelay)
                    {
                        waitingToShowProgress = false;
                        progressView.Show(lastPointerDownPosition);
                        UDA2.Logging.Logger.LogInfo($"[SaveSlotView] progressView.Show (delayed) at {lastPointerDownPosition}", UDA2.Logging.LogChannel.UI);
                    }
                }
            }
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (isEmpty) return;
            isPointerDown = true;
            longPressInProgress = true;
            waitingToShowProgress = true;
            progressShowTimer = 0f;
            lastPointerDownPosition = eventData.position;
            pointerDownTime = Time.unscaledTime;
            UDA2.Logging.Logger.LogInfo($"[SaveSlotView] OnPointerDown slot {SlotId}", UDA2.Logging.LogChannel.UI);
            // progressView.Show will be called with a delay in Update
            longPressHandler.StartPress();
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (isEmpty) return;
            isPointerDown = false;
            waitingToShowProgress = false;
            UDA2.Logging.Logger.LogInfo($"[SaveSlotView] OnPointerUp slot {SlotId}", UDA2.Logging.LogChannel.UI);
            float pressDuration = Time.unscaledTime - pointerDownTime;
            if (longPressInProgress && !wasLongPressed)
            {
                longPressInProgress = false;
            }
            longPressHandler.CancelPress();
            progressView.Hide();
            UDA2.Logging.Logger.LogInfo($"[SaveSlotView] progressView.Hide", UDA2.Logging.LogChannel.UI);
            // Tap: only if not long press and duration < progressShowDelay
            if (!wasLongPressed && pressDuration < progressShowDelay)
            {
                UDA2.Logging.Logger.LogInfo($"SaveSlotView: PrimaryClicked {SlotId} (tap)", UDA2.Logging.LogChannel.UI);
                PrimaryClicked?.Invoke(SlotId);
            }
            // If user held longer than progressShowDelay but did not complete long press, do nothing (no click)
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (isEmpty) return;
            isPointerDown = false;
            waitingToShowProgress = false;
            if (longPressInProgress && !wasLongPressed)
            {
                longPressInProgress = false;
            }
            UDA2.Logging.Logger.LogInfo($"[SaveSlotView] OnPointerExit slot {SlotId}", UDA2.Logging.LogChannel.UI);
            longPressHandler.CancelPress();
            progressView.Hide();
            UDA2.Logging.Logger.LogInfo($"[SaveSlotView] progressView.Hide (exit)", UDA2.Logging.LogChannel.UI);
        }

        private void HandleLongPressStarted()
        {
            // No-op: already handled in OnPointerDown
        }

        private void HandleLongPressProgress(float progress)
        {
            progressView.SetProgress(progress);
        }

        private void HandleLongPressCompleted()
        {
            UDA2.Logging.Logger.LogInfo($"[SaveSlotView] LongPress COMPLETED slot {SlotId}", UDA2.Logging.LogChannel.UI);
            progressView.Hide();
            wasLongPressed = true;
            longPressInProgress = false;
            LongPressed?.Invoke(SlotId);
        }

        private void HandleLongPressCanceled()
        {
            UDA2.Logging.Logger.LogInfo($"[SaveSlotView] LongPress CANCELED slot {SlotId}", UDA2.Logging.LogChannel.UI);
            progressView.Hide();
        }

        private void OnDestroy()
        {
            if (primaryButton != null)
                primaryButton.onClick.RemoveListener(OnPrimaryButtonClicked);
        }

        private void OnPrimaryButtonClicked()
        {
            // Если был начат long press, но не завершён — не срабатывает ни клик, ни long press
            if (longPressInProgress && !wasLongPressed)
            {
                UDA2.Logging.Logger.LogInfo($"SaveSlotView: PrimaryClicked {SlotId} — skipped due to incomplete long press", UDA2.Logging.LogChannel.UI);
                return;
            }
            if (wasLongPressed)
            {
                wasLongPressed = false;
                UDA2.Logging.Logger.LogInfo($"SaveSlotView: PrimaryClicked {SlotId} — skipped due to long press", UDA2.Logging.LogChannel.UI);
                return;
            }
            UDA2.Logging.Logger.LogInfo($"SaveSlotView: PrimaryClicked {SlotId}", UDA2.Logging.LogChannel.UI);
            PrimaryClicked?.Invoke(SlotId);
        }

        public event Action<int> LongPressed;

        public void SetEmpty(int slotId)
        {
            SlotId = slotId;
            isEmpty = true;
            var setter = slotTitle.GetComponent<LocalizedTextSetter>();
            if (setter != null)
            {
                setter.key = "save_load_empty";
                setter.UpdateText();
            }
            var comp = slotTitle.GetComponent<LocalizedTextComponent>();
            if (comp != null)
            {
                comp.textKey = "save_load_empty";
                comp.UpdateText();
            }
            saveTimeText.text = "—";
            levelGoldText.text = "—";
            primaryButtonText.text = "—";
        }

        public void SetData(int slotId, SaveMeta meta)
        {
            SlotId = slotId;
            isEmpty = false;
            var setter = slotTitle.GetComponent<LocalizedTextSetter>();
            if (setter != null)
            {
                setter.key = $"save_load_slot_{slotId}";
                setter.UpdateText();
            }
            else
                primaryButtonText.text = "Load";
            var comp = slotTitle.GetComponent<LocalizedTextComponent>();
            if (comp != null)
            {
                comp.textKey = $"save_load_slot_{slotId}";
                comp.UpdateText();
            }
            else
                primaryButtonText.text = "Load";
            saveTimeText.text = meta.saveTime;
            levelGoldText.text = $"Lv {meta.playerLevel} • Gold {meta.playTimeSeconds}";
        }

        public void SetAutosave(bool isAutosave)
        {
            if (lockBadge != null)
                lockBadge.SetActive(isAutosave);
        }

        public void SetLocked(bool locked)
        {
            if (lockOverlay != null)
                lockOverlay.SetActive(locked);
            if (primaryButton != null)
                primaryButton.interactable = !locked;
        }
    }
}
