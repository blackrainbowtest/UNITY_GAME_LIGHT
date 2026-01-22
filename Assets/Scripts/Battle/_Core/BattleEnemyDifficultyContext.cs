//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleEnemyDifficultyContext.cs                                      */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:40:43 by UDA                                                                    */
/*   Updated: 2026/01/23 01:40:43 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

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
