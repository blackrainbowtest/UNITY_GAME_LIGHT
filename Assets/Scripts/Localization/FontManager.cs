using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public enum FontType { Title, Body, UI, Dialogue }

public class FontManager : MonoBehaviour
{
    public static FontManager Instance { get; private set; }
    public static event Action OnFontChanged;

    [Serializable]
    public class LanguageFontProfile
    {
        public string languageCode; // "en", "ru", "fr" и т.д.
        public FontProfile fontProfile;
    }

    [SerializeField] private List<LanguageFontProfile> fontProfiles;
    [SerializeField] private FontProfile fallbackProfile;
    private FontProfile currentProfile;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        UDA2.Core.SettingsContext.OnLanguageChanged += OnLanguageChanged;
        SetProfileByLanguage(UDA2.Core.SettingsContext.Current?.language ?? "en");
    }

    private void OnDestroy()
    {
        UDA2.Core.SettingsContext.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string lang)
    {
        SetProfileByLanguage(lang);
    }

    private void SetProfileByLanguage(string lang)
    {
        foreach (var entry in fontProfiles)
        {
            if (entry.languageCode == lang)
            {
                if (currentProfile == entry.fontProfile)
                    return;
                currentProfile = entry.fontProfile;
                OnFontChanged?.Invoke();
                return;
            }
        }
        if (currentProfile != fallbackProfile)
        {
            currentProfile = fallbackProfile;
            OnFontChanged?.Invoke();
        }
    }

    public static TMP_FontAsset GetFont(FontType type)
    {
        var profile = Instance?.currentProfile ?? Instance?.fallbackProfile;
        if (profile == null) return null;
        TMP_FontAsset result = null;
        switch (type)
        {
            case FontType.Title: result = profile.titleFont; break;
            case FontType.Body: result = profile.bodyFont; break;
            case FontType.UI: result = profile.uiFont; break;
            case FontType.Dialogue: result = profile.dialogueFont; break;
            default: result = null; break;
        }
        return result;
    }
}
