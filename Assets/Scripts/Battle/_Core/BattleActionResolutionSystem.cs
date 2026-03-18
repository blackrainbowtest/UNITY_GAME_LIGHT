using Game.Battle.Combat;
using Game.Battle.Combat.Actions;
using Game.Battle.Combat.EnemyAI;

namespace Game.Battle
{
    public enum BattleActionResolveFailure
    {
        None = 0,
        BattleSystemsNotInitialized = 1,
        ActionNotFound = 2,
        ActionRejected = 3,
        EnemyCannotAct = 4,
        EnemyNoActionPicked = 5,
    }

    public sealed class BattleActionResolutionSystem
    {
        public bool TryResolvePlayerAction(
            BattleCombatEngine combatEngine,
            CombatActionRegistry actionRegistry,
            CombatState combatState,
            int playerPhysicalDamage,
            int playerMagicDamage,
            CombatActionId actionId,
            out CombatActionData action,
            out CombatResolution resolution,
            out BattleActionResolveFailure failure)
        {
            action = null;
            resolution = null;
            failure = BattleActionResolveFailure.None;

            if (combatEngine == null || actionRegistry == null)
            {
                failure = BattleActionResolveFailure.BattleSystemsNotInitialized;
                return false;
            }

            action = actionRegistry.Get(actionId);
            if (action == null)
            {
                failure = BattleActionResolveFailure.ActionNotFound;
                return false;
            }

            resolution = combatEngine.ResolvePlayerAction(combatState, action, playerPhysicalDamage, playerMagicDamage);
            if (resolution == null || resolution.Result != CombatActionResult.Executed)
            {
                failure = BattleActionResolveFailure.ActionRejected;
                return false;
            }

            return true;
        }

        public bool TryResolveEnemyAction(
            BattleContext context,
            BattleCombatEngine combatEngine,
            CombatActionRegistry actionRegistry,
            CombatState combatState,
            int enemyPhysicalDamage,
            int enemyMagicDamage,
            System.Random rng,
            out CombatActionId actionId,
            out CombatActionData action,
            out CombatResolution resolution,
            out BattleActionResolveFailure failure)
        {
            actionId = default;
            action = null;
            resolution = null;
            failure = BattleActionResolveFailure.None;

            if (combatEngine == null || actionRegistry == null || context == null)
            {
                failure = BattleActionResolveFailure.BattleSystemsNotInitialized;
                return false;
            }

            if (combatState == null || combatState.IsEnemyDead || combatState.IsPlayerDead)
            {
                failure = BattleActionResolveFailure.EnemyCannotAct;
                return false;
            }

            var picked = EnemyActionSelector.SelectEnemyAction(
                context.EnemyDifficulty,
                context.Enemy,
                actionRegistry,
                combatState,
                rng);

            if (!picked.HasValue)
            {
                failure = BattleActionResolveFailure.EnemyNoActionPicked;
                return false;
            }

            actionId = picked.Value;
            action = actionRegistry.Get(actionId);
            if (action == null)
            {
                failure = BattleActionResolveFailure.ActionNotFound;
                return false;
            }

            resolution = combatEngine.ResolveEnemyAction(combatState, action, enemyPhysicalDamage, enemyMagicDamage);
            if (resolution == null || resolution.Result != CombatActionResult.Executed)
            {
                failure = BattleActionResolveFailure.ActionRejected;
                return false;
            }

            return true;
        }
    }
}
