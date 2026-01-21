  ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
 / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
 \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
  ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
 |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 

/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/ItemDatabaseAutoUpdater.cs                                                         */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/21 13:07:17 by UDA                                                                    */
/*   Updated: 2026/01/21 13:07:17 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Editor utility used to synchronize ItemDatabase with all ItemDefinition assets.
/// 
/// This tool ensures that the database always reflects the current
/// set of item definitions present in the project.
/// </summary>
public static class ItemDatabaseAutoUpdater
{
    private const string DatabasePath =
        "Assets/Game/Items/Definitions/ItemDatabase.asset";

    private const string ItemDefinitionsRoot =
        "Assets/Game/Items/Definitions";

    [MenuItem("Tools/Items/Update Item Database")]
    public static void UpdateDatabase()
    {
        var db = LoadOrCreateDatabase();

        var allItems = FindAllItemDefinitions();

        // TODO: Replace SerializedObject mutation with an explicit
        // editor-only method on ItemDatabase (e.g. EditorSetItems).
        // Direct SerializedProperty manipulation is temporarily allowed
        // for editor tooling but should not be the final architecture.

        ApplyItemsViaSerializedObject(db, allItems);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"ItemDatabase updated: {allItems.Count} items"
        );
    }

    /// <summary>
    /// Loads the ItemDatabase asset or creates it if missing.
    /// </summary>
    private static ItemDatabase LoadOrCreateDatabase()
    {
        var db =
            AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);

        if (db != null)
            return db;

        db = ScriptableObject.CreateInstance<ItemDatabase>();
        AssetDatabase.CreateAsset(db, DatabasePath);

        Debug.Log(
            $"ItemDatabase.asset was not found and has been created at {DatabasePath}"
        );

        return db;
    }

    /// <summary>
    /// Finds all ItemDefinition assets under the definitions root folder.
    /// </summary>
    private static ItemDefinition[] FindAllItemDefinitions()
    {
        return AssetDatabase
            .FindAssets("t:ItemDefinition", new[] { ItemDefinitionsRoot })
            .Select(guid =>
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null)
            .OrderBy(item => item.name)
            .ToArray();
    }

    /// <summary>
    /// Applies item list to the database using SerializedObject.
    /// 
    /// WARNING:
    /// This method relies on internal field names and should be
    /// replaced with a dedicated editor API on ItemDatabase.
    /// </summary>
    private static void ApplyItemsViaSerializedObject(
        ItemDatabase db,
        ItemDefinition[] items)
    {
        var so = new SerializedObject(db);
        var itemsProp = so.FindProperty("items");

        itemsProp.arraySize = items.Length;

        for (int i = 0; i < items.Length; i++)
        {
            itemsProp
                .GetArrayElementAtIndex(i)
                .objectReferenceValue = items[i];
        }

        so.ApplyModifiedProperties();
    }
}
