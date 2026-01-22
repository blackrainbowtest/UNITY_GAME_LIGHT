using Game.Battle;

/// <summary>
/// Контекст для передачи выбранного врага между сценами (Single Source of Truth).
/// </summary>
public static class BattleEnemyContext
{
    private static Game.Battle.EnemyData selectedEnemy;

    public static void Set(Game.Battle.EnemyData enemy)
    {
        selectedEnemy = enemy;
    }

    public static Game.Battle.EnemyData Consume()
    {
        var result = selectedEnemy;
        selectedEnemy = null;
        return result;
    }

    public static Game.Battle.EnemyData Peek() => selectedEnemy;
}
