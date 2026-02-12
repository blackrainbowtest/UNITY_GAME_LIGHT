using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocationUpgradeTable", menuName = "UDA2/Location/Upgrade Table")]
public sealed class LocationUpgradeTableAsset : ScriptableObject
{
    [SerializeField] private List<StructureUpgradeTrack> tracks = new List<StructureUpgradeTrack>();

    public IReadOnlyList<StructureUpgradeTrack> Tracks => tracks;

    public bool TryGetUpgradeLevel(LocationStructureType structureType, int targetLevel, out UpgradeLevelConfig levelConfig)
    {
        levelConfig = null;

        if (tracks == null || tracks.Count == 0)
            return false;

        for (int i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            if (track == null || track.structureType != structureType || track.levels == null)
                continue;

            for (int j = 0; j < track.levels.Count; j++)
            {
                var candidate = track.levels[j];
                if (candidate == null)
                    continue;

                if (candidate.level == targetLevel)
                {
                    levelConfig = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    [Serializable]
    public sealed class StructureUpgradeTrack
    {
        public LocationStructureType structureType;
        public List<UpgradeLevelConfig> levels = new List<UpgradeLevelConfig>();
    }

    [Serializable]
    public sealed class UpgradeLevelConfig
    {
        [Min(1)]
        public int level = 1;

        [Tooltip("Resources required to upgrade TO this level.")]
        public List<ResourceCost> requiredResources = new List<ResourceCost>();
    }

    [Serializable]
    public sealed class ResourceCost
    {
        [Tooltip("Item ID from your item database (for example: wood, iron_ingot).")]
        public string itemId;

        [Min(1)]
        public int amount = 1;
    }
}
