using System.Collections.Generic;
using UnityEngine;

namespace UDA2.SaveSystem.Guild
{
    [CreateAssetMenu(fileName = "GuildQuestBoardConfig", menuName = "UDA2/Guild/Quest Board Config")]
    public sealed class GuildQuestBoardConfigAsset : ScriptableObject
    {
        [Min(1)] public int questsPerDay = 3;
        [Range(0, 1439)] public int refreshMinuteOfDay = 12 * 60;
        public List<GuildQuestDefinitionAsset> questPool = new List<GuildQuestDefinitionAsset>();
    }
}
