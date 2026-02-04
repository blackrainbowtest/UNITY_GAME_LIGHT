using UnityEngine;
using TMPro;

public class LocalizedTextSetter : MonoBehaviour
{
    public string key;
    // Removed UIStringsData reference; now uses UIStringsProvider
    public TMP_Text targetText;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    void Start()
    {
        UpdateText();
    }

    void OnEnable()
    {
        UDA2.Core.SettingsContext.OnLanguageChanged += UpdateText;
    }

    void OnDisable()
    {
        UDA2.Core.SettingsContext.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText(string lang)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        if (targetText != null && UIStringsProvider.Instance != null)
        {
            targetText.text = UIStringsProvider.Instance.Get(key, lang);
        }
    }

    /// <summary>
    /// Updates the text using the current language, ensuring settings are loaded if needed.
    /// </summary>
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

    /// <summary>
    /// Updates all LocalizedTextSetter components in the given root GameObject (recursively).
    /// </summary>
    public static void UpdateAllInHierarchy(GameObject root)
    {
        if (root == null) return;
        foreach (var setter in root.GetComponentsInChildren<LocalizedTextSetter>(true))
        {
            setter.UpdateText();
        }
    }
}
