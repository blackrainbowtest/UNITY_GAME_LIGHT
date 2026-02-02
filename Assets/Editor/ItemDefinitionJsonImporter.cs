//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
// 
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/ItemDefinitionJsonImporter.cs                                                      */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/21 13:22:04 by UDA                                                                    */
/*   Updated: 2026/01/21 13:22:04 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor utility for importing ItemDefinition assets from JSON.
/// 
/// This tool converts external item data into ScriptableObjects
/// and is intentionally isolated from runtime logic.
/// </summary>
public class ItemDefinitionJsonImporter : EditorWindow
{
    private string jsonPath =
        "Assets/GameData/Items/Json/items.json";

    private string iconsPath =
        "Assets/Sprites/Items/";

    private string outputPath =
        "Assets/GameData/Items/Definitions/";

    [MenuItem("Tools/Items/Import Items from JSON")]
    public static void ShowWindow()
    {
        GetWindow<ItemDefinitionJsonImporter>(
            "Item JSON Importer"
        );
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "Import ItemDefinitions from JSON",
            EditorStyles.boldLabel
        );

        jsonPath = EditorGUILayout.TextField(
            "JSON Path",
            jsonPath
        );

        iconsPath = EditorGUILayout.TextField(
            "Icons Path",
            iconsPath
        );

        outputPath = EditorGUILayout.TextField(
            "Output Path",
            outputPath
        );

        if (GUILayout.Button("Import"))
        {
            ImportItems();
        }
    }

    /// <summary>
    /// Main import pipeline.
    /// </summary>
    private void ImportItems()
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError(
                $"ItemDefinitionJsonImporter: JSON file not found at '{jsonPath}'."
            );
            return;
        }

        string json = File.ReadAllText(jsonPath);
        var itemList = JsonHelper.FromJson<ItemJson>(json);

        foreach (var item in itemList)
        {
            CreateItemAsset(item);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"ItemDefinitionJsonImporter: Imported {itemList.Length} items."
        );
    }

    /// <summary>
    /// Creates and initializes a single ItemDefinition asset.
    /// </summary>
    private void CreateItemAsset(ItemJson item)
    {
        string assetPath =
            $"{outputPath}{item.id}.asset";

        var asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<ItemDefinition>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        if (!TryBuildEditorDefinition(item, out var definition, out string error))
        {
            Debug.LogError($"ItemDefinitionJsonImporter: Failed to parse '{item?.id}': {error}");
            return;
        }

        var icon = LoadIcon(definition.IconName);
        if (!asset.EditorApplyDefinition(
            definition.Id,
            definition.Type,
            definition.DisplayNameFallback,
            definition.DisplayNameKey,
            definition.DescriptionKey,
            definition.DescriptionFallback,
            icon,
            definition.Stackable,
            definition.MaxStack,
            definition.EquipSlotId,
            definition.InventorySlotsBonus,
            definition.Effect,
            definition.Value,
            out error))
        {
            Debug.LogError($"ItemDefinitionJsonImporter: Failed to apply '{definition.Id}': {error}");
        }
    }

    private Sprite LoadIcon(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return null;

        var basePath = iconsPath ?? string.Empty;
        if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/"))
            basePath += "/";

        string iconAssetPath =
            $"{basePath}{iconName}.png";

        var icon =
            AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPath);

        if (icon == null)
        {
            Debug.LogWarning(
                $"ItemDefinitionJsonImporter: Icon not found at '{iconAssetPath}'."
            );
        }

        return icon;
    }

    private static bool TryBuildEditorDefinition(
        ItemJson item,
        out ItemEditorDefinition result,
        out string error)
    {
        result = default;
        error = null;

        if (item == null)
        {
            error = "Item JSON is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.id))
        {
            error = "Missing 'id'.";
            return false;
        }

        if (!System.Enum.TryParse(item.type, ignoreCase: true, out ItemType itemType))
        {
            // Allow a more generic external schema.
            if (string.Equals(item.type, "Generic", System.StringComparison.OrdinalIgnoreCase))
                itemType = ItemType.Resource;
            else
            {
                error = $"Unknown item type '{item.type}'.";
                return false;
            }
        }

        ConsumableEffect effect = ConsumableEffect.DoingNothing;
        int value = item.value;

        // Prefer explicit effect/value (legacy), otherwise derive from effects block (new schema).
        if (itemType == ItemType.Consumable)
        {
            // Old JSON may contain null-like values.
            if (!string.IsNullOrWhiteSpace(item.effect)
                && item.effect != "null")
            {
                if (!System.Enum.TryParse(item.effect, ignoreCase: true, out effect))
                {
                    error = $"Unknown consumable effect '{item.effect}'.";
                    return false;
                }
            }
            else if (item.effects != null)
            {
                if (item.effects.hp != 0)
                {
                    effect = ConsumableEffect.HealHP;
                    value = item.effects.hp;
                }
                else if (item.effects.mp != 0)
                {
                    effect = ConsumableEffect.RestoreMana;
                    value = item.effects.mp;
                }
                else if (item.effects.sp != 0)
                {
                    effect = ConsumableEffect.RestoreStamina;
                    value = item.effects.sp;
                }
            }
        }

        // Backwards-compatible field mapping.
        // Legacy JSON used 'displayName' as a key (e.g. item.gold.name).
        // New JSON can store keys in meta.nameKey/meta.descriptionKey.
        var nameKey = (item.meta != null && !string.IsNullOrWhiteSpace(item.meta.nameKey))
            ? item.meta.nameKey
            : (item.displayName ?? string.Empty);

        var descKey = (item.meta != null && !string.IsNullOrWhiteSpace(item.meta.descriptionKey))
            ? item.meta.descriptionKey
            : string.Empty;

        var descFallback = (item.meta != null ? item.meta.description : null) ?? string.Empty;

        var iconName = (item.meta != null && !string.IsNullOrWhiteSpace(item.meta.icon))
            ? item.meta.icon
            : item.icon;

        bool stackable = item.meta != null ? item.meta.stackable : (itemType != ItemType.Equipment);
        int maxStack = item.meta != null && item.meta.maxStack > 0 ? item.meta.maxStack : 99;

        string equipSlotId = item.equipment != null ? (item.equipment.slot ?? string.Empty) : string.Empty;
        int inventorySlotsBonus = 0;
        if (item.equipment != null)
        {
            if (item.equipment.inventorySlotsBonus != 0)
                inventorySlotsBonus = item.equipment.inventorySlotsBonus;
        }
        // Alternate bag capacity location from a more "world"-oriented schema.
        if (inventorySlotsBonus == 0 && item.world != null && item.world.containerSize > 0)
            inventorySlotsBonus = item.world.containerSize;

        // Prefer economy.value if present.
        if (item.economy != null && item.economy.value != 0)
            value = item.economy.value;

        result = new ItemEditorDefinition(
            id: item.id,
            type: itemType,
            displayNameKey: nameKey,
            descriptionKey: descKey,
            descriptionFallback: descFallback,
            iconName: iconName,
            stackable: stackable,
            maxStack: maxStack,
            equipSlotId: equipSlotId,
            inventorySlotsBonus: inventorySlotsBonus,
            effect: effect,
            value: value
        );
        return true;
    }

    private readonly struct ItemEditorDefinition
    {
        public string Id { get; }
        public ItemType Type { get; }
        public string DisplayNameKey { get; }
        public string DescriptionKey { get; }
        public string DescriptionFallback { get; }
        public string DisplayNameFallback { get; }
        public string IconName { get; }
        public bool Stackable { get; }
        public int MaxStack { get; }
        public string EquipSlotId { get; }
        public int InventorySlotsBonus { get; }
        public ConsumableEffect Effect { get; }
        public int Value { get; }

        public ItemEditorDefinition(
            string id,
            ItemType type,
            string displayNameKey,
            string descriptionKey,
            string descriptionFallback,
            string displayNameFallback,
            string iconName,
            bool stackable,
            int maxStack,
            string equipSlotId,
            int inventorySlotsBonus,
            ConsumableEffect effect,
            int value)
        {
            Id = id;
            Type = type;
            DisplayNameKey = displayNameKey ?? string.Empty;
            DescriptionKey = descriptionKey ?? string.Empty;
            DescriptionFallback = descriptionFallback ?? string.Empty;
            DisplayNameFallback = displayNameFallback ?? string.Empty;
            IconName = iconName;
            Stackable = stackable;
            MaxStack = maxStack;
            EquipSlotId = equipSlotId ?? string.Empty;
            InventorySlotsBonus = inventorySlotsBonus;
            Effect = effect;
            Value = value;
        }

        public ItemEditorDefinition(
            string id,
            ItemType type,
            string displayNameKey,
            string descriptionKey,
            string descriptionFallback,
            string iconName,
            bool stackable,
            int maxStack,
            string equipSlotId,
            int inventorySlotsBonus,
            ConsumableEffect effect,
            int value)
            : this(
                id,
                type,
                displayNameKey,
                descriptionKey,
                descriptionFallback,
                displayNameFallback: string.Empty,
                iconName,
                stackable,
                maxStack,
                equipSlotId,
                inventorySlotsBonus,
                effect,
                value)
        {
        }
    }

    #region JSON DTOs

    [System.Serializable]
    public class ItemJson
    {
        public string id;
        public string type;

        // Legacy fields (still supported)
        public string displayName;
        public string icon;
        public string effect;
        public int value;

        // New schema (optional)
        public ItemMetaJson meta;
        public ItemEconomyJson economy;
        public ItemUsageJson usage;
        public ItemEffectsJson effects;
        public ItemEquipmentJson equipment;
        public ItemWorldJson world;
        public string[] flags;
    }

    [System.Serializable]
    public class ItemMetaJson
    {
        public string nameKey;
        public string descriptionKey;
        public string icon;
        public string rarity;
        public bool stackable = true;
        public int maxStack = 99;
        public string description;
    }

    [System.Serializable]
    public class ItemEconomyJson
    {
        public int value;
        public float weight;
    }

    [System.Serializable]
    public class ItemUsageJson
    {
        public string useType;
        public bool consumable;
        public int cooldown;
    }

    [System.Serializable]
    public class ItemEffectsJson
    {
        public int hp;
        public int mp;
        public int sp;
        public int lp;
        public string[] statuses;
    }

    [System.Serializable]
    public class ItemEquipmentJson
    {
        public string slot;

        // For bags
        public int inventorySlotsBonus;

        // Not used by runtime yet; kept for schema compatibility.
        public ItemEquipmentStatsJson stats;
    }

    [System.Serializable]
    public class ItemEquipmentStatsJson
    {
        // Placeholder: JsonUtility does not support dictionaries.
        // Add explicit stat fields here when needed.
        public int hp;
        public int mp;
        public int sp;
        public int lp;
    }

    [System.Serializable]
    public class ItemWorldJson
    {
        public bool canDrop = true;
        public bool canDestroy;

        // Use -1 to represent null (JsonUtility can't read nullable ints).
        public int containerSize = -1;
    }

    #endregion

    #region Json Helper

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string wrapped =
                "{\"array\":" + json + "}";

            Wrapper<T> wrapper =
                JsonUtility.FromJson<Wrapper<T>>(wrapped);

            return wrapper.array;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }

    #endregion
}

/*
#if UNITY_EDITOR
public void EditorApplyDefinition(ItemDefinitionData data)
{
    ...
}
#endif
*/