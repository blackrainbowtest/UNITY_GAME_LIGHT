using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle
{
    [CreateAssetMenu(menuName = "Game/Battle/Enemy Spawn Table")]
    public class EnemySpawnTable : ScriptableObject
    {
        [SerializeField] private List<EnemySpawnEntry> entries = new List<EnemySpawnEntry>();

        public IReadOnlyList<EnemySpawnEntry> Entries => entries;
    }

    [System.Serializable]
    public class EnemySpawnEntry
    {
        public EnemyData enemy;
        [Min(0)] public int weight = 1;
    }
}
