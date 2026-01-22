namespace Game.Battle.Combat
{
    /// <summary>
    /// Configuration for basic combat values.
    /// </summary>
    public sealed class CombatConfig
    {
        public int PlayerBaseDamage { get; }
        public int EnemyBaseDamage { get; }

        public CombatConfig(int playerBaseDamage, int enemyBaseDamage)
        {
            PlayerBaseDamage = playerBaseDamage;
            EnemyBaseDamage = enemyBaseDamage;
        }

        // TODO: Позже добавить:
        // - staminaCost
        // - manaCost
        // - attack types
    }
}
