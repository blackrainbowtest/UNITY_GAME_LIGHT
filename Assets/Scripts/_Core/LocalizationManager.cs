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
using System.Reflection;
using UnityEngine;

namespace UDA2.Core
{
    public static class LocalizationManager
    {
        public static string CurrentLanguage { get; private set; } = "en";

		private static MethodInfo _providerGetKeyLang;
		private static PropertyInfo _providerInstanceProp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            CurrentLanguage = "en";
			_providerGetKeyLang = null;
			_providerInstanceProp = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Ensure we don't stack handlers across play sessions when Domain Reload is disabled.
            SettingsContext.OnLanguageChanged -= SetLanguage;
            SettingsContext.OnLanguageChanged += SetLanguage;
        }

        private static bool TryResolveProvider()
        {
            if (_providerGetKeyLang != null && _providerInstanceProp != null)
                return true;

            // UIStringsProvider lives in the Localization assembly (global namespace).
            var providerType = Type.GetType("UIStringsProvider");
            if (providerType == null)
                return false;

            _providerInstanceProp = providerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            _providerGetKeyLang = providerType.GetMethod(
                "Get",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(string), typeof(string) },
                modifiers: null
            );

            return _providerInstanceProp != null && _providerGetKeyLang != null;
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
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            try
            {
                if (TryResolveProvider())
                {
                    var instance = _providerInstanceProp.GetValue(null);
                    if (instance != null)
                        return (string)_providerGetKeyLang.Invoke(instance, new object[] { key, CurrentLanguage });
                }
            }
            catch
            {
                // ignore and fall back to key
            }

            return key;
        }
    }
}
