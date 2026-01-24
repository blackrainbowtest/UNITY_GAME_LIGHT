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
using System.Text;

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
        "Assets/GameData/Localization/CSV";

    private const string LegacyCsvFolder =
        "Assets/Data/Localization/CSV";

    private const string AssetFolder =
        "Assets/Resources/Localization/UI";

    private const string LegacyAssetFolder =
        "Assets/GameData/Localization/Assets";

    private const string LegacyAssetFolder2 =
        "Assets/Data/Localization/Assets";

    private static string ResolveFolder(string preferred, string legacy)
    {
        if (AssetDatabase.IsValidFolder(preferred))
            return preferred;
        if (AssetDatabase.IsValidFolder(legacy))
            return legacy;
        return preferred;
    }

    private static string ResolveFolder(string preferred, string legacy1, string legacy2)
    {
        if (AssetDatabase.IsValidFolder(preferred))
            return preferred;
        if (AssetDatabase.IsValidFolder(legacy1))
            return legacy1;
        if (AssetDatabase.IsValidFolder(legacy2))
            return legacy2;
        return preferred;
    }

    [MenuItem("Assets/Localization/Import UIStringsData from CSV (Auto)", false, 1001)]
    public static void ImportUIStringsDataFromCSV_Auto()
    {
        var assetFolder = ResolveFolder(AssetFolder, LegacyAssetFolder, LegacyAssetFolder2);

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
            assetFolder,
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

        if (!asset.EditorReimportFromCsv(selected, out string error))
        {
            EditorUtility.DisplayDialog(
                "Import Failed",
                error,
                "OK"
            );
            return;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Success",
            $"Imported CSV '{assetName}' into asset.",
            "OK"
        );
    }

    [MenuItem("Assets/Localization/Import ALL UIStringsData from CSV", false, 1002)]
    public static void ImportAllUIStringsDataFromCSV()
    {
        var csvFolder = ResolveFolder(CsvFolder, LegacyCsvFolder);
        var assetFolder = ResolveFolder(AssetFolder, LegacyAssetFolder, LegacyAssetFolder2);

        var csvGuids = AssetDatabase.FindAssets(
            "t:TextAsset",
            new[] { csvFolder }
        );

        int imported = 0;
        int failed = 0;
        var failReport = new StringBuilder();

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
                    assetFolder,
                    out UIStringsData asset,
                    out _))
                continue;

            if (!asset.EditorReimportFromCsv(csv, out string error))
            {
                failed++;
                string assetName = Path.GetFileNameWithoutExtension(csvPath);
                failReport.AppendLine(
                    $"{assetName}: {error ?? "import error"}"
                );
                continue;
            }

            EditorUtility.SetDirty(asset);
            imported++;
        }

        AssetDatabase.SaveAssets();

        string failMsg =
            failReport.Length > 0
                ? $"\n\nFailed:\n{failReport}"
                : string.Empty;

        EditorUtility.DisplayDialog(
            "Batch Import",
            $"Imported: {imported}\nFailed: {failed}{failMsg}",
            "OK"
        );
    }

    /// <summary>
    /// Resolves a CSV TextAsset to its corresponding UIStringsData asset.
    /// </summary>
    private static bool TryResolveAsset(
        TextAsset csv,
        string assetFolder,
        out UIStringsData asset,
        out string assetName)
    {
        string csvPath =
            AssetDatabase.GetAssetPath(csv);

        assetName =
            Path.GetFileNameWithoutExtension(csvPath);

        string assetPath =
            $"{assetFolder}/{assetName}.asset";

        asset =
            AssetDatabase.LoadAssetAtPath<UIStringsData>(assetPath);

        return asset != null;
    }
}
