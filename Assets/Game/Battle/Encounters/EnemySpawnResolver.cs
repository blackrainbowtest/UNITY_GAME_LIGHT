using UnityEngine;

namespace Game.Battle
{
    public class EnemySpawnResolver
    {
        public EnemyData Resolve(EnemySpawnTable table)
        {
            if (table == null)
            {
                Debug.LogError("EnemySpawnResolver: table is null");
                return null;
            }

            var entries = table.Entries;
            if (entries == null || entries.Count == 0)
            {
                Debug.LogError("EnemySpawnResolver: table has no entries");
                return null;
            }

            int totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.enemy == null || e.weight <= 0)
                    continue;

                totalWeight += e.weight;
            }

            if (totalWeight <= 0)
            {
                Debug.LogError("EnemySpawnResolver: totalWeight is 0 (all weights are 0 or entries invalid)");
                return null;
            }

            int roll = Random.Range(0, totalWeight);
            int sum = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.enemy == null || e.weight <= 0)
                    continue;

                sum += e.weight;
                if (roll < sum)
                    return e.enemy;
            }

            Debug.LogError("EnemySpawnResolver: failed to resolve enemy (unexpected)");
            return null;
        }
    }
}
