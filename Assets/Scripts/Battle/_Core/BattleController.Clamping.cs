//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleController.Clamping.cs                                         */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:39:38 by UDA                                                                    */
/*   Updated: 2026/01/23 01:39:38 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using Game.Battle.Combat;

namespace Game.Battle
{
    public partial class BattleController
    {
        private CombatState ClampPlayerResourcesToMax(CombatState state)
        {
            if (context?.Player == null)
                return state;

            var clampedHp = Mathf.Clamp(state.PlayerHp, 0, context.Player.MaxHP);
            var clampedMp = Mathf.Clamp(state.PlayerMp, 0, context.Player.MaxMP);
            var clampedSp = Mathf.Clamp(state.PlayerSp, 0, context.Player.MaxSP);
            var clampedLp = Mathf.Clamp(state.PlayerLp, 0, context.Player.MaxLP);

            if (clampedHp == state.PlayerHp && clampedMp == state.PlayerMp && clampedSp == state.PlayerSp && clampedLp == state.PlayerLp)
                return state;

            return state
                .WithPlayerHp(clampedHp)
                .WithPlayerMp(clampedMp)
                .WithPlayerSp(clampedSp)
                .WithPlayerLp(clampedLp);
        }

        private CombatState ClampEnemyResourcesToMax(CombatState state)
        {
            if (context?.Enemy == null)
                return state;

            var clampedHp = Mathf.Clamp(state.EnemyHp, 0, context.Enemy.maxHp);
            var clampedMp = Mathf.Clamp(state.EnemyMp, 0, context.Enemy.maxMp);
            var clampedSp = Mathf.Clamp(state.EnemySp, 0, context.Enemy.maxSp);
            var clampedLp = Mathf.Clamp(state.EnemyLp, 0, context.Enemy.maxLp);

            if (clampedHp == state.EnemyHp && clampedMp == state.EnemyMp && clampedSp == state.EnemySp && clampedLp == state.EnemyLp)
                return state;

            return state
                .WithEnemyHp(clampedHp)
                .WithEnemyMp(clampedMp)
                .WithEnemySp(clampedSp)
                .WithEnemyLp(clampedLp);
        }
    }
}
