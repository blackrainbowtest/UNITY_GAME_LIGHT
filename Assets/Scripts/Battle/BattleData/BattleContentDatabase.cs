using System;
using UnityEngine;

namespace Game.Battle
{
    [CreateAssetMenu(menuName = "Game/Battle/Content Database")]
    public class BattleContentDatabase : ScriptableObject
    {
        [Header("Catalog")]
        [SerializeField] private EnemyData[] enemies;
        [SerializeField] private BattleLocationData[] locations;

        public bool TryGetEnemy(string id, out EnemyData enemy)
        {
            enemy = null;
            if (string.IsNullOrEmpty(id) || enemies == null)
                return false;

            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (e == null)
                    continue;

                if (string.Equals(e.id, id, StringComparison.OrdinalIgnoreCase))
                {
                    enemy = e;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetLocation(string id, out BattleLocationData location)
        {
            location = null;
            if (string.IsNullOrEmpty(id) || locations == null)
                return false;

            for (int i = 0; i < locations.Length; i++)
            {
                var loc = locations[i];
                if (loc == null)
                    continue;

                if (string.Equals(loc.id, id, StringComparison.OrdinalIgnoreCase))
                {
                    location = loc;
                    return true;
                }
            }

            return false;
        }
    }
}
