using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle.UI
{
    [CreateAssetMenu(
        fileName = "BattleAchievementCatalog",
        menuName = "UDA2/Battle/Achievements/Catalog")]
    public sealed class BattleAchievementCatalogAsset : ScriptableObject
    {
        public enum AchievementMetric
        {
            BattlesFinished = 0,
            BattlesWon = 1,
            BattlesLost = 2,
            EscapesSuccessful = 3,
            TotalMobKills = 4,
            TotalBattleDurationSeconds = 5,
            TotalHpDamageDealtToEnemies = 6,
            TotalHpDamageTakenFromEnemies = 7,
            TotalLpDamageDealtToEnemies = 8,
            TotalLpDamageTakenFromEnemies = 9,
            TotalGoldEarned = 10,
            TotalExpEarned = 11,
        }

        [Serializable]
        public sealed class AchievementDefinition
        {
            [Tooltip("Unique achievement ID, saved in SaveData (e.g. battle_damage_dealt_500).")]
            public string id;

            [Tooltip("Localization key for achievement title.")]
            public string titleLocalizationKey;

            [Tooltip("Localization key for achievement description.")]
            public string descriptionLocalizationKey;

            [Tooltip("Achievement icon.")]
            public Sprite icon;

            [Tooltip("Which cumulative stat is used for unlock check.")]
            public AchievementMetric metric = AchievementMetric.BattlesWon;

            [Tooltip("Required threshold for unlock.")]
            public int threshold = 1;

            [Tooltip("Disabled entries are ignored by unlock checks.")]
            public bool enabled = true;
        }

        [SerializeField] private List<AchievementDefinition> achievements = new List<AchievementDefinition>();

        public IReadOnlyList<AchievementDefinition> Achievements => achievements;

        public bool TryGetById(string id, out AchievementDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(id) && achievements != null)
            {
                for (int i = 0; i < achievements.Count; i++)
                {
                    var candidate = achievements[i];
                    if (candidate == null || string.IsNullOrWhiteSpace(candidate.id))
                        continue;

                    if (string.Equals(candidate.id.Trim(), id.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }
    }
}
