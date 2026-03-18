using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle.Statuses
{
    [CreateAssetMenu(menuName = "Game/Battle/Statuses/Status Catalog", fileName = "BattleStatusCatalog")]
    public sealed class BattleStatusCatalog : ScriptableObject
    {
        public enum ResourceStat
        {
            Hp = 0,
            Mp = 1,
            Sp = 2,
            Lp = 3
        }

        public enum EffectOperation
        {
            Add = 0,
            Reduce = 1
        }

        [Serializable]
        public struct ResourceEffect
        {
            [Tooltip("Resource affected by this status effect.")]
            public ResourceStat stat;

            [Tooltip("Add or reduce the resource value.")]
            public EffectOperation operation;

            [Min(0)]
            [Tooltip("Magnitude per end-of-round tick.")]
            public int amount;

            public int GetSignedDelta()
            {
                if (amount <= 0)
                    return 0;

                return operation == EffectOperation.Reduce ? -amount : amount;
            }
        }

        [Serializable]
        public struct StatusDefinition
        {
            [Tooltip("Stable gameplay status id.")]
            public StatusEffectId id;

            [Header("Presentation")]
            public Sprite icon;
            [Tooltip("Localization key for status title.")]
            public string titleLocalizationKey;
            [Tooltip("Localization key for status description.")]
            public string descriptionLocalizationKey;

            [Header("Gameplay")]
            [Tooltip("Effects applied each end-of-round while status is active.")]
            public ResourceEffect[] effects;
        }

        [SerializeField] private StatusDefinition[] statuses = Array.Empty<StatusDefinition>();

        private Dictionary<StatusEffectId, int> indexById;

        private void OnEnable()
        {
            RebuildIndex();
        }

        private void OnValidate()
        {
            RebuildIndex();
        }

        public bool TryGet(StatusEffectId id, out StatusDefinition definition)
        {
            if (indexById == null)
                RebuildIndex();

            if (indexById != null && indexById.TryGetValue(id, out int index)
                && statuses != null && index >= 0 && index < statuses.Length)
            {
                definition = statuses[index];
                return true;
            }

            definition = default;
            return false;
        }

        private void RebuildIndex()
        {
            indexById = new Dictionary<StatusEffectId, int>();

            if (statuses == null)
                return;

            for (int i = 0; i < statuses.Length; i++)
                indexById[statuses[i].id] = i;
        }
    }
}
