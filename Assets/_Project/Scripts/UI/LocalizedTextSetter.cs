using UnityEngine;
using TMPro;

public class LocalizedTextSetter : MonoBehaviour
{
    public string key;
    public UIStringsData uiStringsData;
    public TMP_Text targetText;

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
        if (uiStringsData != null && targetText != null)
        {
            targetText.text = uiStringsData.Get(key, lang);
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
