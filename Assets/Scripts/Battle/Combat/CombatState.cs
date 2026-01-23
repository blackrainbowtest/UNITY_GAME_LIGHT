//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\Combat\CombatState.cs                                                      */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:39:01 by UDA                                                                    */
/*   Updated: 2026/01/23 01:39:01 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

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


        public bool PlayerBlockedLastTurn { get; }

        public CombatState(
            int playerHp, int playerMp, int playerSp, int playerLp,
            int enemyHp, int enemyMp, int enemySp, int enemyLp,
            bool playerBlockedLastTurn)
        {
            PlayerHp = playerHp;
            PlayerMp = playerMp;
            PlayerSp = playerSp;
            PlayerLp = playerLp;

            EnemyHp = enemyHp;
            EnemyMp = enemyMp;
            EnemySp = enemySp;
            EnemyLp = enemyLp;

            PlayerBlockedLastTurn = playerBlockedLastTurn;
        }



        public CombatState WithPlayerHp(int value)
            => new CombatState(
                value, PlayerMp, PlayerSp, PlayerLp,
                EnemyHp, EnemyMp, EnemySp, EnemyLp,
                PlayerBlockedLastTurn);

        public CombatState WithPlayerMp(int value)
            => new CombatState(
                PlayerHp, value, PlayerSp, PlayerLp,
                EnemyHp, EnemyMp, EnemySp, EnemyLp,
                PlayerBlockedLastTurn);

        public CombatState WithPlayerSp(int value)
            => new CombatState(
                PlayerHp, PlayerMp, value, PlayerLp,
                EnemyHp, EnemyMp, EnemySp, EnemyLp,
                PlayerBlockedLastTurn);

        public CombatState WithPlayerLp(int value)
            => new CombatState(
                PlayerHp, PlayerMp, PlayerSp, value,
                EnemyHp, EnemyMp, EnemySp, EnemyLp,
                PlayerBlockedLastTurn);

        public CombatState WithEnemyHp(int value)
            => new CombatState(
                PlayerHp, PlayerMp, PlayerSp, PlayerLp,
                value, EnemyMp, EnemySp, EnemyLp,
                PlayerBlockedLastTurn);

        public CombatState WithEnemyMp(int value)
            => new CombatState(
                PlayerHp, PlayerMp, PlayerSp, PlayerLp,
                EnemyHp, value, EnemySp, EnemyLp,
                PlayerBlockedLastTurn);

        public CombatState WithEnemySp(int value)
            => new CombatState(
                PlayerHp, PlayerMp, PlayerSp, PlayerLp,
                EnemyHp, EnemyMp, value, EnemyLp,
                PlayerBlockedLastTurn);

        public CombatState WithEnemyLp(int value)
            => new CombatState(
                PlayerHp, PlayerMp, PlayerSp, PlayerLp,
                EnemyHp, EnemyMp, EnemySp, value,
                PlayerBlockedLastTurn);

        public CombatState WithPlayerBlockedLastTurn(bool value)
            => new CombatState(
                PlayerHp, PlayerMp, PlayerSp, PlayerLp,
                EnemyHp, EnemyMp, EnemySp, EnemyLp,
                value);

        public bool IsPlayerDead => PlayerHp <= 0;
        public bool IsEnemyDead => EnemyHp <= 0;

        // Possible future extensions:
        // - Очередь ходов или флаг чей сейчас ход (IsPlayerTurn)
        // - Счетчик раунда (Round)
        // - Активные эффекты (PlayerEffects, EnemyEffects)
        // - Флаг завершения боя (IsBattleOver)
        // - Методы With* для других параметров
        // - Параметры для поддержки нескольких врагов
        // - События последнего действия (например, кто атаковал, какой урон нанесён)
    }
}
