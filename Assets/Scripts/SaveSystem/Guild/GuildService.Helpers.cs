using System;
using System.Collections.Generic;
using System.Linq;

namespace UDA2.SaveSystem.Guild
{
    public sealed partial class GuildService
    {
        private static void SanitizeQuestIdList(List<string> ids)
        {
            if (ids == null)
                return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = ids.Count - 1; i >= 0; i--)
            {
                var id = ids[i]?.Trim();
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                {
                    ids.RemoveAt(i);
                    continue;
                }

                ids[i] = id;
            }
        }

        private int GetInventoryItemCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || save.inventory?.items == null)
                return 0;

            return Math.Max(0, save.inventory.items.FirstOrDefault(i => i.itemId == itemId)?.count ?? 0);
        }

        private int GetStorageItemCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || save.storage?.items == null)
                return 0;

            return Math.Max(0, save.storage.items.FirstOrDefault(i => i.itemId == itemId)?.count ?? 0);
        }

        private static void AddUnique(List<string> list, string questId)
        {
            if (list == null || string.IsNullOrWhiteSpace(questId))
                return;

            if (!list.Contains(questId))
                list.Add(questId);
        }

        private static int ClampMinuteOfDay(int value) => Math.Max(0, Math.Min(1439, value));

        private static int CombineSeed(int day, int minuteOfDay, int completed)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + day;
                hash = hash * 31 + minuteOfDay;
                hash = hash * 31 + completed;
                return hash;
            }
        }

        private bool HasNoCurrentBoard()
        {
            var guild = save.progress.guild;
            return guild.activeQuestIds.Count == 0 && guild.selectedQuestIds.Count == 0;
        }

        private bool IsLikelyLegacyEmptyBoard(int nowDay)
        {
            var guild = save.progress.guild;
            return
                HasNoCurrentBoard() &&
                guild.lastQuestRefreshDay >= nowDay &&
                guild.completedQuestIds.Count == 0 &&
                guild.failedQuestIds.Count == 0;
        }
    }
}
