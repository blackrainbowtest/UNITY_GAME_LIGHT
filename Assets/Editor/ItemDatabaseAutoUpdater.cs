using UnityEditor;
using UnityEngine;
using System.Linq;

public static class ItemDatabaseAutoUpdater
{
    [MenuItem("Tools/Items/Update Item Database")]
    public static void UpdateDatabase()
    {
        string dbPath = "Assets/Game/Items/Definitions/ItemDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
            Debug.Log($"ItemDatabase.asset was not found and has been created at {dbPath}");
        }
        var allItems = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/Game/Items/Definitions" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .ToList();
        var so = new SerializedObject(db);
        var itemsProp = so.FindProperty("items");
        itemsProp.arraySize = allItems.Count;
        for (int i = 0; i < allItems.Count; i++)
            itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = allItems[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"ItemDatabase updated: {allItems.Count} items");
    }
}
