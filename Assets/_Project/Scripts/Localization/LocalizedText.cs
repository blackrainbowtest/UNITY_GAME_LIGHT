using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    public string textKey;
    public FontType fontType = FontType.Body;

    private TMP_Text tmpText;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        FontManager.OnFontChanged += UpdateFont;
        UDA2.Core.LocalizationManager.OnLocalizationChanged += UpdateText;
    }

    private void OnDestroy()
    {
        FontManager.OnFontChanged -= UpdateFont;
        UDA2.Core.LocalizationManager.OnLocalizationChanged -= UpdateText;
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
        tmpText.text = UDA2.Core.LocalizationManager.Get(textKey);
    }
}
