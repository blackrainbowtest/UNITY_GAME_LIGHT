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

        [Header("Tooltip")]
        [SerializeField] private TMP_Text tooltipText;
        [Tooltip("If set, keys will be taken from this UIStringsData (all keys).")]
        [SerializeField] private UIStringsData tooltipKeysSource;
        [Tooltip("Optional explicit keys. If non-empty, overrides tooltipKeysSource.")]
        [SerializeField] private string[] tooltipKeys;

        private readonly List<string> _cachedKeys = new();
        private int _lastBackgroundIndex = -1;

        private void Awake()
        {
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.RegisterLoadingScreen(this);
        }

        private void OnDisable()
        {
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.UnregisterLoadingScreen(this);
        }

        public void Show()
        {
            gameObject.SetActive(true);

            ApplyRandomBackground();
            ApplyRandomTooltip();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            // Обновить прогресс-бар (реализуйте по необходимости)
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

        private void ApplyRandomTooltip()
        {
            if (tooltipText == null)
                return;

            string key = PickRandomTooltipKey();
            if (string.IsNullOrWhiteSpace(key))
            {
                tooltipText.gameObject.SetActive(false);
                return;
            }

            tooltipText.gameObject.SetActive(true);

            string lang = ResolveLanguage();

            if (UIStringsProvider.Instance != null)
                tooltipText.text = UIStringsProvider.Instance.Get(key, lang);
            else
                tooltipText.text = key;
        }

        private string PickRandomTooltipKey()
        {
            if (tooltipKeys != null && tooltipKeys.Length > 0)
                return tooltipKeys[Random.Range(0, tooltipKeys.Length)];

            if (tooltipKeysSource == null)
                return null;

            tooltipKeysSource.CopyKeysTo(_cachedKeys);
            if (_cachedKeys.Count == 0)
                return null;

            return _cachedKeys[Random.Range(0, _cachedKeys.Count)];
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
