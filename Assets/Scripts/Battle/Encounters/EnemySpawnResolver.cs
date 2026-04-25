using UnityEngine;
using Logger = UDA2.Logging.Logger;

namespace Game.Battle
{
    public readonly struct EnemySpawnConstraints
    {
        public int MinLevel { get; }
        public int MaxLevel { get; }
        public int MinRankTier { get; }
        public int MaxRankTier { get; }

        public EnemySpawnConstraints(int minLevel, int maxLevel, int minRankTier, int maxRankTier)
        {
            MinLevel = Mathf.Max(1, minLevel);
            MaxLevel = Mathf.Max(MinLevel, maxLevel);
            MinRankTier = Mathf.Max(0, minRankTier);
            MaxRankTier = Mathf.Max(MinRankTier, maxRankTier);
        }

        public static EnemySpawnConstraints Default => new EnemySpawnConstraints(1, 999, 0, 999);
    }

    public class EnemySpawnResolver
    {
        public EnemyData Resolve(EnemySpawnTable table)
        {
            if (!Resolve(table, EnemySpawnConstraints.Default, out var enemy, out _, out _))
            {
                return null;
            }

            return enemy;
        }

        public bool Resolve(
            EnemySpawnTable table,
            EnemySpawnConstraints constraints,
            out EnemyData resolvedEnemy,
            out int resolvedEnemyLevel,
            out int resolvedEnemyRankTier)
        {
            resolvedEnemy = null;
            resolvedEnemyLevel = 1;
            resolvedEnemyRankTier = 0;

            if (table == null)
            {
                Debug.LogError("EnemySpawnResolver: table is null");
                return false;
            }

            var entries = table.Entries;
            if (entries == null || entries.Count == 0)
            {
                Debug.LogError("EnemySpawnResolver: table has no entries");
                return false;
            }

            int totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.enemy == null || e.weight <= 0)
                    continue;

                if (!TryGetIntersectedRanges(e.enemy, constraints, out _, out _, out _, out _))
                    continue;

                totalWeight += e.weight;
            }

            if (totalWeight <= 0)
            {
                Debug.LogError("EnemySpawnResolver: totalWeight is 0 (all weights are 0, entries invalid, or filtered by constraints)");
                return false;
            }

            int roll = Random.Range(0, totalWeight);
            int sum = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.enemy == null || e.weight <= 0)
                    continue;

                if (!TryGetIntersectedRanges(e.enemy, constraints, out _, out _, out _, out _))
                    continue;

                sum += e.weight;
                if (roll < sum)
                {
                    resolvedEnemy = e.enemy;
                    break;
                }
            }

            if (resolvedEnemy == null)
            {
                Debug.LogError("EnemySpawnResolver: failed to resolve enemy (unexpected)");
                return false;
            }

            if (!TryGetIntersectedRanges(resolvedEnemy, constraints, out var minLevel, out var maxLevel, out var minRank, out var maxRank))
            {
                Debug.LogError("EnemySpawnResolver: resolved enemy has no valid range intersection with constraints");
                return false;
            }

            resolvedEnemyLevel = Random.Range(minLevel, maxLevel + 1);
            resolvedEnemyRankTier = Random.Range(minRank, maxRank + 1);

            Logger.LogInfo(
                $"[Battle][SpawnResolver] table={table.name}, selected enemy name={resolvedEnemy.enemyName}, id={resolvedEnemy.id}, " +
                $"level={resolvedEnemyLevel}, rankTier={resolvedEnemyRankTier}, " +
                $"allowedActions={(resolvedEnemy.allowedActions != null && resolvedEnemy.allowedActions.Length > 0 ? string.Join(", ", resolvedEnemy.allowedActions) : "<default fallback>")}",
                UDA2.Logging.LogChannel.AI);

            return true;
        }

        private static bool TryGetIntersectedRanges(
            EnemyData enemy,
            EnemySpawnConstraints constraints,
            out int minLevel,
            out int maxLevel,
            out int minRank,
            out int maxRank)
        {
            minLevel = 1;
            maxLevel = 1;
            minRank = 0;
            maxRank = 0;

            if (enemy == null)
                return false;

            var enemyMinLevel = Mathf.Max(1, enemy.minSpawnLevel);
            var enemyMaxLevel = Mathf.Max(enemyMinLevel, enemy.maxSpawnLevel);
            var enemyMinRank = Mathf.Max(0, enemy.minSpawnRankTier);
            var enemyMaxRank = Mathf.Max(enemyMinRank, enemy.maxSpawnRankTier);

            minLevel = Mathf.Max(enemyMinLevel, constraints.MinLevel);
            maxLevel = Mathf.Min(enemyMaxLevel, constraints.MaxLevel);
            minRank = Mathf.Max(enemyMinRank, constraints.MinRankTier);
            maxRank = Mathf.Min(enemyMaxRank, constraints.MaxRankTier);

            return minLevel <= maxLevel && minRank <= maxRank;
        }
    }
}
