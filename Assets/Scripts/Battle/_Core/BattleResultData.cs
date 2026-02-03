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
        public readonly struct ItemReward
        {
            public string ItemId { get; }
            public int Count { get; }

            public ItemReward(string itemId, int count)
            {
                ItemId = itemId;
                Count = count;
            }
        }

        public bool PlayerWon { get; }
        public int GoldGained { get; }
        public int ManaCrystalsGained { get; }
        public int DemonCrystalsGained { get; }
        public int ExpGained { get; }
        public IReadOnlyList<ItemReward> Items { get; }

        public BattleResultData(
            bool playerWon,
            int goldGained,
            int manaCrystalsGained,
            int demonCrystalsGained,
            int expGained,
            IReadOnlyList<ItemReward> items)
        {
            PlayerWon = playerWon;
            GoldGained = goldGained;
            ManaCrystalsGained = manaCrystalsGained;
            DemonCrystalsGained = demonCrystalsGained;
            ExpGained = expGained;
            Items = items;
        }
    }
}
