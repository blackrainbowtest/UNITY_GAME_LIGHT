using System;
using System.Collections.Generic;
using Game.Progression;
using UnityEngine;

namespace UDA2.SaveSystem.Guild
{
    [Serializable]
    public sealed class GuildRankVisualEntry
    {
        public AdventurerRank rank = AdventurerRank.None;
        public Color textColor = Color.white;
        public Sprite icon;
    }

    [CreateAssetMenu(fileName = "GuildRankVisualConfig", menuName = "UDA2/Guild/Rank Visual Config")]
    public sealed class GuildRankVisualConfigAsset : ScriptableObject
    {
        public List<GuildRankVisualEntry> entries = new List<GuildRankVisualEntry>();

        public bool TryGet(AdventurerRank rank, out GuildRankVisualEntry entry)
        {
            entry = null;
            if (entries == null)
                return false;

            for (var i = 0; i < entries.Count; i++)
            {
                var candidate = entries[i];
                if (candidate == null)
                    continue;

                if (candidate.rank == rank)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
