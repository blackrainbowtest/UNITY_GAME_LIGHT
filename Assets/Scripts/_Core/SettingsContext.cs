//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\_Core\SettingsContext.cs                                                          */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:35:55 by UDA                                                                    */
/*   Updated: 2026/01/23 01:35:55 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using System;
using UnityEngine;
namespace UDA2.Core
{
    public static partial class SettingsContext
    {
        public static SettingsState Current;

        public static event Action<string> OnLanguageChanged;
        public static event Action<float> OnMusicVolumeChanged;
        public static event Action<float> OnSfxVolumeChanged;
        public static event Action<float> OnAmbientVolumeChanged;
        public static event Action<float> OnUiVolumeChanged;

        public static event Action<bool> OnCityInspectModeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            OnLanguageChanged = null;
            OnMusicVolumeChanged = null;
            OnSfxVolumeChanged = null;
            OnAmbientVolumeChanged = null;
            OnUiVolumeChanged = null;

            OnCityInspectModeChanged = null;
        }

        public static bool GetCityInspectModeEnabled()
        {
            return Current != null && Current.cityInspectModeEnabled;
        }

        public static void SetCityInspectModeEnabled(bool enabled)
        {
            if (Current == null)
                return;

            if (Current.cityInspectModeEnabled == enabled)
                return;

            Current.cityInspectModeEnabled = enabled;
            OnCityInspectModeChanged?.Invoke(enabled);
        }

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
            OnAmbientVolumeChanged?.Invoke(Current.ambientVolume);
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

        public static void SetAmbientVolume(float v)
        {
            if (Current == null) return;
            Current.ambientVolume = v;
            OnAmbientVolumeChanged?.Invoke(v);
        }
    }
}
