using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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
    [FormerlySerializedAs("textKey")]
    [SerializeField] private string legacyTextKey;

    [Header("Formatting")]
    [Tooltip("Optional formatting arguments for templates with {0}, {1}, ...")] 
    [SerializeField] private string[] stringArgs;

    [Header("Font (optional)")]
    [SerializeField] private bool manageFont = true;
    [SerializeField] private FontType fontType = FontType.Body;

    [Header("Target (optional)")]
    [Tooltip("Optional explicit target. If empty, TMP_Text on this GameObject is used.")]
    [SerializeField] private TMP_Text targetText;

    [Header("Startup")]
    [SerializeField, Min(0)] private int deferFirstUpdateFrames = 0;

    private TMP_Text tmpText;
    private bool loggedMissingProvider;
    private object[] cachedArgs;
    private object[] cachedStringArgs;
    private bool cachedStringArgsDirty = true;
    private string normalizedKey;
    private bool normalizedKeyDirty = true;
    private bool initialTextApplied;
    private Coroutine deferredUpdateRoutine;

    public string Key
    {
        get => key;
        set
        {
            if (string.Equals(key, value, StringComparison.Ordinal))
                return;

            key = value;
            normalizedKeyDirty = true;
            UpdateText();
        }
    }

    public void SetArgs(params object[] args)
    {
        cachedArgs = args;
        UpdateText();
    }

    // Legacy API parity with LocalizedTextSetter.
    public void SetFormatArgs(params object[] args)
    {
        SetArgs(args);
    }

    public void ClearArgs()
    {
        if (cachedArgs == null)
            return;

        cachedArgs = null;
        UpdateText();
    }

    private void Awake()
    {
        tmpText = ResolveTargetText();
        normalizedKeyDirty = true;
        cachedStringArgsDirty = true;

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

        if (LocalizationLoadGate.IsDeferring)
        {
            LocalizationLoadGate.Register(this);
            return;
        }

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
        LocalizationLoadGate.Unregister(this);

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
            tmpText = ResolveTargetText();

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
            tmpText = ResolveTargetText();

        if (tmpText == null)
            return;

        var keyToUse = GetNormalizedKey();
        if (string.IsNullOrEmpty(keyToUse))
            return;

        var provider = UIStringsProvider.Instance;
        if (provider == null)
        {
            if (!loggedMissingProvider)
            {
                loggedMissingProvider = true;
                Debug.LogWarning($"[LocalizedGlobalComponent] UIStringsProvider.Instance is null when updating key '{keyToUse}' on '{gameObject.name}'.", this);
            }
            return;
        }

        loggedMissingProvider = false;

        var template = provider.Get(keyToUse, lang);
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

        if (!string.Equals(tmpText.text, template, StringComparison.Ordinal))
            tmpText.text = template;

        initialTextApplied = true;
    }

    private object[] ResolveArgs()
    {
        if (cachedArgs != null)
            return cachedArgs;

        if (stringArgs == null || stringArgs.Length == 0)
            return null;

        if (!cachedStringArgsDirty && cachedStringArgs != null && cachedStringArgs.Length == stringArgs.Length)
            return cachedStringArgs;

        cachedStringArgs = new object[stringArgs.Length];
        for (int i = 0; i < stringArgs.Length; i++)
            cachedStringArgs[i] = stringArgs[i] ?? string.Empty;

        cachedStringArgsDirty = false;
        return cachedStringArgs;
    }

    private string GetNormalizedKey()
    {
        if (!normalizedKeyDirty)
            return normalizedKey;

        var sourceKey = string.IsNullOrWhiteSpace(key) ? legacyTextKey : key;
        normalizedKey = string.IsNullOrWhiteSpace(sourceKey) ? string.Empty : sourceKey.Trim();
        normalizedKeyDirty = false;
        return normalizedKey;
    }

    private TMP_Text ResolveTargetText()
    {
        if (targetText != null)
            return targetText;

        return GetComponent<TMP_Text>();
    }

    public static void UpdateAllInHierarchy(GameObject root)
    {
        if (root == null)
            return;

        var components = root.GetComponentsInChildren<LocalizedGlobalComponent>(true);
        for (int i = 0; i < components.Length; i++)
            components[i].UpdateText();
    }

    private void OnValidate()
    {
        normalizedKeyDirty = true;
        cachedStringArgsDirty = true;

        if (Application.isPlaying && isActiveAndEnabled)
            UpdateText();
    }
}
