//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
// 
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/LocalizationAssetImporter.cs                                                       */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/21 15:43:48 by UDA                                                                    */
/*   Updated: 2026/01/21 15:43:48 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility responsible for importing localization CSV files
/// into existing UIStringsData assets.
/// 
/// This tool does not create assets.
/// It only synchronizes CSV content with already existing ScriptableObjects.
/// </summary>
public static class LocalizationAssetImporter
{
    // Editor-only conventions for localization asset locations.
    private const string CsvFolder =
        "Assets/Data/Localization/CSV";

    private const string AssetFolder =
        "Assets/Data/Localization/Assets";

    [MenuItem("Assets/Localization/Import UIStringsData from CSV (Auto)", false, 1001)]
    public static void ImportUIStringsDataFromCSV_Auto()
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

        if (!TryResolveAsset(
                selected,
                out UIStringsData asset,
                out string assetName))
        {
            EditorUtility.DisplayDialog(
                "Not found",
                $"No UIStringsData asset found for '{assetName}'.",
                "OK"
            );
            return;
        }

        // TODO:
        // Route all CSV import logic through a single, authoritative method
        // (e.g. UIStringsDataEditor.ImportFromCSV).
        // This tool must remain an orchestrator only.
        // UIStringsDataEditor.ImportFromCSV(asset, selected);

        EditorUtility.DisplayDialog(
            "Success",
            $"Imported CSV '{assetName}' into asset.",
            "OK"
        );
    }

    [MenuItem("Assets/Localization/Import ALL UIStringsData from CSV", false, 1002)]
    public static void ImportAllUIStringsDataFromCSV()
    {
        var csvGuids = AssetDatabase.FindAssets(
            "t:TextAsset",
            new[] { CsvFolder }
        );

        int imported = 0;

        foreach (var guid in csvGuids)
        {
            string csvPath =
                AssetDatabase.GUIDToAssetPath(guid);

            var csv =
                AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);

            if (csv == null)
                continue;

            if (!TryResolveAsset(
                    csv,
                    out UIStringsData asset,
                    out _))
                continue;

            // TODO:
            // Centralize import logic and error handling.
            // UIStringsDataEditor.ImportFromCSV(asset, csv);

            imported++;
        }

        EditorUtility.DisplayDialog(
            "Batch Import",
            $"Imported {imported} CSV files into assets.",
            "OK"
        );
    }

    /// <summary>
    /// Resolves a CSV TextAsset to its corresponding UIStringsData asset.
    /// </summary>
    private static bool TryResolveAsset(
        TextAsset csv,
        out UIStringsData asset,
        out string assetName)
    {
        string csvPath =
            AssetDatabase.GetAssetPath(csv);

        assetName =
            Path.GetFileNameWithoutExtension(csvPath);

        string assetPath =
            $"{AssetFolder}/{assetName}.asset";

        asset =
            AssetDatabase.LoadAssetAtPath<UIStringsData>(assetPath);

        return asset != null;
    }
}

/*
// feature update way
UIStringsDataEditor.ImportFromCSV(
    UIStringsData asset,
    TextAsset csv
);
*/