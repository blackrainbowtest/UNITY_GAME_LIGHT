using System.Collections.Generic;
using Game.Progression;
using UnityEngine;

namespace UDA2.SaveSystem.Guild
{
    [CreateAssetMenu(fileName = "GuildQuest", menuName = "UDA2/Guild/Quest Definition")]
    public sealed class GuildQuestDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        public string questId;
        public string title;
        [TextArea] public string description;

        [Header("Availability")]
        public AdventurerRank requiredRank = AdventurerRank.None;
        public int requiredHeroLevel = 1;

        [Header("Turn-in Requirements")]
        public int requiredGold = 0;
        public List<GuildItemAmount> requiredItems = new List<GuildItemAmount>();

        [Header("Rewards")]
        public int rewardGold = 0;
        public int rewardExp = 0;
        public List<GuildItemAmount> rewardItems = new List<GuildItemAmount>();
    }
}
