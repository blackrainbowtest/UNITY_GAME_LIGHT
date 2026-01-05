using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedTextComponent : MonoBehaviour
{
    public string textKey;
    public FontType fontType = FontType.Body;

    private TMP_Text tmpText;

    public UIStringsData uiStringsData;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        FontManager.OnFontChanged += UpdateFont;
    }

    private void OnDestroy()
    {
        FontManager.OnFontChanged -= UpdateFont;
    }

    private void OnEnable()
    {
        UpdateFont();
        UpdateText();
    }

    private void UpdateFont()
    {
        var font = FontManager.GetFont(fontType);
        if (font != null) tmpText.font = font;
    }

    public void UpdateText()
    {
        if (string.IsNullOrEmpty(textKey) || uiStringsData == null)
            return;
        var settings = UDA2.Core.SettingsContext.Current;
        string lang = (settings == null || string.IsNullOrEmpty(settings.language)) ? "en" : settings.language;
        tmpText.text = uiStringsData.Get(textKey, lang);
    }
}
