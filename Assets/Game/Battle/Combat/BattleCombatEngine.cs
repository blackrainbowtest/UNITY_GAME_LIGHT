namespace Game.Battle.Combat
{
    public enum CombatResult
    {
        None,
        PlayerWon,
        PlayerLost
    }

    /// <summary>
    /// Pure combat logic. No UI, no state storage.
    /// </summary>
    public sealed class BattleCombatEngine
    {
        private readonly CombatConfig _config;

        public BattleCombatEngine(CombatConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Executes one turn: player attacks, then enemy attacks (if alive).
        /// Returns new state and combat result.
        /// </summary>
        public (CombatState state, CombatResult result) PlayerAttack(CombatState state)
        {
            var afterPlayerHit =
                state.WithEnemyHp(state.EnemyHp - _config.PlayerBaseDamage);

            if (afterPlayerHit.IsEnemyDead)
                return (afterPlayerHit, CombatResult.PlayerWon);

            var afterEnemyHit =
                afterPlayerHit.WithPlayerHp(afterPlayerHit.PlayerHp - _config.EnemyBaseDamage);

            if (afterEnemyHit.IsPlayerDead)
                return (afterEnemyHit, CombatResult.PlayerLost);

            return (afterEnemyHit, CombatResult.None);
        }

        // Важно:
        // - Никакого MP/SP пока
        // - Никакого UI
        // - Никаких side-effects
        // - Один метод = один ход
    }
}
