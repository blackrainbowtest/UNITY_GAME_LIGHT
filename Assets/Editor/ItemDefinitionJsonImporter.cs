  ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
 / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
 \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
  ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
 |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 

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
        "Assets/Game/Items/Json/items.json";

    private string iconsPath =
        "Assets/Game/Items/Icons/";

    private string outputPath =
        "Assets/Game/Items/Definitions/";

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
        var asset =
            ScriptableObject.CreateInstance<ItemDefinition>();

        // TODO:
        // Replace field-by-field SerializedObject mutation with a single
        // editor-only initialization method on ItemDefinition
        // (e.g. EditorApplyDefinition / EditorInitialize).
        // Current approach is temporary and relies on internal field names.

        ApplyFieldsViaSerializedObject(asset, item);

        string assetPath =
            $"{outputPath}{item.id}.asset";

        AssetDatabase.CreateAsset(asset, assetPath);
    }

    /// <summary>
    /// Applies JSON data to ItemDefinition using SerializedObject.
    /// 
    /// WARNING:
    /// This method relies on internal field names and bypasses
    /// ItemDefinition's domain API. This is editor-only technical debt.
    /// </summary>
    private void ApplyFieldsViaSerializedObject(
        ItemDefinition asset,
        ItemJson item)
    {
        var so = new SerializedObject(asset);

        SetProperty(so, "id", item.id);
        SetProperty(
            so,
            "type",
            (ItemType)System.Enum.Parse(
                typeof(ItemType),
                item.type
            )
        );

        SetProperty(so, "displayName", item.displayName);

        if (item.type == "Consumable")
        {
            ConsumableEffect effectValue =
                ConsumableEffect.HealHP;

            if (!string.IsNullOrEmpty(item.effect)
                && item.effect != "null"
                && System.Enum.TryParse(
                    item.effect,
                    out ConsumableEffect parsedEffect))
            {
                effectValue = parsedEffect;
            }

            SetProperty(so, "effect", effectValue);
        }

        SetProperty(so, "value", item.value);

        var icon = LoadIcon(item.icon);
        SetProperty(so, "icon", icon);

        so.ApplyModifiedProperties();
    }

    private static Sprite LoadIcon(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return null;

        string iconAssetPath =
            $"Assets/Game/Items/Icons/{iconName}.png";

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

    private static void SetProperty(
        SerializedObject so,
        string fieldName,
        object value)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
            return;

        switch (prop.propertyType)
        {
            case SerializedPropertyType.String:
                prop.stringValue = value as string;
                break;

            case SerializedPropertyType.Integer:
                prop.intValue = (int)value;
                break;

            case SerializedPropertyType.Enum:
                prop.enumValueIndex = (int)value;
                break;

            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = value as Object;
                break;
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