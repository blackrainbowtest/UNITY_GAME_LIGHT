//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\Combat\Actions\CombatActionData.cs                                         */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:37:39 by UDA                                                                    */
/*   Updated: 2026/01/23 01:37:39 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

namespace Game.Battle.Combat.Actions
{
    /// <summary>
    /// Data-only description of a combat action.
    /// Contains costs, damage and requirements. No logic.
    /// </summary>
    public sealed class CombatActionData
    {
        public CombatActionId Id { get; }
        public CombatActionCategory Category { get; }

        public int HpDamage { get; }
        public int HpHealSelf { get; }
        public int MpCost { get; }
        public int SpCost { get; }
        public int LpCost { get; }

        public bool RequiresPlayerBlockedLastTurn { get; }

        public CombatActionData(
            CombatActionId id,
            CombatActionCategory category,
            int hpDamage,
            int mpCost,
            int spCost,
            int lpCost,
            bool requiresPlayerBlockedLastTurn,
            int hpHealSelf = 0)
        {
            Id = id;
            Category = category;
            HpDamage = hpDamage;
            HpHealSelf = hpHealSelf;
            MpCost = mpCost;
            SpCost = spCost;
            LpCost = lpCost;
            RequiresPlayerBlockedLastTurn = requiresPlayerBlockedLastTurn;
        }
    }
}
