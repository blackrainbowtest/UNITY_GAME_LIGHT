using System;
using Game.Battle;
using Game.Progression;
using UnityEngine;

namespace Game.Dungeon
{
    [CreateAssetMenu(menuName = "UDA2/Dungeon/Location Definition", fileName = "DungeonLocation")]
    public sealed class DungeonLocationDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;

        [Header("Gating")]
        public AdventurerRank requiredRank = AdventurerRank.None;

        [Header("Battle")]
        [Tooltip("Battle scene name to load (default is FightScene).")]
        public string fightSceneName = "FightScene";

        [Tooltip("If enabled, sets BattleExitContext to return to the scene that started this fight.")]
        public bool returnToActiveSceneAfterBattle = true;

        [Header("Encounters")]
        [Tooltip("Pick one pool by weight. Each pool defines a battle background (location) and the enemy table available for that background.")]
        public DungeonEncounterPool[] encounterPools;

        public bool IsAvailableFor(AdventurerRank playerRank)
        {
            return playerRank >= requiredRank;
        }
    }

    [Serializable]
    public sealed class DungeonEncounterPool
    {
        public BattleLocationData battleLocation;
        public EnemySpawnTable enemyTable;

        [Min(0)]
        public int weight = 1;
    }
}
