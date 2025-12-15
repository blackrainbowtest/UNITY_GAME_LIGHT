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

    public void UpdateText()
    {
        string lang = "en";
        if (UDA2.Core.SettingsContext.Current != null)
            lang = UDA2.Core.SettingsContext.Current.language;
        if (uiStringsData != null && targetText != null)
        {
            targetText.text = uiStringsData.Get(key, lang);
        }
    }
}
