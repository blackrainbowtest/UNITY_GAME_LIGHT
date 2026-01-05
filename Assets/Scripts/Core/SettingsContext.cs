using System;
namespace UDA2.Core
{
    public static partial class SettingsContext
    {
        public static SettingsState Current;

        public static event Action<string> OnLanguageChanged;
        public static event Action<float> OnMusicVolumeChanged;
        public static event Action<float> OnSfxVolumeChanged;
        public static event Action<float> OnUiVolumeChanged;

        public static void SetLanguage(string lang)
        {
            if (Current != null && Current.language != lang)
            {
                Current.language = lang;
                OnLanguageChanged?.Invoke(lang);
            }
        }

        public static void ApplyAll()
        {
            if (Current == null)
                return;
            OnMusicVolumeChanged?.Invoke(Current.musicVolume);
            OnSfxVolumeChanged?.Invoke(Current.sfxVolume);
            OnUiVolumeChanged?.Invoke(Current.uiVolume);
        }

        public static void SetMusicVolume(float v)
        {
            if (Current == null) return;
            Current.musicVolume = v;
            OnMusicVolumeChanged?.Invoke(v);
        }

        public static void SetSfxVolume(float v)
        {
            if (Current == null) return;
            Current.sfxVolume = v;
            OnSfxVolumeChanged?.Invoke(v);
        }

        public static void SetUiVolume(float v)
        {
            if (Current == null) return;
            Current.uiVolume = v;
            OnUiVolumeChanged?.Invoke(v);
        }
    }
}
