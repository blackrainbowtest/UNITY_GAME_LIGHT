using System;
using System.Collections.Generic;
using Game.Progression;

namespace UDA2.SaveSystem.Guild
{
    [Serializable]
    public sealed class GuildItemRequirementProgress
    {
        public string itemId;
        public int required;
        public int inventoryOwned;
        public int storageOwned;
        public int totalOwned;
        public bool isMet;
    }

    [Serializable]
    public sealed class GuildRankUpViewData
    {
        public AdventurerRank currentRank;
        public AdventurerRank targetRank;

        public int requiredGold;
        public int currentGold;

        public int requiredHeroLevel;
        public int currentHeroLevel;

        public int requiredCompletedQuests;
        public int currentCompletedQuests;

        public bool canRankUpNow;

        public List<GuildItemRequirementProgress> requiredItems = new List<GuildItemRequirementProgress>();
    }
}
