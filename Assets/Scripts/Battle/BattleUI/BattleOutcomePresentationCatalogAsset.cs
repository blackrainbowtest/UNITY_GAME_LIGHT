using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle.UI
{
    [CreateAssetMenu(
        fileName = "BattleOutcomePresentationCatalog",
        menuName = "UDA2/Battle/Outcome Presentation Catalog")]
    public sealed class BattleOutcomePresentationCatalogAsset : ScriptableObject
    {
        public enum ConditionType
        {
            None = 0,
            HasStatusEffect = 1,
            MissingStatusEffect = 2,
            HpPercentAtOrAbove = 3,
            HpPercentAtOrBelow = 4,
            MpPercentAtOrAbove = 5,
            MpPercentAtOrBelow = 6,
            SpPercentAtOrAbove = 7,
            SpPercentAtOrBelow = 8,
            LpPercentAtOrAbove = 9,
            LpPercentAtOrBelow = 10,
            PlayerLevelAtLeast = 11,
            PlayerLevelAtMost = 12
        }

        [Serializable]
        public sealed class Condition
        {
            public ConditionType type = ConditionType.None;
            [Tooltip("Used for string-based conditions (e.g. status effect id).")]
            public string stringValue;
            [Tooltip("Used for numeric conditions (percent or level).")]
            public float numberValue;
        }

        [Serializable]
        public sealed class VisualVariant
        {
            [Tooltip("Optional id to identify this variant in logs/tools.")]
            public string id;

            [Tooltip("Static image variant.")]
            public Sprite sprite;

            [Tooltip("Optional animated prefab variant (for VFX/animated image setup).")]
            public GameObject animatedPrefab;

            [Tooltip("Optional player animation asset (IdleAnimation) to play on Player animation target.")]
            public IdleAnimation playerAnimation;

            [Tooltip("Optional enemy animation asset (IdleAnimation) to play on Enemy animation target.")]
            public IdleAnimation enemyAnimation;

            [Tooltip("Selection weight when Random mode is used. <= 0 means ignored.")]
            public int weight = 1;
        }

        [Serializable]
        public sealed class RuleEntry
        {
            [Serializable]
            public sealed class FilterBlock
            {
                [Tooltip("If enabled, entry applies to any location (default behavior).")]
                public bool anyLocation = true;

                [Tooltip("Location id filter used when Any Location is disabled.")]
                public string locationId;

                [Tooltip("Higher priority wins when multiple entries match.")]
                public int priority;
            }

            [Serializable]
            public sealed class MatchBlock
            {
                [Tooltip("All conditions must pass.")]
                public List<Condition> conditions = new List<Condition>();
            }

            [Serializable]
            public sealed class PresentationBlock
            {
                [Tooltip("At least one variant should be assigned.")]
                public List<VisualVariant> variants = new List<VisualVariant>();
            }

            [Header("Filter")]
            public FilterBlock filter = new FilterBlock();

            [Header("Conditions")]
            public MatchBlock match = new MatchBlock();

            [Header("Presentation")]
            public PresentationBlock presentation = new PresentationBlock();
        }

        [Serializable]
        public sealed class OutcomeRuleGroup
        {
            [Header("Outcome")]
            public BattleFinishReason outcome = BattleFinishReason.Victory;

            [Header("Entries")]
            [Tooltip("Entries for this outcome. Highest priority wins among matching entries.")]
            public List<RuleEntry> entries = new List<RuleEntry>();
        }

        [Serializable]
        public sealed class EnemyGroup
        {
            [Header("Enemy")]
            [Tooltip("Assign EnemyData asset for this group. This gives object-picker workflow instead of typing ids manually.")]
            public EnemyData enemy;

            [Tooltip("If enabled, this group is a fallback for any enemy.")]
            public bool anyEnemy;

            [Tooltip("Optional override id. If empty, enemy.id is used.")]
            public string enemyIdOverride;

            [Header("Rules")]
            [Tooltip("Outcome groups evaluated only inside this enemy group.")]
            public List<OutcomeRuleGroup> rules = new List<OutcomeRuleGroup>();
        }

        [SerializeField] private List<EnemyGroup> enemyGroups = new List<EnemyGroup>();

        public IReadOnlyList<VisualVariant> ResolveVariants(BattleFinishReason outcome, string enemyId, string locationId, string sourceLocationId, SaveData save)
        {
            if (enemyGroups == null || enemyGroups.Count == 0)
                return Array.Empty<VisualVariant>();

            RuleEntry bestRule = null;
            var bestScore = int.MinValue;
            var normalizedEnemyId = string.IsNullOrWhiteSpace(enemyId) ? string.Empty : enemyId.Trim();
            var normalizedLocationId = string.IsNullOrWhiteSpace(locationId) ? string.Empty : locationId.Trim();
            var normalizedSourceLocationId = string.IsNullOrWhiteSpace(sourceLocationId) ? string.Empty : sourceLocationId.Trim();

            for (var i = 0; i < enemyGroups.Count; i++)
            {
                var group = enemyGroups[i];
                if (group == null)
                    continue;

                if (!MatchesEnemyGroup(group, normalizedEnemyId, out var isExactEnemyMatch))
                    continue;

                var rules = group.rules;
                if (rules == null || rules.Count == 0)
                    continue;

                for (var r = 0; r < rules.Count; r++)
                {
                    var outcomeGroup = rules[r];
                    if (outcomeGroup == null)
                        continue;

                    if (outcomeGroup.outcome != outcome)
                        continue;

                    var entries = outcomeGroup.entries;
                    if (entries == null || entries.Count == 0)
                        continue;

                    for (var e = 0; e < entries.Count; e++)
                    {
                        var entry = entries[e];
                        if (entry == null)
                            continue;

                        if (!MatchesLocation(entry, normalizedLocationId, normalizedSourceLocationId))
                            continue;

                        if (!MatchesAll(entry.match?.conditions, save))
                            continue;

                        if (!HasAnyVariant(entry.presentation?.variants))
                            continue;

                        // Exact enemy group should always win over fallback groups when priorities are equal.
                        var priority = entry.filter != null ? entry.filter.priority : 0;
                        var score = priority * 10 + (isExactEnemyMatch ? 1 : 0);
                        if (bestRule == null || score > bestScore)
                        {
                            bestRule = entry;
                            bestScore = score;
                        }
                    }
                }
            }

            return bestRule?.presentation?.variants ?? (IReadOnlyList<VisualVariant>)Array.Empty<VisualVariant>();
        }

        private static bool MatchesEnemyGroup(EnemyGroup group, string actualEnemyId, out bool isExactEnemyMatch)
        {
            isExactEnemyMatch = false;

            if (group == null)
                return false;

            if (group.anyEnemy)
                return true;

            var groupEnemyId = ResolveGroupEnemyId(group);
            if (string.IsNullOrWhiteSpace(groupEnemyId))
                return false;

            isExactEnemyMatch = string.Equals(groupEnemyId, actualEnemyId, StringComparison.OrdinalIgnoreCase);
            return isExactEnemyMatch;
        }

        private static string ResolveGroupEnemyId(EnemyGroup group)
        {
            if (group == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(group.enemyIdOverride))
                return group.enemyIdOverride.Trim();

            if (group.enemy != null && !string.IsNullOrWhiteSpace(group.enemy.id))
                return group.enemy.id.Trim();

            return string.Empty;
        }

        private static bool HasAnyVariant(List<VisualVariant> variants)
        {
            if (variants == null || variants.Count == 0)
                return false;

            for (var i = 0; i < variants.Count; i++)
            {
                var v = variants[i];
                if (v == null)
                    continue;

                if (v.sprite != null || v.animatedPrefab != null || v.playerAnimation != null || v.enemyAnimation != null)
                    return true;
            }

            return false;
        }

        private static bool MatchesLocation(RuleEntry rule, string actualLocationId, string sourceLocationId)
        {
            if (rule == null)
                return false;

            var filter = rule.filter;
            if (filter == null)
                return true;

            if (filter.anyLocation)
                return true;

            if (string.IsNullOrWhiteSpace(filter.locationId))
                return false;

            var target = filter.locationId.Trim();
            if (AreEquivalentLocationId(target, actualLocationId))
                return true;

            if (AreEquivalentLocationId(target, sourceLocationId))
                return true;

            return false;
        }

        private static bool AreEquivalentLocationId(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            var left = a.Trim();
            var right = b.Trim();

            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(ToBattleLocationId(left), right, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(ToDungeonLocationId(left), right, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(left, ToBattleLocationId(right), StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(left, ToDungeonLocationId(right), StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static string ToBattleLocationId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            var value = id.Trim();
            if (value.StartsWith("dld_", StringComparison.OrdinalIgnoreCase))
                return "bld_" + value.Substring(4);

            return value;
        }

        private static string ToDungeonLocationId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            var value = id.Trim();
            if (value.StartsWith("bld_", StringComparison.OrdinalIgnoreCase))
                return "dld_" + value.Substring(4);

            return value;
        }

        private static bool MatchesAll(List<Condition> conditions, SaveData save)
        {
            if (conditions == null || conditions.Count == 0)
                return true;

            for (var i = 0; i < conditions.Count; i++)
            {
                if (!Matches(conditions[i], save))
                    return false;
            }

            return true;
        }

        private static bool Matches(Condition condition, SaveData save)
        {
            if (condition == null)
                return true;

            switch (condition.type)
            {
                case ConditionType.None:
                    return true;
                case ConditionType.HasStatusEffect:
                    return HasStatusEffect(save, condition.stringValue);
                case ConditionType.MissingStatusEffect:
                    return !HasStatusEffect(save, condition.stringValue);
                case ConditionType.HpPercentAtOrAbove:
                    return GetPercent(GetHp(save), GetHpMax(save)) >= condition.numberValue;
                case ConditionType.HpPercentAtOrBelow:
                    return GetPercent(GetHp(save), GetHpMax(save)) <= condition.numberValue;
                case ConditionType.MpPercentAtOrAbove:
                    return GetPercent(GetMp(save), GetMpMax(save)) >= condition.numberValue;
                case ConditionType.MpPercentAtOrBelow:
                    return GetPercent(GetMp(save), GetMpMax(save)) <= condition.numberValue;
                case ConditionType.SpPercentAtOrAbove:
                    return GetPercent(GetSp(save), GetSpMax(save)) >= condition.numberValue;
                case ConditionType.SpPercentAtOrBelow:
                    return GetPercent(GetSp(save), GetSpMax(save)) <= condition.numberValue;
                case ConditionType.LpPercentAtOrAbove:
                    return GetPercent(GetLp(save), GetLpMax(save)) >= condition.numberValue;
                case ConditionType.LpPercentAtOrBelow:
                    return GetPercent(GetLp(save), GetLpMax(save)) <= condition.numberValue;
                case ConditionType.PlayerLevelAtLeast:
                    return GetLevel(save) >= Mathf.RoundToInt(condition.numberValue);
                case ConditionType.PlayerLevelAtMost:
                    return GetLevel(save) <= Mathf.RoundToInt(condition.numberValue);
                default:
                    return true;
            }
        }

        private static bool HasStatusEffect(SaveData save, string effectId)
        {
            if (save?.player?.statusEffects == null || string.IsNullOrWhiteSpace(effectId))
                return false;

            var effects = save.player.statusEffects;
            for (var i = 0; i < effects.Count; i++)
            {
                if (string.Equals(effects[i], effectId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static float GetPercent(int value, int max)
        {
            if (max <= 0)
                return 0f;

            return Mathf.Clamp01((float)value / max) * 100f;
        }

        private static int GetHp(SaveData save) => save?.player?.stats?.hp ?? 0;
        private static int GetHpMax(SaveData save) => save?.player?.stats?.hpMax ?? 0;
        private static int GetMp(SaveData save) => save?.player?.stats?.mp ?? 0;
        private static int GetMpMax(SaveData save) => save?.player?.stats?.mpMax ?? 0;
        private static int GetSp(SaveData save) => save?.player?.stats?.sp ?? 0;
        private static int GetSpMax(SaveData save) => save?.player?.stats?.spMax ?? 0;
        private static int GetLp(SaveData save) => save?.player?.stats?.lp ?? 0;
        private static int GetLpMax(SaveData save) => save?.player?.stats?.lpMax ?? 0;
        private static int GetLevel(SaveData save) => save?.player?.level ?? 0;
    }
}
