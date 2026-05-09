using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle.UI
{
    [CreateAssetMenu(
        fileName = "BattleResultOutcomeUiCatalog",
        menuName = "UDA2/Battle/Result Outcome UI Catalog")]
    public sealed class BattleResultOutcomeUiCatalogAsset : ScriptableObject
    {
        [Serializable]
        public sealed class TipVariant
        {
            [Tooltip("Localization key for this tip variant.")]
            public string localizationKey;

            [Tooltip("If enabled, this tip variant will apply custom text color.")]
            public bool useCustomColor;

            [Tooltip("Color for this tip variant when Use Custom Color is enabled.")]
            public Color color = Color.white;
        }

        [Serializable]
        public sealed class OutcomeUiEntry
        {
            public BattleFinishReason reason = BattleFinishReason.Victory;

            [Header("Header")]
            [Tooltip("Optional top separator sprite for this outcome.")]
            public Sprite topSeparator;

            [Tooltip("Localization key for title.")]
            public string titleLocalizationKey;

            [Tooltip("If enabled, title color will be overridden for this outcome.")]
            public bool useCustomTitleColor;

            [Tooltip("Title color when Use Custom Title Color is enabled.")]
            public Color titleColor = Color.white;

            [Header("Tips")]
            [Tooltip("Left panel tip variants. One random variant is selected.")]
            public List<TipVariant> leftTipVariants = new List<TipVariant>();

            [Tooltip("Right panel tip variants. One random variant is selected.")]
            public List<TipVariant> rightTipVariants = new List<TipVariant>();

            [Tooltip("Left panel random tip keys. Used when list has more than one key.")]
            public List<string> leftTipLocalizationKeys = new List<string>();

            [Tooltip("Right panel random tip keys. Used when list has more than one key.")]
            public List<string> rightTipLocalizationKeys = new List<string>();
        }

        [SerializeField] private List<OutcomeUiEntry> outcomes = new List<OutcomeUiEntry>();

        public bool TryGet(BattleFinishReason reason, out OutcomeUiEntry entry)
        {
            if (outcomes != null)
            {
                for (int i = 0; i < outcomes.Count; i++)
                {
                    var candidate = outcomes[i];
                    if (candidate == null)
                        continue;

                    if (candidate.reason == reason)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }
    }
}
