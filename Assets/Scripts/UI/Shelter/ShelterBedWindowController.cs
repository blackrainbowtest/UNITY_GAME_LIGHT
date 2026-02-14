using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UDA2.GameTime;

namespace UDA2.UI.Shelter
{
    public sealed class ShelterBedWindowController : MonoBehaviour
    {
        private const string ActionRest = "rest";
        private const string ActionSleep = "sleep";
        private const string ActionRelax = "relax";
        private const string ActionRelax2 = "relax2";
        private const int DurationStepMinutes = 15;
        private const int MinDurationMinutes = 15;
        private const int MaxDurationMinutes = 24 * 60;

        [Header("Roots")]
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private GameObject modalDurationRoot;
        [SerializeField] private GameObject modalResultRoot;

        [Header("Main Window")]
        [SerializeField] private Button closeWindowButton;
        [SerializeField] private Button restButton;
        [SerializeField] private Button sleepButton;
        [SerializeField] private Button relaxButton;
        [SerializeField] private Button relax2Button;

        [Header("Duration Modal")]
        [SerializeField] private Slider durationSlider;
        [SerializeField] private TMP_Text durationValueText;
        [SerializeField] private Button durationCancelButton;
        [SerializeField] private Button durationConfirmButton;
        [SerializeField] private int defaultDurationHours = 1;

        [Header("Result Modal")]
        [SerializeField] private Button resultCloseButton;
        [SerializeField] private Button resultPrevButton;
        [SerializeField] private Button resultNextButton;
        [SerializeField] private TMP_Text resultIndexText;

        [Header("Result Animation Catalog")]
        [SerializeField] private ShelterBedResultAnimationCatalogAsset resultAnimationCatalog;

        [Header("Behavior")]
        [SerializeField] private bool destroyOnClose = true;
        [Tooltip("Optional explicit object to close/destroy. If empty, auto-resolves ShelterBedWindow root.")]
        [SerializeField] private GameObject closeTargetRoot;

        [Header("Scene UI Visibility")]
        [Tooltip("Scene UI roots to hide while this window is open, then restore on close.")]
        [SerializeField] private GameObject[] uiRootsToHideWhileOpen;
        [Tooltip("If enabled, auto-hides sibling UI roots under the same parent (usually Canvas) while this window is open.")]
        [SerializeField] private bool autoHideSiblingUiRoots = true;
        [Tooltip("Optional sibling roots to keep visible when autoHideSiblingUiRoots is enabled.")]
        [SerializeField] private GameObject[] autoHideExcludeRoots;

        [Header("Canvas Order")]
        [Tooltip("If enabled, this window forces its own Canvas sorting so it can overlay other UI.")]
        [SerializeField] private bool forceWindowCanvasOnTop = true;
        [Tooltip("Sorting order used when forceWindowCanvasOnTop is enabled.")]
        [SerializeField] private int forcedSortingOrder = 100;

        private readonly List<string> _currentAnimationIds = new List<string>();
        private readonly List<Button> _boundCloseButtons = new List<Button>();

        private string _selectedActionId = ActionRest;
        private int _selectedDurationMinutes = 60;
        private int _currentAnimationIndex;
        private GameObject _resolvedCloseTarget;
        private readonly List<UiRootState> _uiRootStates = new List<UiRootState>();
        private bool _sceneUiHidden;

        private struct UiRootState
        {
            public GameObject Root;
            public bool WasActive;
        }

        private void Awake()
        {
            _resolvedCloseTarget = ResolveCloseTarget();
            AutoBindMissingReferences();
            BindAllCloseButtons();

            if (closeWindowButton != null)
                closeWindowButton.onClick.AddListener(CloseWindow);

            if (restButton != null)
                restButton.onClick.AddListener(OpenDurationForRest);
            if (sleepButton != null)
                sleepButton.onClick.AddListener(OpenDurationForSleep);
            if (relaxButton != null)
                relaxButton.onClick.AddListener(OpenDurationForRelax);
            if (relax2Button != null)
                relax2Button.onClick.AddListener(OpenDurationForRelax2);

            if (durationCancelButton != null)
                durationCancelButton.onClick.AddListener(BackToMainWindow);
            if (durationConfirmButton != null)
                durationConfirmButton.onClick.AddListener(ConfirmDuration);

            if (durationSlider != null)
            {
                durationSlider.wholeNumbers = true;
                durationSlider.minValue = MinDurationMinutes / (float)DurationStepMinutes;
                durationSlider.maxValue = MaxDurationMinutes / (float)DurationStepMinutes;
                durationSlider.onValueChanged.AddListener(OnDurationSliderChanged);
            }

            if (resultCloseButton != null)
                resultCloseButton.onClick.AddListener(CloseWindow);
            if (resultPrevButton != null)
                resultPrevButton.onClick.AddListener(ShowPreviousAnimation);
            if (resultNextButton != null)
                resultNextButton.onClick.AddListener(ShowNextAnimation);
        }

        private void OnEnable()
        {
            _resolvedCloseTarget = ResolveCloseTarget();

            if (forceWindowCanvasOnTop)
                EnsureWindowCanvasOnTop();

            HideSceneUiIfNeeded();

            _selectedDurationMinutes = Mathf.Clamp(defaultDurationHours * 60, MinDurationMinutes, MaxDurationMinutes);
            if (durationSlider != null)
                durationSlider.SetValueWithoutNotify(_selectedDurationMinutes / (float)DurationStepMinutes);

            UpdateDurationText();
            ShowOnly(windowRoot);
        }

        private void OnDestroy()
        {
            UnbindAllCloseButtons();

            if (restButton != null)
                restButton.onClick.RemoveListener(OpenDurationForRest);
            if (sleepButton != null)
                sleepButton.onClick.RemoveListener(OpenDurationForSleep);
            if (relaxButton != null)
                relaxButton.onClick.RemoveListener(OpenDurationForRelax);
            if (relax2Button != null)
                relax2Button.onClick.RemoveListener(OpenDurationForRelax2);

            if (durationCancelButton != null)
                durationCancelButton.onClick.RemoveListener(BackToMainWindow);
            if (durationConfirmButton != null)
                durationConfirmButton.onClick.RemoveListener(ConfirmDuration);

            if (durationSlider != null)
                durationSlider.onValueChanged.RemoveListener(OnDurationSliderChanged);

            if (resultPrevButton != null)
                resultPrevButton.onClick.RemoveListener(ShowPreviousAnimation);
            if (resultNextButton != null)
                resultNextButton.onClick.RemoveListener(ShowNextAnimation);

            RestoreSceneUiIfNeeded();
        }

        private void OnDisable()
        {
            RestoreSceneUiIfNeeded();
        }

        public void OpenDurationForRest()
        {
            OpenDurationForAction(ActionRest);
        }

        public void OpenDurationForSleep()
        {
            OpenDurationForAction(ActionSleep);
        }

        public void OpenDurationForRelax()
        {
            OpenDurationForAction(ActionRelax);
        }

        public void OpenDurationForRelax2()
        {
            OpenDurationForAction(ActionRelax2);
        }

        public void OpenDurationForAction(string actionId)
        {
            _selectedActionId = string.IsNullOrWhiteSpace(actionId) ? ActionRest : actionId.Trim().ToLowerInvariant();
            ShowOnly(modalDurationRoot);
            UpdateDurationText();
        }

        public void BackToMainWindow()
        {
            ShowOnly(windowRoot);
        }

        public void ConfirmDuration()
        {
            var minutesToAdd = GetSelectedDurationMinutes();
            if (minutesToAdd > 0)
                GameTimeAPI.AddMinutes(minutesToAdd);

            BuildResultAnimationList();
            _currentAnimationIndex = 0;
            ShowOnly(modalResultRoot);
            UpdateResultTexts();
        }

        public void ShowPreviousAnimation()
        {
            if (_currentAnimationIds.Count <= 1)
                return;

            _currentAnimationIndex--;
            if (_currentAnimationIndex < 0)
                _currentAnimationIndex = _currentAnimationIds.Count - 1;

            UpdateResultTexts();
        }

        public void ShowNextAnimation()
        {
            if (_currentAnimationIds.Count <= 1)
                return;

            _currentAnimationIndex++;
            if (_currentAnimationIndex >= _currentAnimationIds.Count)
                _currentAnimationIndex = 0;

            UpdateResultTexts();
        }

        public void CloseWindow()
        {
            var target = _resolvedCloseTarget != null ? _resolvedCloseTarget : ResolveCloseTarget();
            if (target == null)
                target = gameObject;

            if (destroyOnClose)
                Destroy(target);
            else
                target.SetActive(false);
        }

        public int GetSelectedDurationMinutes()
        {
            return Mathf.Clamp(_selectedDurationMinutes, MinDurationMinutes, MaxDurationMinutes);
        }

        public string GetSelectedActionId()
        {
            return _selectedActionId;
        }

        private void OnDurationSliderChanged(float value)
        {
            int minSteps = MinDurationMinutes / DurationStepMinutes;
            int maxSteps = MaxDurationMinutes / DurationStepMinutes;
            int steps = Mathf.Clamp(Mathf.RoundToInt(value), minSteps, maxSteps);
            _selectedDurationMinutes = steps * DurationStepMinutes;
            UpdateDurationText();
        }

        private void UpdateDurationText()
        {
            if (durationValueText == null)
                return;

            int minutes = Mathf.Clamp(_selectedDurationMinutes, 0, MaxDurationMinutes);
            int hoursPart = minutes / 60;
            int minutesPart = minutes % 60;
            durationValueText.text = $"{hoursPart:00}:{minutesPart:00}";
        }

        private void BuildResultAnimationList()
        {
            _currentAnimationIds.Clear();

            var save = global::GameState.Instance != null ? global::GameState.Instance.CurrentSave : null;
            var resolved = resultAnimationCatalog != null
                ? resultAnimationCatalog.ResolveAnimationIds(_selectedActionId, save)
                : Array.Empty<string>();

            for (int i = 0; i < resolved.Count; i++)
            {
                var id = resolved[i];
                if (!string.IsNullOrWhiteSpace(id))
                    _currentAnimationIds.Add(id.Trim());
            }

            if (_currentAnimationIds.Count == 0)
                _currentAnimationIds.Add(_selectedActionId);
        }

        private void UpdateResultTexts()
        {
            if (resultIndexText != null)
                resultIndexText.text = _currentAnimationIds.Count > 0
                    ? $"{_currentAnimationIndex + 1}/{_currentAnimationIds.Count}"
                    : "0/0";

            bool canSwitch = _currentAnimationIds.Count > 1;
            if (resultPrevButton != null)
                resultPrevButton.interactable = canSwitch;
            if (resultNextButton != null)
                resultNextButton.interactable = canSwitch;
        }

        private void ShowOnly(GameObject target)
        {
            if (windowRoot != null)
                windowRoot.SetActive(target == windowRoot);
            if (modalDurationRoot != null)
                modalDurationRoot.SetActive(target == modalDurationRoot);
            if (modalResultRoot != null)
                modalResultRoot.SetActive(target == modalResultRoot);
        }

        private void EnsureWindowCanvasOnTop()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Clamp(forcedSortingOrder, -32768, 32767);
        }

        private void HideSceneUiIfNeeded()
        {
            if (_sceneUiHidden)
                return;

            _uiRootStates.Clear();

            if (uiRootsToHideWhileOpen != null)
            {
                for (int i = 0; i < uiRootsToHideWhileOpen.Length; i++)
                    AddUiRootStateIfValid(uiRootsToHideWhileOpen[i]);
            }

            if (autoHideSiblingUiRoots)
            {
                var selfRoot = _resolvedCloseTarget != null ? _resolvedCloseTarget : ResolveCloseTarget();
                var parent = selfRoot != null ? selfRoot.transform.parent : null;
                if (parent != null)
                {
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        var child = parent.GetChild(i);
                        if (child == null)
                            continue;

                        var siblingRoot = child.gameObject;
                        if (siblingRoot == selfRoot)
                            continue;
                        if (IsExcludedFromAutoHide(siblingRoot))
                            continue;

                        AddUiRootStateIfValid(siblingRoot);
                    }
                }
            }

            for (int i = 0; i < _uiRootStates.Count; i++)
            {
                var state = _uiRootStates[i];
                if (state.Root != null)
                    state.Root.SetActive(false);
            }

            _sceneUiHidden = true;
        }

        private void RestoreSceneUiIfNeeded()
        {
            if (!_sceneUiHidden)
                return;

            for (int i = 0; i < _uiRootStates.Count; i++)
            {
                var state = _uiRootStates[i];
                if (state.Root == null)
                    continue;

                state.Root.SetActive(state.WasActive);
            }

            _uiRootStates.Clear();
            _sceneUiHidden = false;
        }

        private bool IsExcludedFromAutoHide(GameObject root)
        {
            if (root == null)
                return true;

            if (autoHideExcludeRoots == null || autoHideExcludeRoots.Length == 0)
                return false;

            for (int i = 0; i < autoHideExcludeRoots.Length; i++)
            {
                if (autoHideExcludeRoots[i] == root)
                    return true;
            }

            return false;
        }

        private void AddUiRootStateIfValid(GameObject root)
        {
            if (root == null)
                return;

            if (root == gameObject || root.transform.IsChildOf(transform))
                return;

            for (int i = 0; i < _uiRootStates.Count; i++)
            {
                if (_uiRootStates[i].Root == root)
                    return;
            }

            _uiRootStates.Add(new UiRootState
            {
                Root = root,
                WasActive = root.activeSelf
            });
        }

        private void AutoBindMissingReferences()
        {
            if (closeWindowButton == null)
                closeWindowButton = FindButtonByName("CloseButton", "Button_Close", "Close", "WindowCloseButton");

            if (closeWindowButton == null && windowRoot != null)
                closeWindowButton = FindFirstCloseLikeButton(windowRoot.transform);

            if (resultCloseButton == null)
                resultCloseButton = FindButtonByName("ResultCloseButton", "Button_ResultClose", "Button_Close_Result", "Button_Close");

            if (resultCloseButton == null && modalResultRoot != null)
                resultCloseButton = FindFirstCloseLikeButton(modalResultRoot.transform);

            if (closeWindowButton == null)
                closeWindowButton = FindFirstCloseLikeButton(transform);

            if (resultCloseButton == null)
                resultCloseButton = FindFirstCloseLikeButton(transform);
        }

        private void BindAllCloseButtons()
        {
            _boundCloseButtons.Clear();

            TryBindCloseButton(closeWindowButton);
            TryBindCloseButton(resultCloseButton);

            var searchRoot = GetSearchRootTransform();
            var allButtons = searchRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < allButtons.Length; i++)
            {
                var button = allButtons[i];
                if (button == null)
                    continue;

                var name = button.name ?? string.Empty;
                if (name.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0)
                    TryBindCloseButton(button);
            }

        }

        private void TryBindCloseButton(Button button)
        {
            if (button == null)
                return;

            for (int i = 0; i < _boundCloseButtons.Count; i++)
            {
                if (_boundCloseButtons[i] == button)
                    return;
            }

            button.onClick.RemoveListener(CloseWindow);
            button.onClick.AddListener(CloseWindow);
            _boundCloseButtons.Add(button);
        }

        private void UnbindAllCloseButtons()
        {
            for (int i = 0; i < _boundCloseButtons.Count; i++)
            {
                var button = _boundCloseButtons[i];
                if (button != null)
                    button.onClick.RemoveListener(CloseWindow);
            }

            _boundCloseButtons.Clear();
        }

        private Button FindButtonByName(params string[] names)
        {
            if (names == null || names.Length == 0)
                return null;

            var searchRoot = GetSearchRootTransform();
            var buttons = searchRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null)
                    continue;

                for (int j = 0; j < names.Length; j++)
                {
                    if (string.Equals(button.name, names[j], StringComparison.OrdinalIgnoreCase))
                        return button;
                }
            }

            return null;
        }

        private Transform GetSearchRootTransform()
        {
            if (_resolvedCloseTarget != null)
                return _resolvedCloseTarget.transform;

            var resolved = ResolveCloseTarget();
            if (resolved != null)
                return resolved.transform;

            return transform.root != null ? transform.root : transform;
        }

        private GameObject ResolveCloseTarget()
        {
            if (closeTargetRoot != null)
                return closeTargetRoot;

            var commonRoot = FindCommonAncestorRoot();
            if (commonRoot != null)
                return commonRoot.gameObject;

            var byName = FindNamedWindowRoot(transform);
            if (byName != null)
                return byName.gameObject;

            if (windowRoot != null)
            {
                byName = FindNamedWindowRoot(windowRoot.transform);
                if (byName != null)
                    return byName.gameObject;
            }

            if (modalDurationRoot != null)
            {
                byName = FindNamedWindowRoot(modalDurationRoot.transform);
                if (byName != null)
                    return byName.gameObject;
            }

            if (modalResultRoot != null)
            {
                byName = FindNamedWindowRoot(modalResultRoot.transform);
                if (byName != null)
                    return byName.gameObject;
            }

            return gameObject;
        }

        private Transform FindCommonAncestorRoot()
        {
            var anchors = new List<Transform>(4) { transform };
            if (windowRoot != null) anchors.Add(windowRoot.transform);
            if (modalDurationRoot != null) anchors.Add(modalDurationRoot.transform);
            if (modalResultRoot != null) anchors.Add(modalResultRoot.transform);

            var candidate = anchors[0];
            while (candidate != null)
            {
                bool isCommon = true;
                for (int i = 1; i < anchors.Count; i++)
                {
                    var t = anchors[i];
                    if (t == null || !t.IsChildOf(candidate))
                    {
                        isCommon = false;
                        break;
                    }
                }

                if (isCommon)
                    return candidate;

                candidate = candidate.parent;
            }

            return null;
        }

        private static Transform FindNamedWindowRoot(Transform from)
        {
            var current = from;
            while (current != null)
            {
                var rawName = current.name ?? string.Empty;
                var name = rawName.Replace("(Clone)", string.Empty).Trim();
                if (string.Equals(name, "ShelterBedWindow", StringComparison.OrdinalIgnoreCase))
                    return current;

                current = current.parent;
            }

            return null;
        }

        private static Button FindFirstCloseLikeButton(Transform scope)
        {
            if (scope == null)
                return null;

            var buttons = scope.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null)
                    continue;

                var name = button.name ?? string.Empty;
                if (name.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0)
                    return button;
            }

            return null;
        }
    }
}
