using System.Collections.Generic;
using Game.Progression;
using UnityEngine;

namespace UDA2.SaveSystem.Guild
{
    [System.Serializable]
    public sealed class GuildQuestRankCountRule
    {
        public AdventurerRank questRequiredRank = AdventurerRank.None;
        [Min(0)] public int count = 0;
    }

    [System.Serializable]
    public sealed class GuildQuestSpawnRuleByPlayerRank
    {
        public AdventurerRank playerRank = AdventurerRank.None;
        public List<GuildQuestRankCountRule> rankCounts = new List<GuildQuestRankCountRule>();
    }

    [CreateAssetMenu(fileName = "GuildQuestBoardConfig", menuName = "UDA2/Guild/Quest Board Config")]
    public sealed class GuildQuestBoardConfigAsset : ScriptableObject
    {
        [Min(1)] public int questsPerDay = 3;
        [Range(0, 1439)] public int refreshMinuteOfDay = 12 * 60;
        public List<GuildQuestDefinitionAsset> questPool = new List<GuildQuestDefinitionAsset>();

        [Header("Optional Spawn Rules By Player Rank")]
        [Tooltip("If rule exists for current player rank, board uses rankCounts total instead of questsPerDay. If empty, questsPerDay is used.")]
        public List<GuildQuestSpawnRuleByPlayerRank> spawnRulesByPlayerRank = new List<GuildQuestSpawnRuleByPlayerRank>();

        public bool TryGetRuleForPlayerRank(AdventurerRank playerRank, out GuildQuestSpawnRuleByPlayerRank rule)
        {
            rule = null;
            if (spawnRulesByPlayerRank == null || spawnRulesByPlayerRank.Count == 0)
                return false;

            for (var i = 0; i < spawnRulesByPlayerRank.Count; i++)
            {
                var r = spawnRulesByPlayerRank[i];
                if (r != null && r.playerRank == playerRank)
                {
                    rule = r;
                    return true;
                }
            }

            if (playerRank == AdventurerRank.None)
                return false;

            for (var i = 0; i < spawnRulesByPlayerRank.Count; i++)
            {
                var r = spawnRulesByPlayerRank[i];
                if (r != null && r.playerRank == AdventurerRank.None)
                {
                    rule = r;
                    return true;
                }
            }

            return false;
        }
    }
}
