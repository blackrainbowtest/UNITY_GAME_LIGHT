using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public static class LocalizationAssetImporter
{
    private const string CsvFolder = "Assets/Data/Localization/CSV/";
    private const string AssetFolder = "Assets/Data/Localization/Assets/";

    [MenuItem("Assets/Localization/Import UIStringsData from CSV (Auto)", false, 1001)]
    public static void ImportUIStringsDataFromCSV_Auto()
    {
        var selected = Selection.activeObject as TextAsset;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a CSV TextAsset.", "OK");
            return;
        }
        string assetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(selected));
        string assetPath = Path.Combine(AssetFolder, assetName + ".asset");
        var asset = AssetDatabase.LoadAssetAtPath<UIStringsData>(assetPath);
        if (asset == null)
        {
            EditorUtility.DisplayDialog("Not found", $"No UIStringsData asset found for '{assetName}'.", "OK");
            return;
        }
        // Call your existing import logic here, e.g.:
        // UIStringsDataEditor.ImportFromCSV(asset, selected);
        EditorUtility.DisplayDialog("Success", $"Imported CSV '{assetName}' into asset.", "OK");
    }

    [MenuItem("Assets/Localization/Import ALL UIStringsData from CSV", false, 1002)]
    public static void ImportAllUIStringsDataFromCSV()
    {
        var csvGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { CsvFolder });
        int imported = 0;
        foreach (var guid in csvGuids)
        {
            string csvPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetName = Path.GetFileNameWithoutExtension(csvPath);
            string assetPath = Path.Combine(AssetFolder, assetName + ".asset");
            var asset = AssetDatabase.LoadAssetAtPath<UIStringsData>(assetPath);
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
            if (asset != null && csv != null)
            {
                // Call your existing import logic here, e.g.:
                // UIStringsDataEditor.ImportFromCSV(asset, csv);
                imported++;
            }
        }
        EditorUtility.DisplayDialog("Batch Import", $"Imported {imported} CSV files into assets.", "OK");
    }
}
