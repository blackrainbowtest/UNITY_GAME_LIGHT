using System;
using System.Collections.Generic;
using Game.Progression;

namespace UDA2.SaveSystem.Guild
{
    [Serializable]
    public class GuildRankRequirement
    {
        public AdventurerRank targetRank = AdventurerRank.E;
        public int requiredGold = 0;
        public int requiredHeroLevel = 1;
        public int requiredCompletedQuests = 0;
        public List<GuildItemAmount> requiredItems = new List<GuildItemAmount>();
    }
}
