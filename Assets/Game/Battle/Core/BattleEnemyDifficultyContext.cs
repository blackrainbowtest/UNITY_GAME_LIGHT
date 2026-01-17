namespace Game.Battle
{
    /// <summary>
    /// One-shot context for passing enemy difficulty into the battle scene.
    /// Set this before loading the battle scene.
    /// </summary>
    public static class BattleEnemyDifficultyContext
    {
        private static EnemyDifficulty? _difficulty;

        public static void Set(EnemyDifficulty difficulty)
        {
            _difficulty = difficulty;
        }

        public static EnemyDifficulty ConsumeOrDefault(EnemyDifficulty fallback = EnemyDifficulty.Normal)
        {
            if (_difficulty.HasValue)
            {
                var value = _difficulty.Value;
                _difficulty = null;
                return value;
            }

            return fallback;
        }
    }
}
