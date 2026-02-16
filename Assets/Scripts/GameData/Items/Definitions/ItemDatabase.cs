using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> items;

    private Dictionary<string, ItemDefinition> cache;

    public ItemDefinition GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (cache == null)
        {
            cache = new Dictionary<string, ItemDefinition>(System.StringComparer.OrdinalIgnoreCase);

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(item.Id))
                        continue;

                    cache[item.Id.Trim()] = item;
                }
            }
        }

        return cache.TryGetValue(id.Trim(), out var result) ? result : null;
    }

#if UNITY_EDITOR
    public void EditorSetItems(ItemDefinition[] definitions)
    {
        items = new List<ItemDefinition>(definitions ?? Array.Empty<ItemDefinition>());
        cache = null;
    }
#endif
}
