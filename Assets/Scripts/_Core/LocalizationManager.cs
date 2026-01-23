//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\_Core\LocalizationManager.c                                                       */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:35:23 by UDA                                                                    */
/*   Updated: 2026/01/23 01:35:23 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using System;

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
