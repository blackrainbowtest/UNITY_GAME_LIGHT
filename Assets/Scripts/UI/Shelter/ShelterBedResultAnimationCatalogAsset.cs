using System;
using System.Collections.Generic;
using UnityEngine;

namespace UDA2.UI.Shelter
{
    [CreateAssetMenu(
        fileName = "ShelterBedResultAnimationCatalog",
        menuName = "UDA2/Shelter/Bed Result Animation Catalog")]
    public sealed class ShelterBedResultAnimationCatalogAsset : ScriptableObject
    {
        public enum ConditionType
        {
            None = 0,
            HasStatusEffect = 1,
            MissingStatusEffect = 2,
            SpPercentAtOrAbove = 3,
            SpPercentAtOrBelow = 4,
            HpPercentAtOrAbove = 5,
            HpPercentAtOrBelow = 6,
            MpPercentAtOrAbove = 7,
            MpPercentAtOrBelow = 8,
            LpPercentAtOrAbove = 9,
            LpPercentAtOrBelow = 10,
            PlayerLevelAtLeast = 11,
            PlayerLevelAtMost = 12
        }

        [Serializable]
        public sealed class Condition
        {
            public ConditionType type = ConditionType.None;
            public string stringValue;
            public float numberValue;
        }

        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Action id from the bed window (rest/sleep/relax/relax2)")]
            public string actionId;

            [Tooltip("Optional grouping tag (e.g. default, tired, buffed).")]
            public string categoryId = "default";

            [Tooltip("Higher priority wins when multiple entries match.")]
            public int priority;

            [Tooltip("Preferred source. Ordered list used by Prev/Next in result modal.")]
            public IdleAnimation[] animations;

            [Tooltip("Legacy fallback. Used only when Animations is empty.")]
            public string[] animationIds;

            [Tooltip("All conditions must pass for this entry.")]
            public List<Condition> conditions = new List<Condition>();
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<string> ResolveAnimationIds(string actionId, SaveData save)
        {
            if (string.IsNullOrWhiteSpace(actionId) || entries == null || entries.Count == 0)
                return Array.Empty<string>();

            string normalizedAction = actionId.Trim();
            Entry best = null;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                    continue;

                if (!string.Equals(entry.actionId, normalizedAction, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!MatchesAll(entry.conditions, save))
                    continue;

                if (!HasAnyAnimation(entry))
                    continue;

                if (best == null || entry.priority > best.priority)
                    best = entry;
            }

            if (best == null)
                return Array.Empty<string>();

            var result = new List<string>();

            if (best.animations != null && best.animations.Length > 0)
            {
                for (int i = 0; i < best.animations.Length; i++)
                {
                    var anim = best.animations[i];
                    if (anim == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(anim.Id))
                        result.Add(anim.Id.Trim());
                }
            }

            if (result.Count == 0 && best.animationIds != null)
            {
                for (int i = 0; i < best.animationIds.Length; i++)
                {
                    string id = best.animationIds[i];
                    if (!string.IsNullOrWhiteSpace(id))
                        result.Add(id.Trim());
                }
            }

            return result;
        }

        private static bool HasAnyAnimation(Entry entry)
        {
            if (entry == null)
                return false;

            if (entry.animations != null)
            {
                for (int i = 0; i < entry.animations.Length; i++)
                {
                    var anim = entry.animations[i];
                    if (anim != null && !string.IsNullOrWhiteSpace(anim.Id))
                        return true;
                }
            }

            var animationIds = entry.animationIds;
            if (animationIds == null || animationIds.Length == 0)
                return false;

            for (int i = 0; i < animationIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(animationIds[i]))
                    return true;
            }

            return false;
        }

        private static bool MatchesAll(List<Condition> conditions, SaveData save)
        {
            if (conditions == null || conditions.Count == 0)
                return true;

            for (int i = 0; i < conditions.Count; i++)
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
                case ConditionType.SpPercentAtOrAbove:
                    return GetPercent(GetSp(save), GetSpMax(save)) >= condition.numberValue;
                case ConditionType.SpPercentAtOrBelow:
                    return GetPercent(GetSp(save), GetSpMax(save)) <= condition.numberValue;
                case ConditionType.HpPercentAtOrAbove:
                    return GetPercent(GetHp(save), GetHpMax(save)) >= condition.numberValue;
                case ConditionType.HpPercentAtOrBelow:
                    return GetPercent(GetHp(save), GetHpMax(save)) <= condition.numberValue;
                case ConditionType.MpPercentAtOrAbove:
                    return GetPercent(GetMp(save), GetMpMax(save)) >= condition.numberValue;
                case ConditionType.MpPercentAtOrBelow:
                    return GetPercent(GetMp(save), GetMpMax(save)) <= condition.numberValue;
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
            for (int i = 0; i < effects.Count; i++)
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
