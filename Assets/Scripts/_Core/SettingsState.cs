//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\_Core\SettingsState.cs                                                            */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:36:22 by UDA                                                                    */
/*   Updated: 2026/01/23 01:36:22 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using System;

namespace UDA2.Core
{
    [Serializable]
    public class SettingsState
    {
        public float musicVolume = 0.5f;
        public float sfxVolume = 0.4f;
        public float ambientVolume = 0.4f;
        public float uiVolume = 0.8f;
        public string language = "en";
        public bool tutorialShown = false;
        public string controlScheme = "touch";
        public bool showSubtitles = true;
        public bool vibrationEnabled = true;

        // Battle
        public bool showBattleResultModal = true;

        // City map
        public bool cityInspectModeEnabled = false;
    }
}
