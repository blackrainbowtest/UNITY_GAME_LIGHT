//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleExitContext.cs                                                 */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:41:09 by UDA                                                                    */
/*   Updated: 2026/01/23 01:41:09 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine.SceneManagement;

namespace Game.Battle
{
    /// <summary>
    /// Runtime context for passing "where to return after battle" between scenes.
    /// One-time use by default (Consume).
    /// </summary>
    public static class BattleExitContext
    {
        private static BattleExitData data;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            data = null;
        }

        public static void SetReturnToScene(string sceneName)
        {
            Set(new BattleExitData(sceneName));
        }

        public static void SetReturnToActiveScene()
        {
            SetReturnToScene(SceneManager.GetActiveScene().name);
        }

        public static void Set(BattleExitData exitData)
        {
            data = exitData;
        }

        public static BattleExitData Peek() => data;

        public static BattleExitData Consume()
        {
            var result = data;
            data = null;
            return result;
        }

        public static void Clear()
        {
            data = null;
        }
    }
}
