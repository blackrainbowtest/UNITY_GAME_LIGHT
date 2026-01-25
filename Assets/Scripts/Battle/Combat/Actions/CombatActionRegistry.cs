//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\Combat\Actions\CombatActionRegistry.cs                                     */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:38:04 by UDA                                                                    */
/*   Updated: 2026/01/23 01:38:04 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using System.Collections.Generic;

namespace Game.Battle.Combat.Actions
{
    /// <summary>
    /// Central registry of all combat actions.
    /// Single source of truth for action data.
    /// </summary>
    public sealed class CombatActionRegistry
    {
        private readonly Dictionary<CombatActionId, CombatActionData> _actions;

        public CombatActionRegistry()
        {
            _actions = new Dictionary<CombatActionId, CombatActionData>
            {
                {
                    CombatActionId.FastAttack,
                    new CombatActionData(
                        id: CombatActionId.FastAttack,
                        category: CombatActionCategory.Attack,
                        hpDamage: 10,
                        mpCost: 0,
                        spCost: 5,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.NormalAttack,
                    new CombatActionData(
                        CombatActionId.NormalAttack,
                        CombatActionCategory.Attack,
                        hpDamage: 18,
                        mpCost: 0,
                        spCost: 10,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.HeavyAttack,
                    new CombatActionData(
                        CombatActionId.HeavyAttack,
                        CombatActionCategory.Attack,
                        hpDamage: 30,
                        mpCost: 0,
                        spCost: 20,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.CounterAttack,
                    new CombatActionData(
                        CombatActionId.CounterAttack,
                        CombatActionCategory.Attack,
                        hpDamage: 45,
                        mpCost: 0,
                        spCost: 8,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: true)
                },
                {
                    CombatActionId.Block,
                    new CombatActionData(
                        CombatActionId.Block,
                        CombatActionCategory.Defense,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 5,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },

                // ===== Magic =====
                {
                    CombatActionId.FireSpell,
                    new CombatActionData(
                        CombatActionId.FireSpell,
                        CombatActionCategory.Magic,
                        hpDamage: 22,
                        mpCost: 10,
                        spCost: 0,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.IceSpell,
                    new CombatActionData(
                        CombatActionId.IceSpell,
                        CombatActionCategory.Magic,
                        hpDamage: 18,
                        mpCost: 8,
                        spCost: 0,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.HolySpell,
                    new CombatActionData(
                        CombatActionId.HolySpell,
                        CombatActionCategory.Magic,
                        hpDamage: 0,
                        mpCost: 6,
                        spCost: 0,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false,
                        hpHealSelf: 14)
                },
                {
                    CombatActionId.DarkSpell,
                    new CombatActionData(
                        CombatActionId.DarkSpell,
                        CombatActionCategory.Magic,
                        hpDamage: 28,
                        mpCost: 14,
                        spCost: 0,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },

                // ===== Seduction (placeholder effects for now) =====
                {
                    CombatActionId.SeductionAct1,
                    new CombatActionData(
                        CombatActionId.SeductionAct1,
                        CombatActionCategory.Seduction,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 0,
                        lpCost: 10,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.SeductionAct2,
                    new CombatActionData(
                        CombatActionId.SeductionAct2,
                        CombatActionCategory.Seduction,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 0,
                        lpCost: 20,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.SeductionAct3,
                    new CombatActionData(
                        CombatActionId.SeductionAct3,
                        CombatActionCategory.Seduction,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 0,
                        lpCost: 30,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.SeductionAct4,
                    new CombatActionData(
                        CombatActionId.SeductionAct4,
                        CombatActionCategory.Seduction,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 0,
                        lpCost: 40,
                        requiresPlayerBlockedLastTurn: false)
                },

                // ===== Actions (placeholder effects for now) =====
                {
                    CombatActionId.ActionAct1,
                    new CombatActionData(
                        CombatActionId.ActionAct1,
                        CombatActionCategory.Utility,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 0,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.ActionAct2,
                    new CombatActionData(
                        CombatActionId.ActionAct2,
                        CombatActionCategory.Utility,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 0,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.ActionAct3,
                    new CombatActionData(
                        CombatActionId.ActionAct3,
                        CombatActionCategory.Utility,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 0,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                },
                {
                    CombatActionId.ActionAct4,
                    new CombatActionData(
                        CombatActionId.ActionAct4,
                        CombatActionCategory.Utility,
                        hpDamage: 0,
                        mpCost: 0,
                        spCost: 0,
                        lpCost: 0,
                        requiresPlayerBlockedLastTurn: false)
                }
            };
        }

        public CombatActionData Get(CombatActionId id)
        {
            return _actions.TryGetValue(id, out var action)
                ? action
                : null;
        }

        public IEnumerable<CombatActionData> GetByCategory(CombatActionCategory category)
        {
            foreach (var action in _actions.Values)
            {
                if (action.Category == category)
                    yield return action;
            }
        }
    }
}
