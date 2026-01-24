//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
// 
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/Localization/UIStringsCsvImportWindow.cs                                           */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/08 16:21:10 by UDA                                                                    */
/*   Updated: 2026/01/21 15:48:21 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

/// <summary>
/// Editor window for importing UIStringsData from CSV files.
/// 
/// This tool acts purely as an orchestrator:
/// - validates input
/// - delegates parsing
/// - delegates data application to UIStringsData
/// </summary>
public class UIStringsCsvImportWindow : EditorWindow
{
    private TextAsset csvFile;
    private UIStringsData target;

    // Editor-only localization conventions.
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

    [MenuItem("Tools/Localization/Import UI Strings from CSV")]
    public static void ShowWindow()
    {
        GetWindow<UIStringsCsvImportWindow>(
            "UI Strings CSV Importer"
        );
    }

    private void OnGUI()
    {
        DrawSingleImportSection();

        EditorGUILayout.Space();

        DrawBatchImportSection();
    }

    private void DrawSingleImportSection()
    {
        csvFile = (TextAsset)EditorGUILayout.ObjectField(
            "CSV File",
            csvFile,
            typeof(TextAsset),
            false
        );

        target = (UIStringsData)EditorGUILayout.ObjectField(
            "Target Asset",
            target,
            typeof(UIStringsData),
            false
        );

        using (new EditorGUI.DisabledScope(
                   csvFile == null || target == null))
        {
            if (GUILayout.Button("Import"))
            {
                ImportSingle(csvFile, target);
            }
        }
    }

    private void DrawBatchImportSection()
    {
        if (!GUILayout.Button("Import ALL CSVs in folder"))
            return;

        ImportAllFromFolder();
    }

    /// <summary>
    /// Imports a single CSV into a target UIStringsData asset.
    /// </summary>
    private void ImportSingle(TextAsset csv, UIStringsData asset)
    {
        if (!UIStringsCsvParser.TryParse(
                csv.text,
                out _,
                out var error))
        {
            EditorUtility.DisplayDialog(
                "Error",
                error,
                "OK"
            );
            return;
        }

        // NOTE:
        // Parsed data is intentionally not passed directly.
        // UIStringsData owns the authoritative import logic.
        string csvFolder = ResolveFolder(CsvFolder, LegacyCsvFolder);
        if (!asset.EditorReimportFromCsv(
            csvFolder,
                out error))
        {
            EditorUtility.DisplayDialog(
                "Error",
                error,
                "OK"
            );
            return;
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Batch-imports all CSV files and reports detailed failures.
    /// </summary>
    private void ImportAllFromFolder()
    {
        int imported = 0;
        int failed = 0;

        var failReport = new StringBuilder();

        string csvFolder = ResolveFolder(CsvFolder, LegacyCsvFolder);
        string assetFolder = ResolveFolder(AssetFolder, LegacyAssetFolder, LegacyAssetFolder2);

        if (!AssetDatabase.IsValidFolder(csvFolder))
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"CSV folder not found: '{csvFolder}'\n\nExpected new path: '{CsvFolder}'\nLegacy path: '{LegacyCsvFolder}'",
                "OK"
            );
            return;
        }

        var csvGuids = AssetDatabase.FindAssets(
            "t:TextAsset",
            new[] { csvFolder }
        );

        foreach (var guid in csvGuids)
        {
            string csvPath =
                AssetDatabase.GUIDToAssetPath(guid);

            string assetName =
                Path.GetFileNameWithoutExtension(csvPath);

            string assetPath =
                $"{assetFolder}/{assetName}.asset";

            var asset =
                AssetDatabase.LoadAssetAtPath<UIStringsData>(assetPath);

            var csv =
                AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);

            if (asset == null)
            {
                failed++;
                failReport.AppendLine(
                    $"{assetName}: asset not found"
                );
                continue;
            }

            if (csv == null)
            {
                failed++;
                failReport.AppendLine(
                    $"{assetName}: CSV not found"
                );
                continue;
            }

            if (!UIStringsCsvParser.TryParse(
                    csv.text,
                    out _,
                    out var error))
            {
                failed++;
                failReport.AppendLine(
                    $"{assetName}: {error ?? "parse error"}"
                );
                continue;
            }

            if (!asset.EditorReimportFromCsv(
                    csvFolder,
                    out error))
            {
                failed++;
                failReport.AppendLine(
                    $"{assetName}: {error ?? "import error"}"
                );
                continue;
            }

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
}
