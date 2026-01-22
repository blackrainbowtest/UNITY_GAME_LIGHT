using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle.Visual
{
    [CreateAssetMenu(menuName = "Game/Battle/Visuals/Character Visual Profile")]
    public sealed class CharacterVisualProfile : ScriptableObject
    {
        [SerializeField] private List<OutfitVisuals> outfits = new List<OutfitVisuals>();

        public IReadOnlyList<OutfitVisuals> Outfits => outfits;

        public OutfitVisuals ResolveOutfit(string outfitId, string fallbackOutfitId = "outfit_01")
        {
            if (outfits == null || outfits.Count == 0)
                return null;

            var wanted = string.IsNullOrEmpty(outfitId) ? fallbackOutfitId : outfitId;

            // First pass: exact match
            for (int i = 0; i < outfits.Count; i++)
            {
                var o = outfits[i];
                if (o == null) continue;
                if (string.Equals(o.outfitId, wanted, StringComparison.OrdinalIgnoreCase))
                    return o;
            }

            // Fallback pass
            if (!string.Equals(wanted, fallbackOutfitId, StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < outfits.Count; i++)
                {
                    var o = outfits[i];
                    if (o == null) continue;
                    if (string.Equals(o.outfitId, fallbackOutfitId, StringComparison.OrdinalIgnoreCase))
                        return o;
                }
            }

            // Last resort: first non-null
            for (int i = 0; i < outfits.Count; i++)
            {
                if (outfits[i] != null)
                    return outfits[i];
            }

            return null;
        }
    }
}
