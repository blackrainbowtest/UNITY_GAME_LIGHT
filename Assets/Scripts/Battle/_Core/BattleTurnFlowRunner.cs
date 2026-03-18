using Game.Battle.Combat;
using Game.Battle.Combat.Actions;

namespace Game.Battle
{
    public sealed class BattleTurnFlowRunner
    {
        private readonly BattleEndConditionSystem endConditionSystem;

        public BattleTurnFlowRunner(BattleEndConditionSystem endConditionSystem)
        {
            this.endConditionSystem = endConditionSystem;
        }

        public bool TryResolveAfterPlayerAction(BattleContext context, CombatState combatState, CombatActionId actionId, out BattleEndResolution resolution)
        {
            resolution = BattleEndResolution.None;

            if (endConditionSystem == null)
                return false;

            if (endConditionSystem.TryResolveByLpThreshold(context, combatState, actionByPlayer: true, sourceActionId: actionId, out resolution))
                return true;

            if (endConditionSystem.TryResolveByHp(combatState, checkEnemyDeathAsPlayerVictory: true, sourceActionId: actionId, out resolution))
                return true;

            return false;
        }

        public bool TryResolveAfterEnemyAction(BattleContext context, CombatState combatState, CombatActionId enemyActionId, out BattleEndResolution resolution)
        {
            resolution = BattleEndResolution.None;

            if (endConditionSystem == null)
                return false;

            if (endConditionSystem.TryResolveByLpThreshold(context, combatState, actionByPlayer: false, sourceActionId: enemyActionId, out resolution))
                return true;

            if (endConditionSystem.TryResolveByHp(combatState, checkEnemyDeathAsPlayerVictory: false, sourceActionId: enemyActionId, out resolution))
                return true;

            return false;
        }

        public bool TryResolveAfterEnemyRound(BattleContext context, CombatState combatState, out BattleEndResolution resolution)
        {
            resolution = BattleEndResolution.None;

            if (endConditionSystem == null)
                return false;

            if (endConditionSystem.TryResolveByLpThreshold(context, combatState, actionByPlayer: false, sourceActionId: null, out resolution))
                return true;

            if (endConditionSystem.TryResolveByHp(combatState, checkEnemyDeathAsPlayerVictory: false, sourceActionId: null, out resolution))
                return true;

            return false;
        }
    }
}
