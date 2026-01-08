/* ************************************************************************** */
/*                                                                            */
/*   File: Assets/Scripts/Localization/FontManager.cs                         */
/*                                                        /\_/\               */
/*                                                       ( •.• )              */
/*   By: unluckydungeonadventure.gmail.com                > ^ <               */
/*                                                                            */
/*   Created: 2026/01/08 11:26:58 by UDA                                      */
/*   Updated: 2026/01/08 11:26:58 by UDA                                      */
/*                                                                            */
/* ************************************************************************** */

using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public enum FontType
{
    Title,
    Body,
    UI,
    Dialogue
}

public class FontManager : MonoBehaviour
{
    public static FontManager Instance { get; private set; }
    public static event Action OnFontChanged;

    [Serializable]
    private class LanguageFontProfile
    {
        [SerializeField] private string languageCode;
        [SerializeField] private FontProfile fontProfile;

        public string LanguageCode => languageCode;
        public FontProfile Profile => fontProfile;

#if UNITY_EDITOR
        public static LanguageFontProfile Create(string code, FontProfile profile)
        {
            return new LanguageFontProfile
            {
                languageCode = code,
                fontProfile = profile
            };
        }
#endif
    }

    [SerializeField] private List<LanguageFontProfile> fontProfiles = new();
    [SerializeField] private FontProfile fallbackProfile;

    private FontProfile currentProfile;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        UDA2.Core.SettingsContext.OnLanguageChanged += OnLanguageChanged;
        SetProfileByLanguage(UDA2.Core.SettingsContext.Current?.language ?? "en");
    }

    private void OnDestroy()
    {
        UDA2.Core.SettingsContext.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string languageCode)
    {
        SetProfileByLanguage(languageCode);
    }

    private void SetProfileByLanguage(string languageCode)
    {
        if (fontProfiles == null || fontProfiles.Count == 0)
        {
            ApplyFallback();
            return;
        }

        foreach (var entry in fontProfiles)
        {
            if (entry.LanguageCode == languageCode && entry.Profile != null)
            {
                if (currentProfile == entry.Profile)
                    return;

                currentProfile = entry.Profile;
                OnFontChanged?.Invoke();
                return;
            }
        }

        ApplyFallback();
    }

    private void ApplyFallback()
    {
        if (currentProfile == fallbackProfile)
            return;

        currentProfile = fallbackProfile;
        OnFontChanged?.Invoke();
    }

    public static TMP_FontAsset GetFont(FontType type)
    {
        var manager = Instance;
        if (manager == null)
            return null;

        var profile = manager.currentProfile ?? manager.fallbackProfile;
        if (profile == null)
            return null;

        return type switch
        {
            FontType.Title => profile.titleFont,
            FontType.Body => profile.bodyFont,
            FontType.UI => profile.uiFont,
            FontType.Dialogue => profile.dialogueFont,
            _ => null
        };
    }

#if UNITY_EDITOR
    // Editor-only API for safely extending localization data
    public bool EditorTryAddLanguageProfile(
        string languageCode,
        FontProfile profile,
        out string error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            error = "Language code is empty.";
            return false;
        }

        if (profile == null)
        {
            error = "FontProfile is null.";
            return false;
        }

        foreach (var entry in fontProfiles)
        {
            if (entry.LanguageCode == languageCode)
            {
                error = $"Language '{languageCode}' already exists.";
                return false;
            }
        }

        fontProfiles.Add(LanguageFontProfile.Create(languageCode, profile));
        return true;
    }
#endif
}
