using System;

namespace UDA2.Core
{
    public static class LocalizationManager
    {
        public static string CurrentLanguage { get; private set; } = "en";
        public static event Action OnLanguageChanged;

        public static void SetLanguage(string lang)
        {
            if (CurrentLanguage != lang)
            {
                CurrentLanguage = lang;
                OnLanguageChanged?.Invoke();
            }
        }
    }
}
