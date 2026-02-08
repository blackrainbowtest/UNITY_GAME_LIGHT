//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleController.Statuses.cs                                         */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:40:31 by UDA                                                                    */
/*   Updated: 2026/01/23 01:40:31 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

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

        private static void SetStatusExact(List<StatusInstance> list, StatusEffectId id, int turns)
        {
            if (list == null)
                return;

            if (turns < 0)
                turns = 0;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id != id)
                    continue;

                if (turns <= 0)
                    list.RemoveAt(i);
                else
                    list[i] = new StatusInstance(id, turns);
                return;
            }

            if (turns > 0)
                list.Add(new StatusInstance(id, turns));
        }

        private static void RemoveStatus(List<StatusInstance> list, StatusEffectId id)
        {
            if (list == null || list.Count == 0)
                return;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id != id)
                    continue;

                list.RemoveAt(i);
                return;
            }
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
