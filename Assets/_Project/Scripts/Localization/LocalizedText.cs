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

    private void UpdateText()
    {
        if (string.IsNullOrEmpty(textKey) || uiStringsData == null)
            return;
        var lang = UDA2.Core.LocalizationManager.CurrentLanguage;
        tmpText.text = uiStringsData.Get(textKey, lang);
    }
}
