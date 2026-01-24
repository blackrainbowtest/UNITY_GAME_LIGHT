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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        selectedEnemy = null;
    }

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
