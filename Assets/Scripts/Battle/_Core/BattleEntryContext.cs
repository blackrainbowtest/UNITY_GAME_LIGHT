//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleEntryContext.cs                                                */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:40:54 by UDA                                                                    */
/*   Updated: 2026/01/23 01:40:54 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

namespace Game.Battle
{
    /// <summary>
    /// Runtime context for passing battle entry parameters between scenes.
    /// One-time use, resets after consume.
    /// </summary>
    public static class BattleEntryContext
    {
        private static BattleMode mode = BattleMode.Normal;

        public static void Set(BattleMode battleMode)
        {
            mode = battleMode;
        }

        public static BattleMode Consume()
        {
            var result = mode;
            mode = BattleMode.Normal; // reset to default
            return result;
        }
    }
}
