using UnityEditor;
using UnityEngine;
using System.IO;

public static class LocalizationAssetCreator
{
    private const string TargetFolder = "Assets/Data/Localization/Assets/";

    [MenuItem("Assets/Create/Localization/Create UIStringsData from CSV", false, 1000)]
    public static void CreateUIStringsDataFromCSV()
    {
        var selected = Selection.activeObject as TextAsset;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a CSV TextAsset.", "OK");
            return;
        }

        string assetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(selected));
        string assetPath = Path.Combine(TargetFolder, assetName + ".asset");

        if (!Directory.Exists(TargetFolder))
        {
            Directory.CreateDirectory(TargetFolder);
            AssetDatabase.Refresh();
        }

        var existing = AssetDatabase.LoadAssetAtPath<UIStringsData>(assetPath);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Asset already exists", $"Asset '{assetName}' already exists. Overwrite?", "Overwrite", "Cancel"))
                return;
        }

        var asset = ScriptableObject.CreateInstance<UIStringsData>();
        asset.sourceCsvName = assetName;
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"UIStringsData asset '{assetName}' created at {assetPath}.", "OK");
    }
}
