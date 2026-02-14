using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocationInteractionConfig", menuName = "UDA2/Location/Interaction Config")]
public sealed class LocationInteractionConfigAsset : ScriptableObject
{
    [SerializeField] private LocationStructureType structureType = LocationStructureType.Bed;
    [SerializeField] private bool isUpgradable = true;
    [SerializeField] private List<ActionConfig> actions = new List<ActionConfig>();

    public LocationStructureType StructureType => structureType;
    public bool IsUpgradable => isUpgradable;
    public IReadOnlyList<ActionConfig> Actions => actions;

    public bool IsActionAvailable(string actionId, int currentLevel)
    {
        if (string.IsNullOrWhiteSpace(actionId) || actions == null)
            return false;

        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            if (action == null || !action.isEnabled)
                continue;

            if (!string.Equals(action.actionId, actionId, StringComparison.OrdinalIgnoreCase))
                continue;

            return currentLevel >= action.minRequiredLevel;
        }

        return false;
    }

    [Serializable]
    public sealed class ActionConfig
    {
        [Tooltip("Stable action id. Example: rest, sleep, relax, relax2")]
        public string actionId;

        [Tooltip("Optional localization key for button label.")]
        public string titleKey;

        [Min(0)]
        [Tooltip("Minimal structure level required to unlock this action.")]
        public int minRequiredLevel = 0;

        [Min(15)]
        [Tooltip("Maximum duration in minutes for this action.")]
        public int maxDurationMinutes = 24 * 60;

        public bool isEnabled = true;
    }
}
