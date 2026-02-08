//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\Combat\BattleCombatEngine.cs                                               */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:38:41 by UDA                                                                    */
/*   Updated: 2026/01/23 01:38:41 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

namespace Game.Battle.Combat
{
    public enum CombatResult
    {
        None,
        PlayerWon,
        PlayerLost
    }

    public enum CombatActionResult
    {
        Executed,
        Rejected_NotEnoughResources,
        Rejected_RequirementsNotMet
    }

    public sealed class CombatResolution
    {
        public CombatState State { get; }
        public CombatActionResult Result { get; }

        public CombatResolution(CombatState state, CombatActionResult result)
        {
            State = state;
            Result = result;
        }
    }

    /// <summary>
    /// Pure combat logic. No UI, no state storage.
    /// </summary>
    /// <summary>
    /// Pure combat logic. Resolves player actions.
    /// </summary>
    public sealed class BattleCombatEngine
    {
        private const int BlockArmorAmount = 12;

        private static CombatState ApplyHpDamageToPlayerWithBlockArmor(
            CombatState state,
            int incomingDamage,
            out int absorbedByArmor,
            out int actualHpDamage)
        {
            absorbedByArmor = 0;
            actualHpDamage = 0;

            if (incomingDamage <= 0)
                return state
                    .WithPlayerBlockedLastTurn(false)
                    .WithPlayerBlockArmorAbsorbedLastEnemyAction(0);

            var armor = state.PlayerBlockArmor;

            absorbedByArmor = armor > 0 ? System.Math.Min(armor, incomingDamage) : 0;
            var hpDamage = incomingDamage - absorbedByArmor;

            actualHpDamage = hpDamage > 0 ? System.Math.Min(state.PlayerHp, hpDamage) : 0;

            var newHp = state.PlayerHp - actualHpDamage;
            if (newHp < 0)
                newHp = 0;

            var newArmor = armor - absorbedByArmor;
            if (newArmor < 0)
                newArmor = 0;

            var blocked = absorbedByArmor > 0;

            return state
                .WithPlayerHp(newHp)
                .WithPlayerBlockArmor(newArmor)
                .WithPlayerBlockedLastTurn(blocked)
                .WithPlayerBlockArmorAbsorbedLastEnemyAction(absorbedByArmor);
        }

        private static CombatState ApplyHpDamageToEnemyWithBlockArmor(
            CombatState state,
            int incomingDamage,
            out int absorbedByArmor,
            out int actualHpDamage)
        {
            absorbedByArmor = 0;
            actualHpDamage = 0;

            if (incomingDamage <= 0)
                return state
                    .WithEnemyBlockedLastTurn(false)
                    .WithEnemyBlockArmorAbsorbedLastPlayerAction(0);

            var armor = state.EnemyBlockArmor;

            absorbedByArmor = armor > 0 ? System.Math.Min(armor, incomingDamage) : 0;
            var hpDamage = incomingDamage - absorbedByArmor;

            actualHpDamage = hpDamage > 0 ? System.Math.Min(state.EnemyHp, hpDamage) : 0;

            var newHp = state.EnemyHp - actualHpDamage;
            if (newHp < 0)
                newHp = 0;

            var newArmor = armor - absorbedByArmor;
            if (newArmor < 0)
                newArmor = 0;

            var blocked = absorbedByArmor > 0;

            return state
                .WithEnemyHp(newHp)
                .WithEnemyBlockArmor(newArmor)
                .WithEnemyBlockedLastTurn(blocked)
                .WithEnemyBlockArmorAbsorbedLastPlayerAction(absorbedByArmor);
        }

        public CombatResolution ResolvePlayerAction(
            CombatState state,
            Actions.CombatActionData action)
        {
            // 1. Check special requirements
            if (action.RequiresPlayerBlockedLastTurn && !state.PlayerBlockedLastTurn)
            {
                return new CombatResolution(state, CombatActionResult.Rejected_RequirementsNotMet);
            }

            // 2. Check resources
            if (state.PlayerMp < action.MpCost ||
                state.PlayerSp < action.SpCost ||
                state.PlayerLp < action.LpCost)
            {
                return new CombatResolution(state, CombatActionResult.Rejected_NotEnoughResources);
            }

            // 3. Apply player costs
            var newState = new CombatState(
                playerHp: state.PlayerHp,
                playerMp: state.PlayerMp - action.MpCost,
                playerSp: state.PlayerSp - action.SpCost,
                playerLp: state.PlayerLp - action.LpCost,

                enemyHp: state.EnemyHp,
                enemyMp: state.EnemyMp,
                enemySp: state.EnemySp,
                enemyLp: state.EnemyLp,

                // Any player action consumes the "blocked last enemy action" window.
                playerBlockedLastTurn: false,
                playerBlockArmor: state.PlayerBlockArmor,
                playerBlockArmorAbsorbedLastEnemyAction: 0,
                // Player action does not consume enemy's "blocked last player action" window.
                enemyBlockedLastTurn: state.EnemyBlockedLastTurn,
                enemyBlockArmor: state.EnemyBlockArmor,
                enemyBlockArmorAbsorbedLastPlayerAction: state.EnemyBlockArmorAbsorbedLastPlayerAction
            );

            // 4. Apply action effects
            if (action.Id == Actions.CombatActionId.HolySpell)
            {
                // Holy = heal self
                if (action.HpHealSelf > 0)
                    newState = newState.WithPlayerHp(newState.PlayerHp + action.HpHealSelf);
            }
            else if (action.Id == Actions.CombatActionId.DarkSpell)
            {
                // Dark = lifesteal (heal for actual damage dealt)
                if (action.HpDamage > 0)
                {
                    var dealt = action.HpDamage;
                    if (dealt > newState.EnemyHp)
                        dealt = newState.EnemyHp;

                    var newEnemyHp = newState.EnemyHp - dealt;
                    if (newEnemyHp < 0)
                        newEnemyHp = 0;

                    newState = newState
                        .WithEnemyHp(newEnemyHp)
                        .WithPlayerHp(newState.PlayerHp + dealt);
                }
            }
            else
            {
                // Default: damage enemy
                if (action.HpDamage > 0)
                {
                    var damage = action.HpDamage;

                    // CounterAttack bonus scales with the armor that was actually consumed by the last enemy hit.
                    if (action.Id == Actions.CombatActionId.CounterAttack)
                        damage += state.PlayerBlockArmorAbsorbedLastEnemyAction;

                    newState = ApplyHpDamageToEnemyWithBlockArmor(newState, damage, out _, out _);
                }
                else
                {
                    // Non-damaging player action clears enemy counter window.
                    newState = newState
                        .WithEnemyBlockedLastTurn(false)
                        .WithEnemyBlockArmorAbsorbedLastPlayerAction(0);
                }
            }

            // 5. Handle block
            if (action.Id == Actions.CombatActionId.Block)
            {
                // Rule: Block costs SP, but restores 3x that amount right after.
                // Example: cost 5 -> restore 15 (net +10), clamped later by controller to MaxSP.
                if (action.SpCost > 0)
                    newState = newState.WithPlayerSp(newState.PlayerSp + (action.SpCost * 3));

                // Apply block armor. CounterAttack should NOT become available until armor actually absorbs damage.
                newState = newState
                    .WithPlayerBlockArmor(BlockArmorAmount)
                    .WithPlayerBlockedLastTurn(false)
                    .WithPlayerBlockArmorAbsorbedLastEnemyAction(0);
            }

            return new CombatResolution(newState, CombatActionResult.Executed);
        }

        public CombatResolution ResolveEnemyAction(
            CombatState state,
            Actions.CombatActionData action)
        {
            // 0. Enemy requirements (counterattack requires actual absorbed damage).
            if (action.Id == Actions.CombatActionId.CounterAttack && state.EnemyBlockedLastTurn == false)
                return new CombatResolution(state, CombatActionResult.Rejected_RequirementsNotMet);

            // 1. Check resources
            if (state.EnemyMp < action.MpCost ||
                state.EnemySp < action.SpCost ||
                state.EnemyLp < action.LpCost)
            {
                return new CombatResolution(state, CombatActionResult.Rejected_NotEnoughResources);
            }

            // 2. Apply enemy costs
            var newState = new CombatState(
                playerHp: state.PlayerHp,
                playerMp: state.PlayerMp,
                playerSp: state.PlayerSp,
                playerLp: state.PlayerLp,

                enemyHp: state.EnemyHp,
                enemyMp: state.EnemyMp - action.MpCost,
                enemySp: state.EnemySp - action.SpCost,
                enemyLp: state.EnemyLp - action.LpCost,

                // Enemy action updates whether block absorbed any damage.
                playerBlockedLastTurn: false,
                playerBlockArmor: state.PlayerBlockArmor,
                playerBlockArmorAbsorbedLastEnemyAction: 0,
                // Any enemy action consumes its own "blocked last player action" window.
                enemyBlockedLastTurn: false,
                enemyBlockArmor: state.EnemyBlockArmor,
                enemyBlockArmorAbsorbedLastPlayerAction: 0
            );

            // 3. Apply effects
            if (action.Id == Actions.CombatActionId.HolySpell)
            {
                // Holy = heal self (enemy)
                if (action.HpHealSelf > 0)
                    newState = newState.WithEnemyHp(newState.EnemyHp + action.HpHealSelf);
            }
            else if (action.Id == Actions.CombatActionId.DarkSpell)
            {
                // Dark = lifesteal (enemy heals for actual damage dealt)
                if (action.HpDamage > 0)
                {
                    // With block armor, "effective HP" includes armor.
                    var effectiveHp = newState.PlayerHp + newState.PlayerBlockArmor;
                    var incoming = action.HpDamage;
                    if (incoming > effectiveHp)
                        incoming = effectiveHp;

                    newState = ApplyHpDamageToPlayerWithBlockArmor(newState, incoming, out _, out var hpDealt);
                    newState = newState.WithEnemyHp(newState.EnemyHp + hpDealt);
                }
            }
            else
            {
                // Default: enemy damages player
                if (action.HpDamage > 0)
                {
                    var damage = action.HpDamage;

                    if (action.Id == Actions.CombatActionId.CounterAttack)
                        damage += state.EnemyBlockArmorAbsorbedLastPlayerAction;

                    newState = ApplyHpDamageToPlayerWithBlockArmor(newState, damage, out _, out _);
                }
                else
                {
                    // Non-damaging enemy action clears the counter window.
                    newState = newState
                        .WithPlayerBlockedLastTurn(false)
                        .WithPlayerBlockArmorAbsorbedLastEnemyAction(0);
                }
            }

            // Enemy block mirrors player block.
            if (action.Id == Actions.CombatActionId.Block)
            {
                if (action.SpCost > 0)
                    newState = newState.WithEnemySp(newState.EnemySp + (action.SpCost * 3));

                newState = newState
                    .WithEnemyBlockArmor(BlockArmorAmount)
                    .WithEnemyBlockedLastTurn(false)
                    .WithEnemyBlockArmorAbsorbedLastPlayerAction(0);
            }

            return new CombatResolution(newState, CombatActionResult.Executed);
        }
    }
}
