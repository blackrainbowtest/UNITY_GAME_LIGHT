using System.Collections.Generic;
using UnityEngine;
using Game.Battle.Statuses;

namespace Game.Battle
{
    public partial class BattleController
    {
        private readonly List<StatusInstance> playerStatuses = new List<StatusInstance>(8);
        private readonly List<StatusInstance> enemyStatuses = new List<StatusInstance>(8);

        public IReadOnlyList<StatusInstance> PlayerStatuses => playerStatuses;
        public IReadOnlyList<StatusInstance> EnemyStatuses => enemyStatuses;

        public void AddOrRefreshPlayerStatus(StatusEffectId id, int turns)
        {
            AddOrRefreshPlayerStatusInternal(id, turns);
            PushHudState(showDeltas: false);
        }

        public void AddOrRefreshEnemyStatus(StatusEffectId id, int turns)
        {
            AddOrRefreshEnemyStatusInternal(id, turns);
            PushHudState(showDeltas: false);
        }

        public void ClearAllStatuses()
        {
            playerStatuses.Clear();
            enemyStatuses.Clear();
        }

        private void AddOrRefreshPlayerStatusInternal(StatusEffectId id, int turns)
        {
            AddOrRefreshStatus(playerStatuses, id, turns);
        }

        private void AddOrRefreshEnemyStatusInternal(StatusEffectId id, int turns)
        {
            AddOrRefreshStatus(enemyStatuses, id, turns);
        }

        private static void AddOrRefreshStatus(List<StatusInstance> list, StatusEffectId id, int turns)
        {
            if (list == null)
                return;

            if (turns < 0)
                turns = 0;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id != id)
                    continue;

                list[i] = new StatusInstance(id, Mathf.Max(list[i].TurnsLeft, turns));
                return;
            }

            list.Add(new StatusInstance(id, turns));
        }

        private void TickStatuses()
        {
            TickStatusList(playerStatuses);
            TickStatusList(enemyStatuses);
        }

        private static void TickStatusList(List<StatusInstance> list)
        {
            if (list == null || list.Count == 0)
                return;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var s = list[i];
                var next = s.TurnsLeft - 1;
                if (next <= 0)
                    list.RemoveAt(i);
                else
                    list[i] = s.WithTurnsLeft(next);
            }
        }
    }
}
