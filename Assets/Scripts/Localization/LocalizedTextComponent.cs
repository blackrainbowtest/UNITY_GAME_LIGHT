using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedTextComponent : MonoBehaviour
{
    public string textKey;
    public FontType fontType = FontType.Body;

    private TMP_Text tmpText;

    // Temporary diagnostics: helps pinpoint missing wiring/order issues without hard-crashing.
    private bool loggedMissingTmpText;
    private bool loggedMissingProvider;



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
        if (tmpText == null)
        {
            tmpText = GetComponent<TMP_Text>();
            if (tmpText == null)
            {
                if (!loggedMissingTmpText)
                {
                    loggedMissingTmpText = true;
                    Debug.LogError($"[LocalizedTextComponent] TMP_Text is missing on '{gameObject.name}'.", this);
                }
                return;
            }
        }

        var font = FontManager.GetFont(fontType);
        if (font != null) tmpText.font = font;
    }

    public void UpdateText()
    {
        if (string.IsNullOrEmpty(textKey))
            return;

        if (tmpText == null)
        {
            tmpText = GetComponent<TMP_Text>();
            if (tmpText == null)
            {
                if (!loggedMissingTmpText)
                {
                    loggedMissingTmpText = true;
                    Debug.LogError($"[LocalizedTextComponent] UpdateText called but TMP_Text is null on '{gameObject.name}'.", this);
                }
                return;
            }
        }

        if (UIStringsProvider.Instance == null)
        {
            if (!loggedMissingProvider)
            {
                loggedMissingProvider = true;
                Debug.LogWarning($"[LocalizedTextComponent] UIStringsProvider.Instance is null when updating key '{textKey}' on '{gameObject.name}'.", this);
            }
            return;
        }

        var settings = UDA2.Core.SettingsContext.Current;
        string lang = (settings == null || string.IsNullOrEmpty(settings.language)) ? "en" : settings.language;
        tmpText.text = UIStringsProvider.Instance.Get(textKey, lang);
    }
}
