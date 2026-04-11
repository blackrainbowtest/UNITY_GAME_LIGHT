//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\_Core\SettingsManager.cs                                                          */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:36:10 by UDA                                                                    */
/*   Updated: 2026/01/23 01:36:10 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

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
            try
            {
                if (!File.Exists(SettingsPath))
                    return Sanitize(new SettingsState());

                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonConvert.DeserializeObject<SettingsState>(json);
                return Sanitize(loaded ?? new SettingsState());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SettingsManager.Load: failed to read settings, using defaults. {ex.Message}");
                return Sanitize(new SettingsState());
            }
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

        private static SettingsState Sanitize(SettingsState state)
        {
            if (state == null)
                state = new SettingsState();

            state.musicVolume = Mathf.Clamp01(state.musicVolume);
            state.sfxVolume = Mathf.Clamp01(state.sfxVolume);
            state.ambientVolume = state.sfxVolume;
            state.uiVolume = Mathf.Clamp01(state.uiVolume);

            if (string.IsNullOrWhiteSpace(state.language))
            {
                state.language = SupportedLanguages[0];
                return state;
            }

            bool supported = false;
            for (int i = 0; i < SupportedLanguages.Length; i++)
            {
                if (string.Equals(SupportedLanguages[i], state.language, StringComparison.OrdinalIgnoreCase))
                {
                    state.language = SupportedLanguages[i];
                    supported = true;
                    break;
                }
            }

            if (!supported)
                state.language = SupportedLanguages[0];

            return state;
        }
    }
}
