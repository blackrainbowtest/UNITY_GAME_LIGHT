using System;
using System.Collections.Generic;
using UnityEngine;

namespace UDA2.UI.Shelter
{
    [CreateAssetMenu(
        fileName = "ShelterBedRecoveryConfig",
        menuName = "UDA2/UI/Shelter Bed Recovery Config")]
    public sealed class ShelterBedRecoveryConfigAsset : ScriptableObject
    {
        [Serializable]
        public struct ActionRecoveryConfig
        {
            [Tooltip("Action id from window buttons, e.g. rest/sleep/relax/relax2")]
            public string actionId;

            [Tooltip("Maximum allowed duration in minutes for this action.")]
            public int maxDurationMinutes;

            [Tooltip("Stat delta per 15-minute step.")]
            public int hpPerStep;
            [Tooltip("Stat delta per 15-minute step.")]
            public int mpPerStep;
            [Tooltip("Stat delta per 15-minute step.")]
            public int lpPerStep;
            [Tooltip("Stat delta per 15-minute step.")]
            public int spPerStep;
        }

        [SerializeField] private List<ActionRecoveryConfig> actions = new List<ActionRecoveryConfig>();

        public bool TryGetRule(string actionId, out ActionRecoveryConfig config)
        {
            if (actions != null)
            {
                string normalizedActionId = string.IsNullOrWhiteSpace(actionId)
                    ? string.Empty
                    : actionId.Trim().ToLowerInvariant();

                for (int i = 0; i < actions.Count; i++)
                {
                    var candidate = actions[i];
                    if (string.IsNullOrWhiteSpace(candidate.actionId))
                        continue;

                    if (string.Equals(candidate.actionId.Trim(), normalizedActionId, StringComparison.OrdinalIgnoreCase))
                    {
                        config = candidate;
                        return true;
                    }
                }
            }

            config = default;
            return false;
        }
    }
}
