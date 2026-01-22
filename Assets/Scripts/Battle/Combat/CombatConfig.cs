//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\Combat\CombatConfig.cs                                                     */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:38:50 by UDA                                                                    */
/*   Updated: 2026/01/23 01:38:50 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

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

        // Possible future extensions:
        // - staminaCost
        // - manaCost
        // - attack types
    }
}
