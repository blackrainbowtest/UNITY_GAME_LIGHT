using System;
using UnityEngine;

namespace UDA2.SaveSystem.Guild
{
    [Serializable]
    public class GuildMobKillAmount
    {
        /// <summary>
        /// Preferred way: pick enemy asset from object field (for example EnemyData).
        /// </summary>
        public UnityEngine.Object enemy;

        /// <summary>
        /// Legacy fallback: explicit enemy id. Used when enemy asset is not assigned.
        /// </summary>
        public string enemyId;

        public int amount = 1;
    }
}
