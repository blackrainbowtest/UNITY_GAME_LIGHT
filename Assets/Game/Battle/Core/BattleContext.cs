namespace Game.Battle
{
    /// <summary>
    /// Aggregates all input data required to start a battle.
    /// Single Source of Truth for battle initialization.
    /// </summary>
    public class BattleContext
    {
        public PlayerCombatSnapshot Player { get; }
        public EnemyData Enemy { get; }
        public BattleLocationData Location { get; }
        public BattleMode Mode { get; }
        public EnemyDifficulty EnemyDifficulty { get; }

        public BattleContext(
            PlayerCombatSnapshot player,
            EnemyData enemy,
            BattleLocationData location,
            BattleMode mode,
            EnemyDifficulty enemyDifficulty = EnemyDifficulty.Normal)
        {
            Player = player;
            Enemy = enemy;
            Location = location;
            Mode = mode;
            EnemyDifficulty = enemyDifficulty;
        }
    }
}
