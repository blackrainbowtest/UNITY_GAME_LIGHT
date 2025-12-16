using System;

namespace UDA2.Core
{
    public static class SettingsContext
    {
        public static SettingsState Current;
        public static event Action<string> OnLanguageChanged;

        public static void SetLanguage(string lang)
        {
            if (Current != null && Current.language != lang)
            {
                Current.language = lang;
                OnLanguageChanged?.Invoke(lang);
            }
        }
    }
}
