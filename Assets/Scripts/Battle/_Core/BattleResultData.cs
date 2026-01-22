//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleResultData.cs                                                  */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:41:53 by UDA                                                                    */
/*   Updated: 2026/01/23 01:41:53 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// Data-only battle outcome for UI.
    /// </summary>
    public sealed class BattleResultData
    {
        public bool PlayerWon { get; }
        public int GoldGained { get; }
        public IReadOnlyList<string> ItemIds { get; }

        public BattleResultData(bool playerWon, int goldGained, IReadOnlyList<string> itemIds)
        {
            PlayerWon = playerWon;
            GoldGained = goldGained;
            ItemIds = itemIds;
        }
    }
}
