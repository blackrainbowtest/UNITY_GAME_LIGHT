namespace Game.Battle.Combat
{
    /// <summary>
    /// Immutable combat state that changes during battle.
    /// Contains only mutable combat resources.
    /// </summary>
    public sealed class CombatState
    {
        public int PlayerHp { get; }
        public int PlayerMp { get; }
        public int PlayerSp { get; }
        public int PlayerLp { get; }

        public int EnemyHp { get; }
        public int EnemyMp { get; }
        public int EnemySp { get; }
        public int EnemyLp { get; }

        public CombatState(
            int playerHp, int playerMp, int playerSp, int playerLp,
            int enemyHp, int enemyMp, int enemySp, int enemyLp)
        {
            PlayerHp = playerHp;
            PlayerMp = playerMp;
            PlayerSp = playerSp;
            PlayerLp = playerLp;

            EnemyHp = enemyHp;
            EnemyMp = enemyMp;
            EnemySp = enemySp;
            EnemyLp = enemyLp;
        }

        public CombatState WithPlayerHp(int value)
            => new CombatState(value, PlayerMp, PlayerSp, PlayerLp, EnemyHp, EnemyMp, EnemySp, EnemyLp);

        public CombatState WithEnemyHp(int value)
            => new CombatState(PlayerHp, PlayerMp, PlayerSp, PlayerLp, value, EnemyMp, EnemySp, EnemyLp);

        public bool IsPlayerDead => PlayerHp <= 0;
        public bool IsEnemyDead => EnemyHp <= 0;

        // TODO: Можно добавить:
        // - Очередь ходов или флаг чей сейчас ход (IsPlayerTurn)
        // - Счетчик раунда (Round)
        // - Активные эффекты (PlayerEffects, EnemyEffects)
        // - Флаг завершения боя (IsBattleOver)
        // - Методы With* для других параметров
        // - Параметры для поддержки нескольких врагов
        // - События последнего действия (например, кто атаковал, какой урон нанесён)
    }
}
