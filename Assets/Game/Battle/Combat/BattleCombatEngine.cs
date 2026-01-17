
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

                playerBlockedLastTurn: false
            );

            // 4. Apply action effects
            if (action.HpDamage > 0)
            {
                var newEnemyHp = newState.EnemyHp - action.HpDamage;
                if (newEnemyHp < 0)
                    newEnemyHp = 0;
                newState = newState.WithEnemyHp(newEnemyHp);
            }

            // 5. Handle block
            if (action.Id == Actions.CombatActionId.Block)
            {
                newState = newState.WithPlayerBlockedLastTurn(true);
            }

            return new CombatResolution(newState, CombatActionResult.Executed);
        }
    }
}
