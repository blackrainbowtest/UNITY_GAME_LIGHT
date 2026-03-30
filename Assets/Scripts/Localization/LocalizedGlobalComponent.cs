using System;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Unified localization component.
/// - Supports key-based localization.
/// - Supports optional string.Format args ({0}, {1}, ...).
/// - Updates on language change and font change.
/// - Designed to replace using LocalizedTextComponent + LocalizedTextSetter together.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedGlobalComponent : MonoBehaviour
{
    [Header("Key")]
    [SerializeField] private string key;

    [Header("Formatting")]
    [Tooltip("Optional formatting arguments for templates with {0}, {1}, ...")] 
    [SerializeField] private string[] stringArgs;

    [Header("Font (optional)")]
    [SerializeField] private bool manageFont = true;
    [SerializeField] private FontType fontType = FontType.Body;

    [Header("Startup")]
    [SerializeField, Min(0)] private int deferFirstUpdateFrames = 0;

    private TMP_Text tmpText;
    private bool loggedMissingProvider;
    private object[] cachedArgs;
    private bool initialTextApplied;
    private Coroutine deferredUpdateRoutine;

    public string Key
    {
        get => key;
        set
        {
            key = value;
            UpdateText();
        }
    }

    public void SetArgs(params object[] args)
    {
        cachedArgs = args;
        UpdateText();
    }

    public void ClearArgs()
    {
        cachedArgs = null;
        UpdateText();
    }

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();

        // Prevent double-driving if old localization components are present.
        // IMPORTANT: don't reference those types directly to avoid asmdef coupling.
        DisableLegacyLocalizationDrivers();

        if (manageFont)
            FontManager.OnFontChanged += HandleFontChanged;
    }

    private void DisableLegacyLocalizationDrivers()
    {
        // We intentionally disable by type name to avoid compile-time dependency.
        // This component is meant to be usable in any assembly.
        var behaviours = GetComponents<MonoBehaviour>();
        if (behaviours == null)
            return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            var b = behaviours[i];
            if (b == null || ReferenceEquals(b, this))
                continue;

            var typeName = b.GetType().Name;
            if (string.Equals(typeName, "LocalizedTextSetter", StringComparison.Ordinal)
                || string.Equals(typeName, "LocalizedTextComponent", StringComparison.Ordinal))
            {
                if (b.enabled)
                    b.enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        UDA2.Core.SettingsContext.OnLanguageChanged += HandleLanguageChanged;
        if (manageFont)
            HandleFontChanged();

        if (!initialTextApplied && deferFirstUpdateFrames > 0)
        {
            if (deferredUpdateRoutine != null)
                StopCoroutine(deferredUpdateRoutine);

            deferredUpdateRoutine = StartCoroutine(DeferredInitialUpdateRoutine());
            return;
        }

        UpdateText();
    }

    private void OnDisable()
    {
        UDA2.Core.SettingsContext.OnLanguageChanged -= HandleLanguageChanged;

        if (deferredUpdateRoutine != null)
        {
            StopCoroutine(deferredUpdateRoutine);
            deferredUpdateRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (manageFont)
            FontManager.OnFontChanged -= HandleFontChanged;
    }

    private void HandleLanguageChanged(string lang)
    {
        if (deferredUpdateRoutine != null)
        {
            StopCoroutine(deferredUpdateRoutine);
            deferredUpdateRoutine = null;
        }

        UpdateText(lang);
    }

    private System.Collections.IEnumerator DeferredInitialUpdateRoutine()
    {
        int frames = deferFirstUpdateFrames;
        while (frames > 0)
        {
            frames--;
            yield return null;
        }

        deferredUpdateRoutine = null;
        UpdateText();
    }

    private void HandleFontChanged()
    {
        if (!manageFont)
            return;

        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();

        var font = FontManager.GetFont(fontType);
        if (font != null && tmpText != null)
            tmpText.font = font;
    }

    public void UpdateText()
    {
        var settings = UDA2.Core.SettingsContext.Current;
        if (settings == null)
        {
            settings = UDA2.Core.SettingsManager.Load();
            if (settings == null)
                settings = new UDA2.Core.SettingsState();
            UDA2.Core.SettingsContext.Current = settings;
        }

        var lang = string.IsNullOrEmpty(settings.language) ? "en" : settings.language;
        UpdateText(lang);
    }

    public void UpdateText(string lang)
    {
        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();

        if (tmpText == null)
            return;

        if (string.IsNullOrWhiteSpace(key))
            return;

        var provider = UIStringsProvider.Instance;
        if (provider == null)
        {
            if (!loggedMissingProvider)
            {
                loggedMissingProvider = true;
                Debug.LogWarning($"[LocalizedGlobalComponent] UIStringsProvider.Instance is null when updating key '{key}' on '{gameObject.name}'.", this);
            }
            return;
        }

        loggedMissingProvider = false;

        var template = provider.Get(key.Trim(), lang);
        var args = ResolveArgs();

        if (args != null && args.Length > 0)
        {
            try
            {
                template = string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException)
            {
                // Keep template as-is.
            }
        }

        tmpText.text = template;
        initialTextApplied = true;
    }

    private object[] ResolveArgs()
    {
        if (cachedArgs != null)
            return cachedArgs;

        if (stringArgs == null || stringArgs.Length == 0)
            return null;

        var arr = new object[stringArgs.Length];
        for (int i = 0; i < stringArgs.Length; i++)
            arr[i] = stringArgs[i] ?? string.Empty;

        return arr;
    }
}
