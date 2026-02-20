//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\BattleEnemyContext.cs                                                      */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:43:38 by UDA                                                                    */
/*   Updated: 2026/01/23 01:43:38 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using Game.Battle;
using UnityEngine;

/// <summary>
/// Контекст для передачи выбранного врага между сценами (Single Source of Truth).
/// </summary>
public static class BattleEnemyContext
{
    private static Game.Battle.EnemyData selectedEnemy;
    private static int selectedEnemyLevel;
    private static int selectedEnemyRankTier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        selectedEnemy = null;
        selectedEnemyLevel = 1;
        selectedEnemyRankTier = 0;
    }

    public static void Set(Game.Battle.EnemyData enemy)
    {
        selectedEnemy = enemy;
        selectedEnemyLevel = Mathf.Max(1, enemy != null ? enemy.minSpawnLevel : 1);
        selectedEnemyRankTier = Mathf.Max(0, enemy != null ? enemy.minSpawnRankTier : 0);
    }

    public static void Set(Game.Battle.EnemyData enemy, int enemyLevel, int enemyRankTier)
    {
        selectedEnemy = enemy;
        selectedEnemyLevel = Mathf.Max(1, enemyLevel);
        selectedEnemyRankTier = Mathf.Max(0, enemyRankTier);
    }

    public static Game.Battle.EnemyData Consume()
    {
        var result = selectedEnemy;
        selectedEnemy = null;
        return result;
    }

    public static Game.Battle.EnemyData Peek() => selectedEnemy;

    public static int PeekLevelOrDefault(Game.Battle.EnemyData enemy)
    {
        if (selectedEnemy != null && enemy == selectedEnemy)
            return Mathf.Max(1, selectedEnemyLevel);

        return Mathf.Max(1, enemy != null ? enemy.minSpawnLevel : 1);
    }

    public static int PeekRankTierOrDefault(Game.Battle.EnemyData enemy)
    {
        if (selectedEnemy != null && enemy == selectedEnemy)
            return Mathf.Max(0, selectedEnemyRankTier);

        return Mathf.Max(0, enemy != null ? enemy.minSpawnRankTier : 0);
    }
}
