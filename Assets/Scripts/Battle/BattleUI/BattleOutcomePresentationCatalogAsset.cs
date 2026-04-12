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
        private static readonly BattleFinishReason[] DefaultOutcomes =
        {
            BattleFinishReason.Victory,
            BattleFinishReason.Defeat,
            BattleFinishReason.EscapeSuccess,
            BattleFinishReason.EscapeFailed,
            BattleFinishReason.Surrender,
            BattleFinishReason.VictoryByLp,
            BattleFinishReason.DefeatByLp
        };

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

        [Serializable]
        public sealed class LocationBackgroundFallback
        {
            [Tooltip("If enabled, this entry can be used as a default for any location.")]
            public bool anyLocation;

            [Tooltip("Optional location asset reference. If assigned, id is taken from this asset.")]
            public BattleLocationData location;

            [Tooltip("Optional location id override. Used when Location is not assigned.")]
            public string locationId;

            [Tooltip("Background sprite used for this location fallback.")]
            public Sprite sprite;
        }

        [SerializeField] private List<EnemyGroup> enemyGroups = new List<EnemyGroup>();
        [Header("Location Background Fallback")]
        [Tooltip("Global location-to-background mapping used as fallback for all enemy groups/outcomes.")]
        [SerializeField] private List<LocationBackgroundFallback> locationBackgrounds = new List<LocationBackgroundFallback>();
        [Tooltip("If enabled, variant.sprite can override location fallback background. If disabled, location fallback background is always used.")]
        [SerializeField] private bool useVariantSpriteOverrides = true;
        [Header("Global Fallback")]
        [Tooltip("Used when no enemy/outcome/location rule matched. Acts as a reserve default presentation.")]
        [SerializeField] private List<VisualVariant> fallbackVariants = new List<VisualVariant>();

        public bool UseVariantSpriteOverrides => useVariantSpriteOverrides;

        private void OnValidate()
        {
            EnsureAllOutcomesInEnemyGroups();
            SyncLocationBackgroundIds();
        }

        [ContextMenu("Ensure All Outcomes In Enemy Groups")]
        public void EnsureAllOutcomesInEnemyGroups()
        {
            if (enemyGroups == null || enemyGroups.Count == 0)
                return;

            for (int i = 0; i < enemyGroups.Count; i++)
            {
                var group = enemyGroups[i];
                if (group == null)
                    continue;

                if (group.rules == null)
                    group.rules = new List<OutcomeRuleGroup>();

                for (int r = 0; r < DefaultOutcomes.Length; r++)
                {
                    var outcome = DefaultOutcomes[r];
                    if (HasOutcomeGroup(group.rules, outcome))
                        continue;

                    group.rules.Add(new OutcomeRuleGroup
                    {
                        outcome = outcome,
                        entries = new List<RuleEntry>()
                    });
                }
            }
        }

        [ContextMenu("Normalize And Sort Catalog")]
        public void NormalizeAndSortCatalog()
        {
            SortEnemyGroups();
            SortLocationBackgrounds();
        }

        public Sprite ResolveLocationFallbackSprite(string locationId, string sourceLocationId, Sprite defaultSprite = null)
        {
            if (locationBackgrounds == null || locationBackgrounds.Count == 0)
                return defaultSprite;

            var normalizedLocationId = string.IsNullOrWhiteSpace(locationId) ? string.Empty : locationId.Trim();
            var normalizedSourceLocationId = string.IsNullOrWhiteSpace(sourceLocationId) ? string.Empty : sourceLocationId.Trim();
            Sprite anyLocationSprite = null;

            for (int i = 0; i < locationBackgrounds.Count; i++)
            {
                var entry = locationBackgrounds[i];
                if (entry == null || entry.sprite == null)
                    continue;

                if (entry.anyLocation)
                {
                    if (anyLocationSprite == null)
                        anyLocationSprite = entry.sprite;

                    continue;
                }

                var entryId = ResolveLocationFallbackId(entry);
                if (string.IsNullOrWhiteSpace(entryId))
                    continue;

                if (AreEquivalentLocationId(entryId, normalizedLocationId) || AreEquivalentLocationId(entryId, normalizedSourceLocationId))
                    return entry.sprite;
            }

            return anyLocationSprite != null ? anyLocationSprite : defaultSprite;
        }

        public IReadOnlyList<VisualVariant> ResolveVariants(BattleFinishReason outcome, string enemyId, string locationId, string sourceLocationId, SaveData save)
        {
            return ResolveVariants(outcome, enemyId, locationId, sourceLocationId, save, debugLogs: false, out _);
        }

        public IReadOnlyList<VisualVariant> ResolveVariants(
            BattleFinishReason outcome,
            string enemyId,
            string locationId,
            string sourceLocationId,
            SaveData save,
            bool debugLogs,
            out string debugReport)
        {
            List<string> debugLines = debugLogs ? new List<string>(64) : null;
            if (debugLines != null)
            {
                debugLines.Add($"Input: outcome={outcome}, enemyId='{enemyId}', locationId='{locationId}', sourceLocationId='{sourceLocationId}'");
            }

            if (enemyGroups == null || enemyGroups.Count == 0)
            {
                debugReport = debugLines != null ? string.Join("\n", debugLines) + "\nNo enemy groups configured." : null;
                return Array.Empty<VisualVariant>();
            }

            RuleEntry bestRule = null;
            int bestGroupIndex = -1;
            int bestOutcomeIndex = -1;
            int bestEntryIndex = -1;
            var bestScore = int.MinValue;
            var normalizedEnemyId = string.IsNullOrWhiteSpace(enemyId) ? string.Empty : enemyId.Trim();
            var normalizedLocationId = string.IsNullOrWhiteSpace(locationId) ? string.Empty : locationId.Trim();
            var normalizedSourceLocationId = string.IsNullOrWhiteSpace(sourceLocationId) ? string.Empty : sourceLocationId.Trim();

            for (var i = 0; i < enemyGroups.Count; i++)
            {
                var group = enemyGroups[i];
                if (group == null)
                {
                    if (debugLines != null)
                        debugLines.Add($"Group[{i}]: null -> skip");
                    continue;
                }

                if (!MatchesEnemyGroup(group, normalizedEnemyId, out var isExactEnemyMatch))
                {
                    if (debugLines != null)
                    {
                        var groupEnemyId = ResolveGroupEnemyId(group);
                        debugLines.Add($"Group[{i}]: enemy mismatch (anyEnemy={group.anyEnemy}, groupEnemyId='{groupEnemyId}', actualEnemyId='{normalizedEnemyId}') -> skip");
                    }
                    continue;
                }

                if (debugLines != null)
                    debugLines.Add($"Group[{i}]: enemy matched (exact={isExactEnemyMatch})");

                var rules = group.rules;
                if (rules == null || rules.Count == 0)
                {
                    if (debugLines != null)
                        debugLines.Add($"Group[{i}]: no outcome rules");
                    continue;
                }

                for (var r = 0; r < rules.Count; r++)
                {
                    var outcomeGroup = rules[r];
                    if (outcomeGroup == null)
                    {
                        if (debugLines != null)
                            debugLines.Add($"Group[{i}] Outcome[{r}]: null -> skip");
                        continue;
                    }

                    if (outcomeGroup.outcome != outcome)
                    {
                        if (debugLines != null)
                            debugLines.Add($"Group[{i}] Outcome[{r}]: expected={outcomeGroup.outcome}, actual={outcome} -> skip");
                        continue;
                    }

                    if (debugLines != null)
                        debugLines.Add($"Group[{i}] Outcome[{r}]: matched outcome={outcome}");

                    var entries = outcomeGroup.entries;
                    if (entries == null || entries.Count == 0)
                    {
                        if (debugLines != null)
                            debugLines.Add($"Group[{i}] Outcome[{r}]: entries empty");
                        continue;
                    }

                    for (var e = 0; e < entries.Count; e++)
                    {
                        var entry = entries[e];
                        if (entry == null)
                        {
                            if (debugLines != null)
                                debugLines.Add($"Group[{i}] Outcome[{r}] Entry[{e}]: null -> skip");
                            continue;
                        }

                        var locationMatched = MatchesLocation(entry, normalizedLocationId, normalizedSourceLocationId);
                        if (!locationMatched)
                        {
                            if (debugLines != null)
                            {
                                var expectedLoc = entry.filter != null ? entry.filter.locationId : "<null filter>";
                                var anyLoc = entry.filter != null && entry.filter.anyLocation;
                                debugLines.Add($"Group[{i}] Outcome[{r}] Entry[{e}]: location mismatch (anyLocation={anyLoc}, expected='{expectedLoc}', actual='{normalizedLocationId}', source='{normalizedSourceLocationId}')");
                            }
                            continue;
                        }

                        if (!MatchesAll(entry.match?.conditions, save))
                        {
                            if (debugLines != null)
                                debugLines.Add($"Group[{i}] Outcome[{r}] Entry[{e}]: conditions mismatch");
                            continue;
                        }

                        if (!HasAnyVariant(entry.presentation?.variants))
                        {
                            if (debugLines != null)
                                debugLines.Add($"Group[{i}] Outcome[{r}] Entry[{e}]: no usable variants");
                            continue;
                        }

                        // Exact enemy group should always win over fallback groups when priorities are equal.
                        var priority = entry.filter != null ? entry.filter.priority : 0;
                        var score = priority * 10 + (isExactEnemyMatch ? 1 : 0);
                        if (debugLines != null)
                            debugLines.Add($"Group[{i}] Outcome[{r}] Entry[{e}]: candidate matched (priority={priority}, score={score})");

                        if (bestRule == null || score > bestScore)
                        {
                            bestRule = entry;
                            bestScore = score;
                            bestGroupIndex = i;
                            bestOutcomeIndex = r;
                            bestEntryIndex = e;

                            if (debugLines != null)
                                debugLines.Add($"Group[{i}] Outcome[{r}] Entry[{e}]: selected as best so far");
                        }
                    }
                }
            }

            if (debugLines != null)
            {
                if (bestRule == null)
                {
                    debugLines.Add("Result: no matching rule found.");
                }
                else
                {
                    var variantsCount = bestRule.presentation?.variants != null ? bestRule.presentation.variants.Count : 0;
                    debugLines.Add($"Result: selected Group[{bestGroupIndex}] Outcome[{bestOutcomeIndex}] Entry[{bestEntryIndex}], variants={variantsCount}, bestScore={bestScore}");
                }
            }

            debugReport = debugLines != null ? string.Join("\n", debugLines) : null;

            if (bestRule?.presentation?.variants != null)
                return bestRule.presentation.variants;

            if (HasAnyVariant(fallbackVariants))
            {
                if (debugLines != null)
                    debugReport += "\nFallback: using global fallback variants.";

                return fallbackVariants;
            }

            if (debugLines != null)
                debugReport += "\nFallback: none configured.";

            return Array.Empty<VisualVariant>();
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

        private static string ResolveLocationFallbackId(LocationBackgroundFallback entry)
        {
            if (entry == null)
                return string.Empty;

            if (entry.location != null)
            {
                var locationAssetId = ResolveLocationAssetId(entry.location);
                if (!string.IsNullOrWhiteSpace(locationAssetId))
                    return locationAssetId;
            }

            if (!string.IsNullOrWhiteSpace(entry.locationId))
                return entry.locationId.Trim();

            return string.Empty;
        }

        private void SyncLocationBackgroundIds()
        {
            if (locationBackgrounds == null || locationBackgrounds.Count == 0)
                return;

            for (int i = 0; i < locationBackgrounds.Count; i++)
            {
                var entry = locationBackgrounds[i];
                if (entry == null || entry.location == null)
                    continue;

                var normalizedId = ResolveLocationAssetId(entry.location);
                if (string.IsNullOrWhiteSpace(normalizedId))
                    continue;

                if (!string.Equals(entry.locationId, normalizedId, StringComparison.Ordinal))
                    entry.locationId = normalizedId;
            }
        }

        private void SortEnemyGroups()
        {
            if (enemyGroups == null || enemyGroups.Count == 0)
                return;

            for (int i = 0; i < enemyGroups.Count; i++)
            {
                var group = enemyGroups[i];
                if (group == null)
                    continue;

                NormalizeAndSortOutcomeGroups(group);
            }

            enemyGroups.Sort(CompareEnemyGroups);
        }

        private static int CompareEnemyGroups(EnemyGroup a, EnemyGroup b)
        {
            if (ReferenceEquals(a, b))
                return 0;

            if (a == null)
                return 1;
            if (b == null)
                return -1;

            if (a.anyEnemy != b.anyEnemy)
                return a.anyEnemy ? 1 : -1;

            var aId = ResolveGroupEnemyId(a);
            var bId = ResolveGroupEnemyId(b);
            return string.Compare(aId, bId, StringComparison.OrdinalIgnoreCase);
        }

        private static void NormalizeAndSortOutcomeGroups(EnemyGroup group)
        {
            if (group == null)
                return;

            var rules = group.rules;
            if (rules == null || rules.Count == 0)
                return;

            var mergedByOutcome = new Dictionary<BattleFinishReason, OutcomeRuleGroup>();
            for (int i = 0; i < rules.Count; i++)
            {
                var src = rules[i];
                if (src == null)
                    continue;

                if (!mergedByOutcome.TryGetValue(src.outcome, out var dst))
                {
                    dst = new OutcomeRuleGroup { outcome = src.outcome, entries = new List<RuleEntry>() };
                    mergedByOutcome[src.outcome] = dst;
                }

                if (src.entries != null && src.entries.Count > 0)
                    dst.entries.AddRange(src.entries);
            }

            rules.Clear();
            foreach (var kv in mergedByOutcome)
            {
                var outcomeGroup = kv.Value;
                if (outcomeGroup.entries != null)
                    outcomeGroup.entries.Sort(CompareRuleEntries);
                rules.Add(outcomeGroup);
            }

            rules.Sort((left, right) =>
            {
                if (ReferenceEquals(left, right))
                    return 0;
                if (left == null)
                    return 1;
                if (right == null)
                    return -1;
                return OutcomeSortKey(left.outcome).CompareTo(OutcomeSortKey(right.outcome));
            });
        }

        private static bool HasOutcomeGroup(List<OutcomeRuleGroup> rules, BattleFinishReason outcome)
        {
            if (rules == null || rules.Count == 0)
                return false;

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null)
                    continue;

                if (rule.outcome == outcome)
                    return true;
            }

            return false;
        }

        private static int CompareRuleEntries(RuleEntry a, RuleEntry b)
        {
            if (ReferenceEquals(a, b))
                return 0;
            if (a == null)
                return 1;
            if (b == null)
                return -1;

            int aPriority = a.filter != null ? a.filter.priority : 0;
            int bPriority = b.filter != null ? b.filter.priority : 0;
            int byPriority = bPriority.CompareTo(aPriority);
            if (byPriority != 0)
                return byPriority;

            bool aAnyLoc = a.filter == null || a.filter.anyLocation;
            bool bAnyLoc = b.filter == null || b.filter.anyLocation;
            if (aAnyLoc != bAnyLoc)
                return aAnyLoc ? 1 : -1;

            int aConditions = a.match != null && a.match.conditions != null ? a.match.conditions.Count : 0;
            int bConditions = b.match != null && b.match.conditions != null ? b.match.conditions.Count : 0;
            int byConditionCount = bConditions.CompareTo(aConditions);
            if (byConditionCount != 0)
                return byConditionCount;

            var aLoc = a.filter != null ? a.filter.locationId : string.Empty;
            var bLoc = b.filter != null ? b.filter.locationId : string.Empty;
            return string.Compare(aLoc, bLoc, StringComparison.OrdinalIgnoreCase);
        }

        private static int OutcomeSortKey(BattleFinishReason reason)
        {
            switch (reason)
            {
                case BattleFinishReason.Victory:
                    return 0;
                case BattleFinishReason.Defeat:
                    return 1;
                case BattleFinishReason.EscapeSuccess:
                    return 2;
                case BattleFinishReason.EscapeFailed:
                    return 3;
                case BattleFinishReason.Surrender:
                    return 4;
                case BattleFinishReason.VictoryByLp:
                    return 5;
                case BattleFinishReason.DefeatByLp:
                    return 6;
                default:
                    return 100;
            }
        }

        private void SortLocationBackgrounds()
        {
            if (locationBackgrounds == null || locationBackgrounds.Count == 0)
                return;

            locationBackgrounds.Sort((a, b) =>
            {
                if (ReferenceEquals(a, b))
                    return 0;
                if (a == null)
                    return 1;
                if (b == null)
                    return -1;

                if (a.anyLocation != b.anyLocation)
                    return a.anyLocation ? 1 : -1;

                string aId = ResolveLocationFallbackId(a);
                string bId = ResolveLocationFallbackId(b);
                return string.Compare(aId, bId, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static string ResolveLocationAssetId(BattleLocationData location)
        {
            if (location == null)
                return string.Empty;

            var id = string.IsNullOrWhiteSpace(location.id) ? string.Empty : location.id.Trim();
            if (!string.IsNullOrWhiteSpace(id) && !string.Equals(id, "location", StringComparison.OrdinalIgnoreCase))
                return id;

            var assetName = location.name;
            if (string.IsNullOrWhiteSpace(assetName))
                return string.Empty;

            return assetName.Trim().ToLowerInvariant().Replace(' ', '_');
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
