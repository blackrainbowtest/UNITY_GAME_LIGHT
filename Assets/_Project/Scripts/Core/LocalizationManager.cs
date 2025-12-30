
using System;
// using Data.Localization; // Удалено, т.к. класс UIStringsData находится в глобальном пространстве имён

namespace UDA2.Core
{
    public static class LocalizationManager
    {
        public static string CurrentLanguage { get; private set; } = "en";

        private static UIStringsData _uiStringsData;

        static LocalizationManager()
        {
            SettingsContext.OnLanguageChanged += SetLanguage;
            if (_uiStringsData == null)
            {
                // Попробуем загрузить ассет из Resources, если он там лежит
                _uiStringsData = UnityEngine.Resources.Load<UIStringsData>("UIStrings");
            }
        }

        public static void SetLanguage(string lang)
        {
            if (CurrentLanguage != lang)
            {
                CurrentLanguage = lang;
                // Здесь можно добавить логику обновления UI, ресурсов и т.д.
            }
        }

        /// <summary>
        /// Получить перевод по ключу для текущего языка. Если ассет не найден — вернуть ключ.
        /// </summary>
        public static string Get(string key)
        {
            if (_uiStringsData == null)
            {
                // Попробуем загрузить ассет из Resources, если он там лежит
                _uiStringsData = UnityEngine.Resources.Load<UIStringsData>("UIStrings");
            }
            if (_uiStringsData != null)
            {
                return _uiStringsData.Get(key, CurrentLanguage);
            }
            return key;
        }
    }
}
