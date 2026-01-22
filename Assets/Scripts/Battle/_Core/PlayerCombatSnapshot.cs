//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\PlayerCombatSnapshot.cs                                              */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:42:27 by UDA                                                                    */
/*   Updated: 2026/01/23 01:42:27 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

namespace Game.Battle
{
    /// <summary>
    /// Immutable snapshot of player combat state at battle start.
    /// Extended later with equipment, buffs, etc.
    /// </summary>
    public class PlayerCombatSnapshot
    {
        public string OutfitId { get; }

        public int MaxHP { get; }
        public int CurrentHP { get; }
        public int MaxMP { get; }
        public int CurrentMP { get; }
        public int MaxSP { get; }
        public int CurrentSP { get; }
        public int MaxLP { get; }
        public int CurrentLP { get; }

        // Passive regeneration per own turn (LP does not regenerate).
        public int RegenHpPerTurn { get; }
        public int RegenMpPerTurn { get; }
        public int RegenSpPerTurn { get; }

        public PlayerCombatSnapshot(
            int maxHp,
            int currentHp,
            int maxMp = 0,
            int currentMp = 0,
            int maxSp = 0,
            int currentSp = 0,
            int maxLp = 0,
            int currentLp = 0,
            int regenHpPerTurn = 5,
            int regenMpPerTurn = 2,
            int regenSpPerTurn = 4,
            string outfitId = "outfit_01")
        {
            OutfitId = string.IsNullOrEmpty(outfitId) ? "outfit_01" : outfitId;

            MaxHP = maxHp;
            CurrentHP = currentHp;
            MaxMP = maxMp;
            CurrentMP = currentMp;
            MaxSP = maxSp;
            CurrentSP = currentSp;
            MaxLP = maxLp;
            CurrentLP = currentLp;

            RegenHpPerTurn = regenHpPerTurn;
            RegenMpPerTurn = regenMpPerTurn;
            RegenSpPerTurn = regenSpPerTurn;
        }
    }
}
