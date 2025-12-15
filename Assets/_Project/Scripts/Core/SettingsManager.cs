using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace UDA2.Core
{
    public static class SettingsManager
    {
        private static string SettingsPath => Path.Combine(Application.persistentDataPath, "settings.json");

        // Список поддерживаемых языков (код языка)
        public static readonly string[] SupportedLanguages = { "ru", "en", "fr" };

        // Для отображения в UI (можно расширить при необходимости)
        public static readonly string[] SupportedLanguageDisplayNames = { "Русский", "English", "Français" };

        public static void Save(UDA2.Core.SettingsState state)
        {
            var json = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }

        public static UDA2.Core.SettingsState Load()
        {
            if (!File.Exists(SettingsPath)) return new SettingsState();
            var json = File.ReadAllText(SettingsPath);
            return JsonConvert.DeserializeObject<SettingsState>(json);
        }
        public static void ResetToDefault()
        {
            SettingsContext.Current = new SettingsState();
        }

        // Получить код языка по индексу
        public static string GetLanguageByIndex(int index)
        {
            if (index >= 0 && index < SupportedLanguages.Length)
                return SupportedLanguages[index];
            return SupportedLanguages[0];
        }

        // Получить индекс текущего языка
        public static int GetLanguageIndex()
        {
            var lang = SettingsContext.Current?.language ?? "en";
            for (int i = 0; i < SupportedLanguages.Length; i++)
                if (SupportedLanguages[i] == lang)
                    return i;
            return 0;
        }

        // Получить массив отображаемых имён языков для UI
        public static string[] GetLanguageDisplayNames()
        {
            return SupportedLanguageDisplayNames;
        }
    }
}
