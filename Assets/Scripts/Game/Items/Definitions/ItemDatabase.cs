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
        if (cache == null)
        {
            cache = new Dictionary<string, ItemDefinition>();
            foreach (var item in items)
                cache[item.Id] = item;
        }

        return cache.TryGetValue(id, out var result) ? result : null;
    }

#if UNITY_EDITOR
    public void EditorSetItems(ItemDefinition[] definitions)
    {
        items = new List<ItemDefinition>(definitions ?? Array.Empty<ItemDefinition>());
        cache = null;
    }
#endif
}
