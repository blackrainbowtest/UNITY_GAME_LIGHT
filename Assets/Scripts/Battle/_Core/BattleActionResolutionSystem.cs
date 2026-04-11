using Game.Battle.Combat;
using Game.Battle.Combat.Actions;
using Game.Battle.Combat.EnemyAI;
using Logger = UDA2.Logging.Logger;

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
        private const int DefaultNonHealActionsBetweenHeals = 2;
        private int nonHealActionsBeforeNextHeal;

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

            var allowHealActions = nonHealActionsBeforeNextHeal <= 0;

            var picked = EnemyActionSelector.SelectEnemyAction(
                context.EnemyDifficulty,
                context.Enemy,
                actionRegistry,
                combatState,
                rng,
                allowHealActions);

            // Fallback: if heal is temporarily blocked but no non-heal action is available,
            // let the enemy act instead of skipping the turn.
            if (!picked.HasValue && !allowHealActions)
            {
                picked = EnemyActionSelector.SelectEnemyAction(
                    context.EnemyDifficulty,
                    context.Enemy,
                    actionRegistry,
                    combatState,
                    rng,
                    allowHealActions: true);
            }

            if (!picked.HasValue)
            {
                failure = BattleActionResolveFailure.EnemyNoActionPicked;
                return false;
            }

            if (context.Enemy != null)
            {
                var allowed = context.Enemy.allowedActions;
                var allowedText = (allowed != null && allowed.Length > 0)
                    ? string.Join(", ", allowed)
                    : "<default fallback>";

                Logger.LogInfo(
                    $"[Battle][AI] enemyId={context.Enemy.id}, hp={combatState.EnemyHp}/{context.Enemy.maxHp}, mp={combatState.EnemyMp}/{context.Enemy.maxMp}, " +
                    $"allowed=[{allowedText}], picked={picked.Value}",
                    UDA2.Logging.LogChannel.AI);

                // Force update of uda2.log for easier bug-report sharing during active play sessions.
                Logger.FlushToFile();
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

            var isHealAction = action.HpHealSelf > 0;
            if (isHealAction)
            {
                var configured = context != null && context.Enemy != null
                    ? context.Enemy.nonHealActionsBetweenHeals
                    : DefaultNonHealActionsBetweenHeals;

                nonHealActionsBeforeNextHeal = configured < 0 ? 0 : configured;
            }
            else if (nonHealActionsBeforeNextHeal > 0)
            {
                nonHealActionsBeforeNextHeal--;
            }

            return true;
        }
    }
}
