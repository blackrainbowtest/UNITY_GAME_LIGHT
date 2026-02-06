using System;
using UnityEngine;

namespace UDA2.UI.Game
{
    [CreateAssetMenu(menuName = "UDA2/Profile/Outfit Sprite Config", fileName = "ProfileOutfitSpriteConfig")]
    public sealed class ProfileOutfitSpriteConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string outfitId;
            public Sprite sprite;
        }

        [Header("Defaults")]
        [SerializeField] private Sprite defaultSprite;

        [Header("Mapping")]
        [SerializeField] private Entry[] entries;

        public Sprite Resolve(string outfitId)
        {
            if (!string.IsNullOrEmpty(outfitId) && entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry == null)
                        continue;

                    if (!string.IsNullOrEmpty(entry.outfitId)
                        && string.Equals(entry.outfitId, outfitId, StringComparison.OrdinalIgnoreCase)
                        && entry.sprite != null)
                    {
                        return entry.sprite;
                    }
                }
            }

            return defaultSprite;
        }
    }
}
