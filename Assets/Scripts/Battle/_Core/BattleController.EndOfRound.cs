//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleController.EndOfRound.cs                                       */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:40:09 by UDA                                                                    */
/*   Updated: 2026/01/23 01:40:09 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using System.Collections.Generic;
using UnityEngine;
using Game.Battle.Statuses;

namespace Game.Battle
{
    public partial class BattleController
    {
        private readonly struct EndOfRoundEffect
        {
            public string SourceId { get; }

            public int PlayerHpDelta { get; }
            public int PlayerMpDelta { get; }
            public int PlayerSpDelta { get; }
            public int PlayerLpDelta { get; }

            public int EnemyHpDelta { get; }
            public int EnemyMpDelta { get; }
            public int EnemySpDelta { get; }
            public int EnemyLpDelta { get; }

            public EndOfRoundEffect(
                string sourceId,
                int playerHpDelta, int playerMpDelta, int playerSpDelta, int playerLpDelta,
                int enemyHpDelta, int enemyMpDelta, int enemySpDelta, int enemyLpDelta)
            {
                SourceId = sourceId;

                PlayerHpDelta = playerHpDelta;
                PlayerMpDelta = playerMpDelta;
                PlayerSpDelta = playerSpDelta;
                PlayerLpDelta = playerLpDelta;

                EnemyHpDelta = enemyHpDelta;
                EnemyMpDelta = enemyMpDelta;
                EnemySpDelta = enemySpDelta;
                EnemyLpDelta = enemyLpDelta;
            }
        }

        private readonly List<EndOfRoundEffect> pendingEndOfRoundEffects = new List<EndOfRoundEffect>(8);

        /// <summary>
        /// Queues end-of-round resource changes (poison/burn/auras/etc).
        /// All queued effects are summed and applied once in ApplyEndOfRoundEffects(), so HUD shows one net delta.
        /// </summary>
        public void QueueEndOfRoundEffect(
            string sourceId,
            int playerHpDelta = 0,
            int playerMpDelta = 0,
            int playerSpDelta = 0,
            int playerLpDelta = 0,
            int enemyHpDelta = 0,
            int enemyMpDelta = 0,
            int enemySpDelta = 0,
            int enemyLpDelta = 0)
        {
            pendingEndOfRoundEffects.Add(new EndOfRoundEffect(
                sourceId,
                playerHpDelta, playerMpDelta, playerSpDelta, playerLpDelta,
                enemyHpDelta, enemyMpDelta, enemySpDelta, enemyLpDelta));
        }

        public void ClearEndOfRoundEffects()
        {
            pendingEndOfRoundEffects.Clear();
        }

        private void ApplyEndOfRoundEffects()
        {
            // End-of-round effects are applied as a single batch so HUD shows one net delta.

            if (statusCatalog == null && !warnedMissingStatusCatalog && ((playerStatuses != null && playerStatuses.Count > 0) || (enemyStatuses != null && enemyStatuses.Count > 0)))
            {
                warnedMissingStatusCatalog = true;
                Debug.LogWarning("[BattleController] statusCatalog is not assigned. Active statuses will tick down, but catalog-based effects (HP/MP/SP/LP) are not applied.", this);
            }

            var totalPlayerHpDelta = 0;
            var totalPlayerMpDelta = 0;
            var totalPlayerSpDelta = 0;
            var totalPlayerLpDelta = 0;

            var totalEnemyHpDelta = 0;
            var totalEnemyMpDelta = 0;
            var totalEnemySpDelta = 0;
            var totalEnemyLpDelta = 0;

            // 0) Status effects (burning/poison/etc) contribute into the same end-of-round batch.
            AccumulateStatusEffects(
                playerStatuses,
                isPlayerSide: true,
                ref totalPlayerHpDelta,
                ref totalPlayerMpDelta,
                ref totalPlayerSpDelta,
                ref totalPlayerLpDelta,
                ref totalEnemyHpDelta,
                ref totalEnemyMpDelta,
                ref totalEnemySpDelta,
                ref totalEnemyLpDelta);

            AccumulateStatusEffects(
                enemyStatuses,
                isPlayerSide: false,
                ref totalPlayerHpDelta,
                ref totalPlayerMpDelta,
                ref totalPlayerSpDelta,
                ref totalPlayerLpDelta,
                ref totalEnemyHpDelta,
                ref totalEnemyMpDelta,
                ref totalEnemySpDelta,
                ref totalEnemyLpDelta);

            // 1) External queued effects (poison/burn/auras/etc).
            for (var i = 0; i < pendingEndOfRoundEffects.Count; i++)
            {
                var e = pendingEndOfRoundEffects[i];
                totalPlayerHpDelta += e.PlayerHpDelta;
                totalPlayerMpDelta += e.PlayerMpDelta;
                totalPlayerSpDelta += e.PlayerSpDelta;
                totalPlayerLpDelta += e.PlayerLpDelta;

                totalEnemyHpDelta += e.EnemyHpDelta;
                totalEnemyMpDelta += e.EnemyMpDelta;
                totalEnemySpDelta += e.EnemySpDelta;
                totalEnemyLpDelta += e.EnemyLpDelta;
            }

            // 2) Passive regen (treated as part of end-of-round batch).
            if (context?.Player != null)
            {
                totalPlayerHpDelta += Mathf.Max(0, context.Player.RegenHpPerTurn);
                totalPlayerMpDelta += Mathf.Max(0, context.Player.RegenMpPerTurn);
                totalPlayerSpDelta += Mathf.Max(0, context.Player.RegenSpPerTurn);
            }

            if (context?.Enemy != null)
            {
                totalEnemyHpDelta += Mathf.Max(0, context.Enemy.regenHpPerTurn);
                totalEnemyMpDelta += Mathf.Max(0, context.Enemy.regenMpPerTurn);
                totalEnemySpDelta += Mathf.Max(0, context.Enemy.regenSpPerTurn);
            }

            // Apply totals (clamped to max pools).
            if (context?.Player != null)
            {
                var hp = Mathf.Clamp(combatState.PlayerHp + totalPlayerHpDelta, 0, context.Player.MaxHP);
                var mp = Mathf.Clamp(combatState.PlayerMp + totalPlayerMpDelta, 0, context.Player.MaxMP);
                var sp = Mathf.Clamp(combatState.PlayerSp + totalPlayerSpDelta, 0, context.Player.MaxSP);
                var lp = Mathf.Clamp(combatState.PlayerLp + totalPlayerLpDelta, 0, context.Player.MaxLP);

                combatState = combatState
                    .WithPlayerHp(hp)
                    .WithPlayerMp(mp)
                    .WithPlayerSp(sp)
                    .WithPlayerLp(lp);
            }

            if (context?.Enemy != null)
            {
                var hp = Mathf.Clamp(combatState.EnemyHp + totalEnemyHpDelta, 0, context.Enemy.maxHp);
                var mp = Mathf.Clamp(combatState.EnemyMp + totalEnemyMpDelta, 0, context.Enemy.maxMp);
                var sp = Mathf.Clamp(combatState.EnemySp + totalEnemySpDelta, 0, context.Enemy.maxSp);
                var lp = Mathf.Clamp(combatState.EnemyLp + totalEnemyLpDelta, 0, context.Enemy.maxLp);

                combatState = combatState
                    .WithEnemyHp(hp)
                    .WithEnemyMp(mp)
                    .WithEnemySp(sp)
                    .WithEnemyLp(lp);
            }

            pendingEndOfRoundEffects.Clear();

            TickStatuses();

            // If Block expired by duration, clear remaining armor.
            // NOTE: Do NOT clear PlayerBlockedLastTurn / absorbed amount here:
            // the Block status icon can be removed early when armor is fully consumed,
            // but the player should still be able to CounterAttack on the next turn.
            if (!HasStatus(playerStatuses, StatusEffectId.Block))
            {
                combatState = combatState
                    .WithPlayerBlockArmor(0);
            }

            if (!HasStatus(enemyStatuses, StatusEffectId.Block))
            {
                combatState = combatState
                    .WithEnemyBlockArmor(0);
            }
        }

        private void AccumulateStatusEffects(
            List<StatusInstance> list,
            bool isPlayerSide,
            ref int totalPlayerHpDelta,
            ref int totalPlayerMpDelta,
            ref int totalPlayerSpDelta,
            ref int totalPlayerLpDelta,
            ref int totalEnemyHpDelta,
            ref int totalEnemyMpDelta,
            ref int totalEnemySpDelta,
            ref int totalEnemyLpDelta)
        {
            if (list == null || list.Count == 0 || statusCatalog == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                var status = list[i];
                if (!statusCatalog.TryGet(status.Id, out var def) || def.effects == null || def.effects.Length == 0)
                    continue;

                for (int e = 0; e < def.effects.Length; e++)
                {
                    var effect = def.effects[e];
                    int delta = effect.GetSignedDelta();
                    if (delta == 0)
                        continue;

                    if (isPlayerSide)
                    {
                        switch (effect.stat)
                        {
                            case BattleStatusCatalog.ResourceStat.Hp: totalPlayerHpDelta += delta; break;
                            case BattleStatusCatalog.ResourceStat.Mp: totalPlayerMpDelta += delta; break;
                            case BattleStatusCatalog.ResourceStat.Sp: totalPlayerSpDelta += delta; break;
                            case BattleStatusCatalog.ResourceStat.Lp: totalPlayerLpDelta += delta; break;
                        }
                    }
                    else
                    {
                        switch (effect.stat)
                        {
                            case BattleStatusCatalog.ResourceStat.Hp: totalEnemyHpDelta += delta; break;
                            case BattleStatusCatalog.ResourceStat.Mp: totalEnemyMpDelta += delta; break;
                            case BattleStatusCatalog.ResourceStat.Sp: totalEnemySpDelta += delta; break;
                            case BattleStatusCatalog.ResourceStat.Lp: totalEnemyLpDelta += delta; break;
                        }
                    }
                }
            }
        }

        private static bool HasStatus(List<StatusInstance> list, StatusEffectId id)
        {
            if (list == null || list.Count == 0)
                return false;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id == id)
                    return true;
            }

            return false;
        }
    }
}
