using System;
using System.Collections.Generic;
using System.Reflection;

namespace UDA2.SaveSystem.Guild
{
    public sealed partial class GuildService
    {
        private static void SanitizeQuestKillBaselines(List<SaveData.QuestKillBaselineEntry> entries)
        {
            if (entries == null)
                return;

            var merged = new Dictionary<string, SaveData.QuestKillBaselineEntry>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.questId) || string.IsNullOrWhiteSpace(e.enemyId))
                    continue;

                var questId = e.questId.Trim();
                var enemyId = e.enemyId.Trim();
                var key = questId + "::" + enemyId;
                var kills = Math.Max(0, e.killsAtAccept);

                if (merged.TryGetValue(key, out var existing))
                {
                    if (kills < existing.killsAtAccept)
                        existing.killsAtAccept = kills;
                    continue;
                }

                merged[key] = new SaveData.QuestKillBaselineEntry
                {
                    questId = questId,
                    enemyId = enemyId,
                    killsAtAccept = kills
                };
            }

            entries.Clear();
            foreach (var kv in merged)
                entries.Add(kv.Value);
        }

        private int GetMobKillCount(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
                return 0;

            var list = save.achievementStats?.mobKillsByEnemyId;
            if (list == null || list.Count == 0)
                return 0;

            var id = enemyId.Trim();
            for (var i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || string.IsNullOrWhiteSpace(e.enemyId))
                    continue;

                if (!string.Equals(e.enemyId, id, StringComparison.OrdinalIgnoreCase))
                    continue;

                return Math.Max(0, e.kills);
            }

            return 0;
        }

        private int GetQuestKillBaseline(string questId, string enemyId)
        {
            var baselines = save.progress.guild?.questKillBaselines;
            if (baselines == null || baselines.Count == 0)
                return 0;

            for (var i = 0; i < baselines.Count; i++)
            {
                var e = baselines[i];
                if (e == null)
                    continue;

                if (!string.Equals(e.questId, questId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(e.enemyId, enemyId, StringComparison.OrdinalIgnoreCase))
                    continue;

                return Math.Max(0, e.killsAtAccept);
            }

            return 0;
        }

        private void CaptureQuestKillBaselinesOnAccept(string questId, GuildQuestDefinitionAsset quest)
        {
            if (string.IsNullOrWhiteSpace(questId) || quest == null || quest.requiredMobKills == null)
                return;

            var baselines = save.progress.guild.questKillBaselines;
            if (baselines == null)
                return;

            RemoveQuestKillBaselines(questId);

            for (var i = 0; i < quest.requiredMobKills.Count; i++)
            {
                var requirement = quest.requiredMobKills[i];
                if (requirement == null)
                    continue;

                var enemyId = ResolveRequiredMobEnemyId(requirement);
                if (string.IsNullOrWhiteSpace(enemyId))
                    continue;

                var current = GetMobKillCount(enemyId);
                baselines.Add(new SaveData.QuestKillBaselineEntry
                {
                    questId = questId,
                    enemyId = enemyId,
                    killsAtAccept = current
                });
            }
        }

        private void RemoveQuestKillBaselines(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return;

            var baselines = save.progress.guild?.questKillBaselines;
            if (baselines == null)
                return;

            baselines.RemoveAll(e => e == null || string.Equals(e.questId, questId, StringComparison.OrdinalIgnoreCase));
        }

        private static string ResolveRequiredMobEnemyId(GuildMobKillAmount requirement)
        {
            if (requirement == null)
                return null;

            if (requirement.enemy != null)
            {
                var enemyObject = requirement.enemy;
                var enemyType = enemyObject.GetType();

                var idField = enemyType.GetField("id", BindingFlags.Public | BindingFlags.Instance);
                if (idField != null && idField.GetValue(enemyObject) is string idValue && !string.IsNullOrWhiteSpace(idValue))
                    return idValue.Trim();

                var idProp = enemyType.GetProperty("id", BindingFlags.Public | BindingFlags.Instance);
                if (idProp != null && idProp.GetValue(enemyObject) is string idPropValue && !string.IsNullOrWhiteSpace(idPropValue))
                    return idPropValue.Trim();

                var nameField = enemyType.GetField("enemyName", BindingFlags.Public | BindingFlags.Instance);
                if (nameField != null && nameField.GetValue(enemyObject) is string enemyNameValue && !string.IsNullOrWhiteSpace(enemyNameValue))
                    return enemyNameValue.Trim();

                var nameProp = enemyType.GetProperty("enemyName", BindingFlags.Public | BindingFlags.Instance);
                if (nameProp != null && nameProp.GetValue(enemyObject) is string enemyNamePropValue && !string.IsNullOrWhiteSpace(enemyNamePropValue))
                    return enemyNamePropValue.Trim();

                if (!string.IsNullOrWhiteSpace(enemyObject.name))
                    return enemyObject.name.Trim();
            }

            return string.IsNullOrWhiteSpace(requirement.enemyId) ? null : requirement.enemyId.Trim();
        }

        private static string ResolveRequiredMobDisplayName(GuildMobKillAmount requirement, string fallbackEnemyId)
        {
            if (requirement?.enemy != null)
            {
                var enemyObject = requirement.enemy;
                var enemyType = enemyObject.GetType();

                var enemyNameField = enemyType.GetField("enemyName", BindingFlags.Public | BindingFlags.Instance);
                if (enemyNameField != null && enemyNameField.GetValue(enemyObject) is string fieldValue && !string.IsNullOrWhiteSpace(fieldValue))
                    return fieldValue.Trim();

                var enemyNameProp = enemyType.GetProperty("enemyName", BindingFlags.Public | BindingFlags.Instance);
                if (enemyNameProp != null && enemyNameProp.GetValue(enemyObject) is string propValue && !string.IsNullOrWhiteSpace(propValue))
                    return propValue.Trim();

                if (!string.IsNullOrWhiteSpace(enemyObject.name))
                    return enemyObject.name.Trim();
            }

            return string.IsNullOrWhiteSpace(fallbackEnemyId) ? string.Empty : fallbackEnemyId.Trim();
        }
    }
}
