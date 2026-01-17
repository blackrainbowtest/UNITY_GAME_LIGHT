using Game.Battle.Combat;

namespace Game.Battle.UI
{
    /// <summary>
    /// Builds HUD state from combat and context data.
    /// </summary>
    public static class BattleHUDStateFactory
    {
        public static BattleHUDState Create(
            PlayerCombatSnapshot player,
            EnemyData enemy,
            CombatState combat)
        {
            return new BattleHUDState
            {
                PlayerHp = combat.PlayerHp,
                PlayerHpMax = player.MaxHP,
                PlayerMp = combat.PlayerMp,
                PlayerMpMax = player.MaxMP,
                PlayerSp = combat.PlayerSp,
                PlayerSpMax = player.MaxSP,
                PlayerLp = combat.PlayerLp,
                PlayerLpMax = player.MaxLP,

                EnemyHp = combat.EnemyHp,
                EnemyHpMax = enemy.maxHp,
                EnemyMp = combat.EnemyMp,
                EnemyMpMax = enemy.maxMp,
                EnemySp = combat.EnemySp,
                EnemySpMax = enemy.maxSp,
                EnemyLp = combat.EnemyLp,
                EnemyLpMax = enemy.maxLp
            };
        }
    }
}
