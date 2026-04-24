using System.Collections;
using System.Collections.Generic;
using TMPro;
using UDA2.SceneFlow;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI
{
    public class LoadingScreenController : MonoBehaviour, ILoadingScreen
    {
        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite[] backgroundOptions;
        [SerializeField] private bool logBackgroundWarnings = true;

        [Header("Progress")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private TMP_Text progressPercentText;
        [SerializeField] private bool smoothProgress = true;
        [SerializeField, Min(0f)] private float smoothProgressSpeed = 8f;

        [Header("Tooltip")]
        [SerializeField] private TMP_Text tooltipText;
        [Tooltip("If set, keys will be taken from this UIStringsData (all keys).")]
        [SerializeField] private UIStringsData tooltipKeysSource;
        [Tooltip("Optional explicit keys. If non-empty, overrides tooltipKeysSource.")]
        [SerializeField] private string[] tooltipKeys;
        [SerializeField] private TMP_Text tooltipCounterText;
        [SerializeField] private Button previousTooltipButton;
        [SerializeField] private Button nextTooltipButton;
        [SerializeField] private bool allowManualTooltipNavigation = true;
        [SerializeField] private bool rotateTooltipsWhileVisible = true;
        [SerializeField, Min(0f)] private float tooltipRotateIntervalSeconds = 2.5f;

        private readonly List<string> _cachedKeys = new();
        private readonly List<string> _resolvedTooltipKeys = new();
        private int _lastBackgroundIndex = -1;
        private int _lastTooltipIndex = -1;
        private int _currentTooltipIndex = -1;
        private Coroutine _tooltipRotationCoroutine;
        private float _displayedProgress;
        private float _targetProgress;

        private void Awake()
        {
            BindTooltipButtons();
        }

        private void OnEnable()
        {
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.RegisterLoadingScreen(this);
        }

        private void OnDestroy()
        {
            UnbindTooltipButtons();
        }

        private void Update()
        {
            if (!smoothProgress)
                return;

            if (Mathf.Approximately(_displayedProgress, _targetProgress))
                return;

            _displayedProgress = Mathf.MoveTowards(
                _displayedProgress,
                _targetProgress,
                Mathf.Max(0f, smoothProgressSpeed) * Time.unscaledDeltaTime);

            ApplyProgressVisual(_displayedProgress);
        }

        private void OnDisable()
        {
            StopTooltipRotation();

            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.UnregisterLoadingScreen(this);
        }

        public void Show()
        {
            gameObject.SetActive(true);

            _displayedProgress = 0f;
            _targetProgress = 0f;
            ApplyProgressVisual(0f);

            ApplyRandomBackground();
            RebuildTooltipKeys();
            SelectInitialTooltip();
            ApplyCurrentTooltip();
            StartTooltipRotation();
        }

        public void Hide()
        {
            StopTooltipRotation();
            gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);

            if (!smoothProgress)
            {
                _displayedProgress = _targetProgress;
                ApplyProgressVisual(_displayedProgress);
            }
        }

        public void ShowNextTooltip()
        {
            ChangeTooltip(1, true);
        }

        public void ShowPreviousTooltip()
        {
            ChangeTooltip(-1, true);
        }

        private void ApplyRandomBackground()
        {
            if (backgroundImage == null)
            {
                if (logBackgroundWarnings)
                    Debug.LogWarning("LoadingScreenController: backgroundImage is not assigned.");
                return;
            }

            if (backgroundOptions == null || backgroundOptions.Length == 0)
            {
                if (logBackgroundWarnings)
                    Debug.LogWarning("LoadingScreenController: backgroundOptions is empty (no sprites to pick from).");
                return;
            }

            int index = PickRandomIndexAvoidRepeat(backgroundOptions.Length, _lastBackgroundIndex);
            var sprite = backgroundOptions[index];
            if (sprite == null)
            {
                if (logBackgroundWarnings)
                    Debug.LogWarning($"LoadingScreenController: backgroundOptions[{index}] is null.");
                return;
            }

            _lastBackgroundIndex = index;
            backgroundImage.sprite = sprite;
            backgroundImage.enabled = true;
            backgroundImage.gameObject.SetActive(true);
        }

        private static int PickRandomIndexAvoidRepeat(int length, int lastIndex)
        {
            if (length <= 1)
                return 0;

            int index = Random.Range(0, length);
            if (index == lastIndex)
                index = (index + 1 + Random.Range(0, length - 1)) % length;
            return index;
        }

        private void ApplyCurrentTooltip()
        {
            if (tooltipText == null)
                return;

            string key = GetCurrentTooltipKey();
            if (string.IsNullOrWhiteSpace(key))
            {
                tooltipText.gameObject.SetActive(false);
                UpdateTooltipCounter();
                UpdateTooltipNavigationState();
                return;
            }

            tooltipText.gameObject.SetActive(true);

            string lang = ResolveLanguage();

            if (UIStringsProvider.Instance != null)
                tooltipText.text = UIStringsProvider.Instance.Get(key, lang);
            else
                tooltipText.text = key;

            UpdateTooltipCounter();
            UpdateTooltipNavigationState();
        }

        private void RebuildTooltipKeys()
        {
            _resolvedTooltipKeys.Clear();

            if (tooltipKeys != null && tooltipKeys.Length > 0)
            {
                for (int i = 0; i < tooltipKeys.Length; i++)
                {
                    var key = tooltipKeys[i];
                    if (!string.IsNullOrWhiteSpace(key))
                        _resolvedTooltipKeys.Add(key.Trim());
                }
            }
            else if (tooltipKeysSource != null)
            {
                _cachedKeys.Clear();
                tooltipKeysSource.CopyKeysTo(_cachedKeys);

                for (int i = 0; i < _cachedKeys.Count; i++)
                {
                    var key = _cachedKeys[i];
                    if (!string.IsNullOrWhiteSpace(key))
                        _resolvedTooltipKeys.Add(key.Trim());
                }
            }
        }

        private void SelectInitialTooltip()
        {
            if (_resolvedTooltipKeys.Count == 0)
            {
                _currentTooltipIndex = -1;
                return;
            }

            int index = PickRandomIndexAvoidRepeat(_resolvedTooltipKeys.Count, _lastTooltipIndex);
            _currentTooltipIndex = Mathf.Clamp(index, 0, _resolvedTooltipKeys.Count - 1);
            _lastTooltipIndex = _currentTooltipIndex;
        }

        private string GetCurrentTooltipKey()
        {
            if (_resolvedTooltipKeys.Count == 0)
                return null;

            if (_currentTooltipIndex < 0 || _currentTooltipIndex >= _resolvedTooltipKeys.Count)
                _currentTooltipIndex = 0;

            _lastTooltipIndex = _currentTooltipIndex;
            return _resolvedTooltipKeys[_currentTooltipIndex];
        }

        private void ChangeTooltip(int delta, bool restartRotation)
        {
            if (_resolvedTooltipKeys.Count == 0)
                return;

            int count = _resolvedTooltipKeys.Count;

            if (_currentTooltipIndex < 0 || _currentTooltipIndex >= count)
                _currentTooltipIndex = 0;
            else
                _currentTooltipIndex = (_currentTooltipIndex + delta % count + count) % count;

            ApplyCurrentTooltip();

            if (restartRotation)
                RestartTooltipRotation();
        }

        private void ApplyProgressVisual(float progress)
        {
            if (progressSlider != null)
            {
                if (progressSlider.maxValue <= 0f)
                    progressSlider.maxValue = 1f;

                progressSlider.value = progress;
            }

            if (progressFillImage != null)
                progressFillImage.fillAmount = progress;

            if (progressPercentText != null)
                progressPercentText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f)}%";
        }

        private void UpdateTooltipCounter()
        {
            if (tooltipCounterText == null)
                return;

            if (_resolvedTooltipKeys.Count == 0 || _currentTooltipIndex < 0)
            {
                tooltipCounterText.text = "0 / 0";
                return;
            }

            tooltipCounterText.text = $"{_currentTooltipIndex + 1} / {_resolvedTooltipKeys.Count}";
        }

        private void UpdateTooltipNavigationState()
        {
            bool hasManyTips = _resolvedTooltipKeys.Count > 1;
            bool canManual = allowManualTooltipNavigation && hasManyTips;

            if (previousTooltipButton != null)
            {
                previousTooltipButton.interactable = canManual;
                previousTooltipButton.gameObject.SetActive(allowManualTooltipNavigation);
            }

            if (nextTooltipButton != null)
            {
                nextTooltipButton.interactable = canManual;
                nextTooltipButton.gameObject.SetActive(allowManualTooltipNavigation);
            }
        }

        private void BindTooltipButtons()
        {
            if (previousTooltipButton != null)
                previousTooltipButton.onClick.AddListener(ShowPreviousTooltip);

            if (nextTooltipButton != null)
                nextTooltipButton.onClick.AddListener(ShowNextTooltip);
        }

        private void UnbindTooltipButtons()
        {
            if (previousTooltipButton != null)
                previousTooltipButton.onClick.RemoveListener(ShowPreviousTooltip);

            if (nextTooltipButton != null)
                nextTooltipButton.onClick.RemoveListener(ShowNextTooltip);
        }

        private void StartTooltipRotation()
        {
            if (!rotateTooltipsWhileVisible || tooltipRotateIntervalSeconds <= 0f)
                return;

            if (_tooltipRotationCoroutine != null)
                StopCoroutine(_tooltipRotationCoroutine);

            _tooltipRotationCoroutine = StartCoroutine(TooltipRotationRoutine());
        }

        private void StopTooltipRotation()
        {
            if (_tooltipRotationCoroutine == null)
                return;

            StopCoroutine(_tooltipRotationCoroutine);
            _tooltipRotationCoroutine = null;
        }

        private void RestartTooltipRotation()
        {
            if (!rotateTooltipsWhileVisible)
                return;

            StopTooltipRotation();
            StartTooltipRotation();
        }

        private IEnumerator TooltipRotationRoutine()
        {
            float interval = Mathf.Max(0f, tooltipRotateIntervalSeconds);
            while (interval > 0f)
            {
                float elapsed = 0f;
                while (elapsed < interval)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                ChangeTooltip(1, false);
            }

            _tooltipRotationCoroutine = null;
        }

        private static string ResolveLanguage()
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
    }
}
