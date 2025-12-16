using System;

namespace UDA2.Core
{
    public static class LocalizationManager
    {
        public static string CurrentLanguage { get; private set; } = "en";

        static LocalizationManager()
        {
            SettingsContext.OnLanguageChanged += SetLanguage;
        }

        public static void SetLanguage(string lang)
        {
            if (CurrentLanguage != lang)
            {
                CurrentLanguage = lang;
                // Здесь можно добавить логику обновления UI, ресурсов и т.д.
            }
        }
    }
}
