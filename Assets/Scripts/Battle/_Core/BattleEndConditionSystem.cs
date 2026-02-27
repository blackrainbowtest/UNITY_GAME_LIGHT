using UnityEngine;
using Game.Battle.Combat;
using Game.Battle.Combat.Actions;

namespace Game.Battle
{
    public readonly struct BattleEndResolution
    {
        public bool ShouldFinish { get; }
        public bool PlayerWon { get; }
        public BattleFinishReason Reason { get; }
        public CombatActionId? WinningActionId { get; }

        public BattleEndResolution(bool shouldFinish, bool playerWon, BattleFinishReason reason, CombatActionId? winningActionId)
        {
            ShouldFinish = shouldFinish;
            PlayerWon = playerWon;
            Reason = reason;
            WinningActionId = winningActionId;
        }

        public static BattleEndResolution None => new BattleEndResolution(false, false, BattleFinishReason.Defeat, null);
    }

    public sealed class BattleEndConditionSystem
    {
        public bool TryResolveByLpThreshold(
            BattleContext context,
            CombatState combatState,
            bool actionByPlayer,
            CombatActionId? sourceActionId,
            out BattleEndResolution resolution)
        {
            resolution = BattleEndResolution.None;
            if (context == null || combatState == null)
                return false;

            int playerMaxLp = context.Player != null ? Mathf.Max(0, context.Player.MaxLP) : 0;
            int enemyMaxLp = context.Enemy != null ? Mathf.Max(0, context.Enemy.maxLp) : 0;

            bool playerReachedMaxLp = playerMaxLp > 0 && combatState.PlayerLp >= playerMaxLp;
            bool enemyReachedMaxLp = enemyMaxLp > 0 && combatState.EnemyLp >= enemyMaxLp;

            if (!playerReachedMaxLp && !enemyReachedMaxLp)
                return false;

            bool playerWon;
            if (playerReachedMaxLp && enemyReachedMaxLp)
            {
                playerWon = actionByPlayer;
            }
            else
            {
                playerWon = enemyReachedMaxLp;
            }

            var reason = playerWon ? BattleFinishReason.VictoryByLp : BattleFinishReason.DefeatByLp;
            resolution = new BattleEndResolution(true, playerWon, reason, sourceActionId);
            return true;
        }

        public bool TryResolveByHp(CombatState combatState, bool checkEnemyDeathAsPlayerVictory, CombatActionId? sourceActionId, out BattleEndResolution resolution)
        {
            resolution = BattleEndResolution.None;
            if (combatState == null)
                return false;

            if (checkEnemyDeathAsPlayerVictory && combatState.IsEnemyDead)
            {
                resolution = new BattleEndResolution(true, true, BattleFinishReason.Victory, sourceActionId);
                return true;
            }

            if (!checkEnemyDeathAsPlayerVictory && combatState.IsPlayerDead)
            {
                resolution = new BattleEndResolution(true, false, BattleFinishReason.Defeat, sourceActionId);
                return true;
            }

            return false;
        }
    }
}
