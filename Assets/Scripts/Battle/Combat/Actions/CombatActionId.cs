//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\Combat\Actions\CombatActionId.cs                                           */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:37:51 by UDA                                                                    */
/*   Updated: 2026/01/23 01:37:51 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

namespace Game.Battle.Combat.Actions
{
    /// <summary>
    /// Stable identifiers for combat actions.
    /// UI sends only these IDs. Combat decides what they do.
    /// </summary>
    public enum CombatActionId
    {
        FastAttack = 0,
        NormalAttack = 1,
        HeavyAttack = 2,
        CounterAttack = 3,

        Block = 10,

        FireSpell = 20,
        IceSpell = 21,
        HolySpell = 22,
        DarkSpell = 23,

        SeductionAct1 = 30,
        SeductionAct2 = 31,
        SeductionAct3 = 32,
        SeductionAct4 = 33,

		ActionAct1 = 40,
		ActionAct2 = 41,
        ActionAct3 = 42,
        ActionAct4 = 43
    }
}
