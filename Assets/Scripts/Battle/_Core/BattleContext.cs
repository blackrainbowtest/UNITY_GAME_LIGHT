//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleContext.cs                                                     */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:39:17 by UDA                                                                    */
/*   Updated: 2026/01/23 01:39:17 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

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
        public int EnemyLevel { get; }
        public int EnemyRankTier { get; }
        public string SourceLocationId { get; }

        public BattleContext(
            PlayerCombatSnapshot player,
            EnemyData enemy,
            BattleLocationData location,
            BattleMode mode,
            EnemyDifficulty enemyDifficulty = EnemyDifficulty.Normal,
            int enemyLevel = 1,
            int enemyRankTier = 0,
            string sourceLocationId = null)
        {
            Player = player;
            Enemy = enemy;
            Location = location;
            Mode = mode;
            EnemyDifficulty = enemyDifficulty;
            EnemyLevel = enemyLevel < 1 ? 1 : enemyLevel;
            EnemyRankTier = enemyRankTier < 0 ? 0 : enemyRankTier;
            SourceLocationId = sourceLocationId;
        }
    }
}
