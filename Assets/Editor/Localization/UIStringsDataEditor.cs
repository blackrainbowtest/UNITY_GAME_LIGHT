//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
// 
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/Localization/UIStringsDataEditor.cs                                                */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/08 16:51:50 by UDA                                                                    */
/*   Updated: 2026/01/21 16:01:53 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom inspector for UIStringsData.
/// 
/// Provides an editor-side entry point for reimporting localization data
/// from CSV files without exposing internal data mutation logic.
/// </summary>
[CustomEditor(typeof(UIStringsData))]
public class UIStringsDataEditor : Editor
{
    // Editor-only convention:
    // Root folder containing localization CSV files.
    private const string CsvFolder =
        "Assets/GameData/Localization/CSV";

    private const string LegacyCsvFolder =
        "Assets/Data/Localization/CSV";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Reimport from CSV"))
        {
            ReimportFromCsv();
        }
    }

    /// <summary>
    /// Triggers a CSV reimport for the currently selected UIStringsData asset.
    /// </summary>
    private void ReimportFromCsv()
    {
        var data = (UIStringsData)target;

        // NOTE:
        // This action overwrites serialized data based on an external authoritative source (CSV).
        if (!EditorUtility.DisplayDialog(
                "Reimport from CSV",
                "This will overwrite all strings in the asset with data from the CSV file. Continue?",
                "Reimport",
                "Cancel"))
        {
            return;
        }

        var csvFolder = ResolveFolder(CsvFolder, LegacyCsvFolder);

        if (!data.EditorReimportFromCsv(csvFolder, out string error))
        {
            EditorUtility.DisplayDialog(
                "Reimport Failed",
                error,
                "OK"
            );
            return;
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Reimported '{data.name}' from CSV."
        );
    }

    private static string ResolveFolder(string preferred, string legacy)
    {
        if (AssetDatabase.IsValidFolder(preferred))
            return preferred;
        if (AssetDatabase.IsValidFolder(legacy))
            return legacy;
        return preferred;
    }
}

