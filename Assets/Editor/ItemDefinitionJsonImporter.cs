using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ItemDefinitionJsonImporter : EditorWindow
{
    private string jsonPath = "Assets/Game/Items/Json/items.json";
    private string iconsPath = "Assets/Game/Items/Icons/";
    private string outputPath = "Assets/Game/Items/Definitions/";

    [MenuItem("Tools/Items/Import Items from JSON")]
    public static void ShowWindow()
    {
        GetWindow<ItemDefinitionJsonImporter>("Item JSON Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Import ItemDefinitions from JSON", EditorStyles.boldLabel);
        jsonPath = EditorGUILayout.TextField("JSON Path", jsonPath);
        iconsPath = EditorGUILayout.TextField("Icons Path", iconsPath);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        if (GUILayout.Button("Import"))
        {
            ImportItems();
        }
    }

    private void ImportItems()
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"JSON file not found: {jsonPath}");
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
        Debug.Log($"Imported {itemList.Length} items from JSON.");
    }

    private void CreateItemAsset(ItemJson item)
    {
        var asset = ScriptableObject.CreateInstance<ItemDefinition>();
        SetField(asset, "id", item.id);
        SetField(asset, "type", (ItemType)System.Enum.Parse(typeof(ItemType), item.type));
        SetField(asset, "displayName", item.displayName);
        // Не устанавливать effect для Currency
        if (item.type == "Consumable")
        {
            ConsumableEffect effectValue = ConsumableEffect.HealHP;
            if (!string.IsNullOrEmpty(item.effect) && item.effect != "null" && System.Enum.TryParse(item.effect, out ConsumableEffect parsedEffect))
                effectValue = parsedEffect;
            SetField(asset, "effect", effectValue);
        }
        SetField(asset, "value", item.value);
        // Find sprite by name
        string iconAssetPath = iconsPath + item.icon + ".png";
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPath);
        if (icon == null)
        {
            Debug.LogWarning($"Icon not found: {iconAssetPath}");
        }
        SetField(asset, "icon", icon);
        string assetPath = outputPath + item.id + ".asset";
        AssetDatabase.CreateAsset(asset, assetPath);
    }

    private void SetField(ItemDefinition asset, string fieldName, object value)
    {
        var so = new SerializedObject(asset);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            if (prop.propertyType == SerializedPropertyType.String)
                prop.stringValue = value as string;
            else if (prop.propertyType == SerializedPropertyType.Integer)
                prop.intValue = (int)value;
            else if (prop.propertyType == SerializedPropertyType.Enum)
                prop.enumValueIndex = (int)value;
            else if (prop.propertyType == SerializedPropertyType.ObjectReference)
                prop.objectReferenceValue = value as Object;
            so.ApplyModifiedProperties();
        }
    }

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

    // Вспомогательный класс для парсинга массива
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string newJson = "{\"array\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }
        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }
}