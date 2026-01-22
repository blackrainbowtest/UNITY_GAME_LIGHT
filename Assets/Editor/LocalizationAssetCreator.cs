//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
// 
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/LocalizationAssetCreator.cs                                                        */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/21 15:37:50 by UDA                                                                    */
/*   Updated: 2026/01/21 15:37:50 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility for creating UIStringsData assets from selected CSV TextAssets.
/// 
/// This tool provides a consistent workflow for generating localization assets
/// without manual setup.
/// </summary>
public static class LocalizationAssetCreator
{
    private const string TargetFolder = "Assets/Data/Localization/Assets";

    [MenuItem("Assets/Create/Localization/Create UIStringsData from CSV", false, 1000)]
    public static void CreateUIStringsDataFromCSV()
    {
        var selected = Selection.activeObject as TextAsset;
        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please select a CSV TextAsset.",
                "OK"
            );
            return;
        }

        string selectedPath = AssetDatabase.GetAssetPath(selected);
        string assetName = Path.GetFileNameWithoutExtension(selectedPath);

        EnsureTargetFolderExists();

        string assetPath = $"{TargetFolder}/{assetName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<UIStringsData>(assetPath);
        if (existing != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Asset already exists",
                $"Asset '{assetName}' already exists. Overwrite?",
                "Overwrite",
                "Cancel"
            );

            if (!overwrite)
                return;

            // Delete the existing asset to avoid CreateAsset errors.
            AssetDatabase.DeleteAsset(assetPath);
        }

        var asset = ScriptableObject.CreateInstance<UIStringsData>();

        if (!asset.EditorSetSourceCsvName(assetName, out string error))
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"Failed to initialize UIStringsData: {error}",
                "OK"
            );
            return;
        }

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Success",
            $"UIStringsData asset '{assetName}' created at {assetPath}.",
            "OK"
        );
    }

    /// <summary>
    /// Ensures the target folder exists in the AssetDatabase.
    /// Uses AssetDatabase APIs to keep Unity's asset pipeline consistent.
    /// </summary>
    private static void EnsureTargetFolderExists()
    {
        if (AssetDatabase.IsValidFolder(TargetFolder))
            return;

        // Create missing parent folders progressively.
        // This avoids relying on Directory.CreateDirectory for AssetDatabase paths.
        string[] parts = TargetFolder.Split('/');
        string current = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }

        AssetDatabase.Refresh();
    }
}

/*
#if UNITY_EDITOR
public void EditorInitialize(string csvName)
{
    sourceCsvName = csvName;
}
#endif
*/