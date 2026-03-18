using System.Collections.Generic;
using System.Linq;
using Game.Progression;
using UnityEngine;

namespace UDA2.SaveSystem.Guild
{
    [CreateAssetMenu(fileName = "GuildRankProgressionConfig", menuName = "UDA2/Guild/Rank Progression Config")]
    public sealed class GuildRankProgressionConfigAsset : ScriptableObject
    {
        public List<GuildRankRequirement> requirements = new List<GuildRankRequirement>();

        public bool TryGetNextRequirement(AdventurerRank currentRank, out GuildRankRequirement requirement)
        {
            requirement = null;
            if (requirements == null || requirements.Count == 0)
                return false;

            requirement = requirements
                .Where(r => r != null && r.targetRank > currentRank)
                .OrderBy(r => r.targetRank)
                .FirstOrDefault();

            return requirement != null;
        }
    }
}
