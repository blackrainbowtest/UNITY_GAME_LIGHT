using System;
using System.Collections.Generic;
using UnityEngine;

namespace UDA2.SaveSystem.Guild
{
    public enum GuildQuestObjectiveType
    {
        Gold = 0,
        Item = 1,
        MobKill = 2
    }

    [Serializable]
    public sealed class GuildQuestTurnInObjectiveProgress
    {
        public GuildQuestObjectiveType type;
        public string objectiveId;
        public string displayName;
        public int current;
        public int required;
        public UnityEngine.Object sourceObject;

        public bool IsMet => current >= required;
    }

    [Serializable]
    public sealed class GuildQuestTurnInProgressData
    {
        public string questId;
        public bool isTaken;
        public bool canSubmit;
        public List<GuildQuestTurnInObjectiveProgress> objectives = new List<GuildQuestTurnInObjectiveProgress>();
    }
}
