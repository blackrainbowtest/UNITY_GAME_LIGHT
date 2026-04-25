using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UDA2.UI.SaveLoad
{
    public class SaveSlotView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
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
        private bool isSaveMode = false;

        /// <summary>
        /// Enables save mode: handlers work for all slots.
        /// </summary>
        public void SetSaveMode(bool value)
        {
            isSaveMode = value;
        }


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

        [Header("Scroll/Drag Guard")]
        [SerializeField] private ScrollRect parentScrollRect;
        [Tooltip("If the finger moved more than this (in pixels) we treat it as scroll/drag and ignore tap.")]
        // <= 0 => use EventSystem.pixelDragThreshold
        [SerializeField] private float dragThresholdPixels = -1f;
        [Tooltip("If ScrollRect normalizedPosition changes more than this while pressed, we cancel tap/long press.")]
        [SerializeField] private float scrollCancelThreshold = 0.0005f;

        private bool canceledByScroll;
        private Vector2 scrollPosOnPointerDown;

        public void ResetLongPressFlag()
        {
            wasLongPressed = false;
            longPressInProgress = false;
        }

        private void Awake()
        {
            if (primaryButton != null)
                primaryButton.onClick.RemoveAllListeners(); // Remove all listeners to prevent Unity Button click

            if (parentScrollRect == null)
                parentScrollRect = GetComponentInParent<ScrollRect>();

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
                if (!canceledByScroll && parentScrollRect != null)
                {
                    var delta = parentScrollRect.normalizedPosition - scrollPosOnPointerDown;
                    if (delta.sqrMagnitude > scrollCancelThreshold * scrollCancelThreshold)
                        CancelByScroll();
                }

                longPressHandler.Update(Time.unscaledDeltaTime);
                if (waitingToShowProgress)
                {
                    progressShowTimer += Time.unscaledDeltaTime;
                    if (progressShowTimer >= progressShowDelay)
                    {
                        waitingToShowProgress = false;
                        if (!canceledByScroll && progressView != null)
                            progressView.Show(lastPointerDownPosition);
                    }
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Clicking is allowed for all slots in save mode, long press - only for non-empty ones
            if (isEmpty && !isSaveMode) return;
            isPointerDown = true;
            canceledByScroll = false;
            progressShowTimer = 0f;
            lastPointerDownPosition = eventData.position;
            pointerDownTime = Time.unscaledTime;

            if (parentScrollRect != null)
                scrollPosOnPointerDown = parentScrollRect.normalizedPosition;

            // long press only for non-empty
            if (!isEmpty)
            {
                longPressInProgress = true;
                waitingToShowProgress = true;
                // progressView.Show will be called with a delay in Update
                longPressHandler.StartPress();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isEmpty && !isSaveMode) return;
            isPointerDown = false;
            waitingToShowProgress = false;

            if (canceledByScroll)
            {
                if (progressView != null)
                    progressView.Hide();
                return;
            }

            float pressDuration = Time.unscaledTime - pointerDownTime;
            if (!isEmpty)
            {
                if (longPressInProgress && !wasLongPressed)
                {
                    longPressInProgress = false;
                }
                longPressHandler.CancelPress();
                if (progressView != null)
                    progressView.Hide();
            }

            // If finger moved, treat as scroll/drag -> ignore tap.
            if (eventData != null)
            {
                float threshold = dragThresholdPixels > 0f
                    ? dragThresholdPixels
                    : (EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f);
                var moved = (eventData.position - lastPointerDownPosition).sqrMagnitude;
                if (moved > threshold * threshold)
                    return;
                if (eventData.dragging)
                    return;
            }

            // Tap: only if not long press and short press
            if (!wasLongPressed && pressDuration < progressShowDelay)
            {
                PrimaryClicked?.Invoke(SlotId);
            }
            // If there was a long press, do nothing.
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isEmpty && !isSaveMode) return;
            isPointerDown = false;
            waitingToShowProgress = false;
            canceledByScroll = true;
            if (!isEmpty)
            {
                if (longPressInProgress && !wasLongPressed)
                {
                    longPressInProgress = false;
                }
                longPressHandler.CancelPress();
                if (progressView != null)
                    progressView.Hide();
            }
        }

        private void HandleLongPressStarted()
        {
            // No-op: already handled in OnPointerDown
        }

        private void HandleLongPressProgress(float progress)
        {
            if (progressView != null)
                progressView.SetProgress(progress);
        }

        private void HandleLongPressCompleted()
        {
            // long press только для непустых слотов
            if (isEmpty) return;
            if (canceledByScroll) return;
            if (progressView != null)
                progressView.Hide();
            wasLongPressed = true;
            longPressInProgress = false;
            LongPressed?.Invoke(SlotId);
        }

        private void HandleLongPressCanceled()
        {
            if (progressView != null)
                progressView.Hide();
        }

        private void CancelByScroll()
        {
            canceledByScroll = true;
            waitingToShowProgress = false;
            longPressInProgress = false;
            longPressHandler.CancelPress();
            if (progressView != null)
                progressView.Hide();
        }

        private void OnDestroy()
        {
            if (primaryButton != null)
                primaryButton.onClick.RemoveListener(OnPrimaryButtonClicked);
        }

        private void OnPrimaryButtonClicked()
        {
            // If a long press was started but not completed, neither the click nor the long press will work.
            if (longPressInProgress && !wasLongPressed)
            {
                return;
            }
            if (wasLongPressed)
            {
                wasLongPressed = false;
                return;
            }
            PrimaryClicked?.Invoke(SlotId);
        }

        public event Action<int> LongPressed;

        private static string GetCurrentLang()
        {
            var settings = UDA2.Core.SettingsContext.Current;
            if (settings == null)
            {
                settings = UDA2.Core.SettingsManager.Load();
                if (settings == null)
                    settings = new UDA2.Core.SettingsState();
                UDA2.Core.SettingsContext.Current = settings;
            }
            return string.IsNullOrEmpty(settings.language) ? "en" : settings.language;
        }

        private static string TryGetUiString(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            var provider = UIStringsProvider.Instance;
            if (provider == null)
                return key;

            return provider.Get(key, GetCurrentLang());
        }

        private void SetSlotTitleKey(string key)
        {
            var localized = slotTitle != null ? slotTitle.GetComponent<LocalizedGlobalComponent>() : null;
            if (localized != null)
            {
                localized.Key = key;
                localized.ClearArgs();
                localized.UpdateText();
            }

            // Hard fallback protects from missing localization wiring.
            if (slotTitle != null)
                slotTitle.text = TryGetUiString(key);
        }

        public void SetEmpty(int slotId)
        {
            SlotId = slotId;
            isEmpty = true;

            // Populate secondary fields first (guards against accidental prefab miswiring
            // where multiple serialized fields point to the same TMP_Text instance).
            if (saveTimeText != null && !ReferenceEquals(saveTimeText, slotTitle))
                saveTimeText.text = TryGetUiString("save_load_empty");

            if (levelGoldText != null && !ReferenceEquals(levelGoldText, slotTitle))
                levelGoldText.text = "—";

            if (primaryButtonText != null && !ReferenceEquals(primaryButtonText, slotTitle))
                primaryButtonText.text = "—";

            // Title should always identify the slot number (set last so it can't be overwritten).
            SetSlotTitleKey($"save_load_slot_{slotId}");
        }

        public void SetData(int slotId, SaveMeta meta)
        {
            SlotId = slotId;
            isEmpty = false;

            if (saveTimeText != null && !ReferenceEquals(saveTimeText, slotTitle))
                saveTimeText.text = meta != null ? NormalizeSaveTime(meta.saveTime) : "—";

            if (levelGoldText != null && !ReferenceEquals(levelGoldText, slotTitle))
                levelGoldText.text = meta != null
                    ? BuildLevelGoldText(meta.playerLevel, meta.playerGold)
                    : "—";

            // Keep the title stable.
            SetSlotTitleKey($"save_load_slot_{slotId}");
        }

        private string BuildLevelGoldText(int level, int gold)
        {
            var provider = UIStringsProvider.Instance;
            if (provider != null)
                return provider.GetFormatted("save_load_level_gold", GetCurrentLang(), level, gold);

            return $"Lv {level} • Gold {gold}";
        }

        private static string NormalizeSaveTime(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "—";

            var s = value.Replace('T', ' ');
            if (s.EndsWith("Z"))
                s = s.Substring(0, s.Length - 1);
            return s;
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

        public void Render(SaveSlotViewModel model)
        {
            SlotId = model.SlotId;
            isEmpty = !model.HasSave;

            SetAutosave(model.IsAutosave);
            SetLocked(model.IsLocked);

            // Populate secondary fields first (guards against accidental prefab miswiring).
            if (isEmpty)
            {
                if (saveTimeText != null && !ReferenceEquals(saveTimeText, slotTitle))
                    saveTimeText.text = TryGetUiString(model.EmptyKey);

                if (levelGoldText != null && !ReferenceEquals(levelGoldText, slotTitle))
                    levelGoldText.text = "—";

                if (primaryButtonText != null && !ReferenceEquals(primaryButtonText, slotTitle))
                    primaryButtonText.text = "—";
            }
            else
            {
                if (saveTimeText != null && !ReferenceEquals(saveTimeText, slotTitle))
                    saveTimeText.text = string.IsNullOrEmpty(model.SaveTimeText) ? "—" : model.SaveTimeText;

                if (levelGoldText != null && !ReferenceEquals(levelGoldText, slotTitle))
                    levelGoldText.text = string.IsNullOrEmpty(model.LevelGoldText) ? "—" : model.LevelGoldText;
            }

            // Keep the title stable.
            SetSlotTitleKey(model.TitleKey);
        }
    }
}
