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
                definition.DisplayName,
                icon,
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
            error = $"Unknown item type '{item.type}'.";
            return false;
        }

        ConsumableEffect effect = ConsumableEffect.DoingNothing;
        if (itemType == ItemType.Consumable)
        {
            // Old JSON may contain null-like values.
            if (!string.IsNullOrWhiteSpace(item.effect)
                && item.effect != "null"
                && !System.Enum.TryParse(item.effect, ignoreCase: true, out effect))
            {
                error = $"Unknown consumable effect '{item.effect}'.";
                return false;
            }
        }

        result = new ItemEditorDefinition(
            id: item.id,
            type: itemType,
            displayName: item.displayName,
            iconName: item.icon,
            effect: effect,
            value: item.value
        );
        return true;
    }

    private readonly struct ItemEditorDefinition
    {
        public string Id { get; }
        public ItemType Type { get; }
        public string DisplayName { get; }
        public string IconName { get; }
        public ConsumableEffect Effect { get; }
        public int Value { get; }

        public ItemEditorDefinition(
            string id,
            ItemType type,
            string displayName,
            string iconName,
            ConsumableEffect effect,
            int value)
        {
            Id = id;
            Type = type;
            DisplayName = displayName;
            IconName = iconName;
            Effect = effect;
            Value = value;
        }
    }

    #region JSON DTOs

    [System.Serializable]
    public class ItemJson
    {
        public string id;
        public string type;
        public string displayName;
        public string icon;
        public string effect;
        public int value;
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