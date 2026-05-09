using System;
using Game.Battle.UI;

namespace Game.Progression
{
    /// <summary>
    /// Evaluates cumulative battle stats using catalog rules and persists unlocked IDs.
    /// </summary>
    public static class BattleAchievementService
    {
        public static int EvaluateAndCollectNewUnlockIds(
            SaveData save,
            BattleAchievementCatalogAsset catalog,
            System.Collections.Generic.ICollection<string> newlyUnlockedIds)
        {
            if (save == null || catalog == null)
                return 0;

            if (save.achievementStats == null)
                save.achievementStats = new SaveData.AchievementStats();

            if (save.achievementStats.unlockedAchievementIds == null)
                save.achievementStats.unlockedAchievementIds = new System.Collections.Generic.List<string>();

            if (save.progress == null)
                save.progress = new SaveData.Progress();

            if (save.progress.flags == null)
                save.progress.flags = new System.Collections.Generic.Dictionary<string, bool>();

            int unlockedNow = 0;
            var stats = save.achievementStats;
            var unlockedIds = save.achievementStats.unlockedAchievementIds;
            var definitions = catalog.Achievements;

            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null || !definition.enabled)
                    continue;

                var id = string.IsNullOrWhiteSpace(definition.id) ? null : definition.id.Trim();
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                int value = ResolveMetricValue(stats, definition.metric);
                if (value < Math.Max(1, definition.threshold))
                    continue;

                if (ContainsId(unlockedIds, id))
                    continue;

                unlockedIds.Add(id);
                save.progress.flags[id] = true;
                newlyUnlockedIds?.Add(id);
                unlockedNow++;
            }

            return unlockedNow;
        }

        public static bool IsUnlocked(SaveData save, string achievementId)
        {
            if (save == null || save.achievementStats == null || string.IsNullOrWhiteSpace(achievementId))
                return false;

            return ContainsId(save.achievementStats.unlockedAchievementIds, achievementId.Trim());
        }

        private static bool ContainsId(System.Collections.Generic.IReadOnlyList<string> ids, string id)
        {
            if (ids == null || string.IsNullOrWhiteSpace(id))
                return false;

            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static int ResolveMetricValue(SaveData.AchievementStats stats, BattleAchievementCatalogAsset.AchievementMetric metric)
        {
            if (stats == null)
                return 0;

            switch (metric)
            {
                case BattleAchievementCatalogAsset.AchievementMetric.BattlesFinished:
                    return Math.Max(0, stats.battlesFinished);
                case BattleAchievementCatalogAsset.AchievementMetric.BattlesWon:
                    return Math.Max(0, stats.battlesWon);
                case BattleAchievementCatalogAsset.AchievementMetric.BattlesLost:
                    return Math.Max(0, stats.battlesLost);
                case BattleAchievementCatalogAsset.AchievementMetric.EscapesSuccessful:
                    return Math.Max(0, stats.escapesSuccessful);
                case BattleAchievementCatalogAsset.AchievementMetric.TotalMobKills:
                    return Math.Max(0, stats.totalMobKills);
                case BattleAchievementCatalogAsset.AchievementMetric.TotalBattleDurationSeconds:
                    return Math.Max(0, stats.totalBattleDurationSeconds);
                case BattleAchievementCatalogAsset.AchievementMetric.TotalHpDamageDealtToEnemies:
                    return Math.Max(0, stats.totalHpDamageDealtToEnemies);
                case BattleAchievementCatalogAsset.AchievementMetric.TotalHpDamageTakenFromEnemies:
                    return Math.Max(0, stats.totalHpDamageTakenFromEnemies);
                case BattleAchievementCatalogAsset.AchievementMetric.TotalLpDamageDealtToEnemies:
                    return Math.Max(0, stats.totalLpDamageDealtToEnemies);
                case BattleAchievementCatalogAsset.AchievementMetric.TotalLpDamageTakenFromEnemies:
                    return Math.Max(0, stats.totalLpDamageTakenFromEnemies);
                case BattleAchievementCatalogAsset.AchievementMetric.TotalGoldEarned:
                    return Math.Max(0, stats.totalGoldEarned);
                case BattleAchievementCatalogAsset.AchievementMetric.TotalExpEarned:
                    return Math.Max(0, stats.totalExpEarned);
                default:
                    return 0;
            }
        }
    }
}
