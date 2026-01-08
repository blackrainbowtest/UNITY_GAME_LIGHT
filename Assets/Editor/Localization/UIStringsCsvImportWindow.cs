/* ************************************************************************** */
/*                                                                            */
/*   File: Assets/Editor/Localization/UIStringsCsvImportWindow.cs             */
/*                                                        /\_/\               */
/*                                                       ( •.• )              */
/*   By: unluckydungeonadventure.gmail.com                > ^ <               */
/*                                                                            */
/*   Created: 2026/01/08 16:21:10 by UDA                                      */
/*   Updated: 2026/01/08 16:21:10 by UDA                                      */
/*                                                                            */
/* ************************************************************************** */

using UnityEngine;
using UnityEditor;

public class UIStringsCsvImportWindow : EditorWindow
{
    private TextAsset csvFile;
    private UIStringsData target;

    [MenuItem("Tools/Localization/Import UI Strings from CSV")]
    public static void ShowWindow()
    {
        GetWindow<UIStringsCsvImportWindow>("UI Strings CSV Importer");
    }

    private void OnGUI()
    {
csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        target = (UIStringsData)EditorGUILayout.ObjectField("Target Asset", target, typeof(UIStringsData), false);

        using (new EditorGUI.DisabledScope(csvFile == null || target == null))
        {
            if (GUILayout.Button("Import"))
            {
                if (!UIStringsCsvParser.TryParse(csvFile.text, out var parsed, out var error))
                {
                    EditorUtility.DisplayDialog("Error", error, "OK");
                    return;
                }

                if (!target.EditorReimportFromCsv("Assets/Data/Localization/CSV", out error))
                {
                    EditorUtility.DisplayDialog("Error", error, "OK");
                    return;
                }

                AssetDatabase.SaveAssets();
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Import ALL CSVs in folder"))
        {
            int imported = 0;
            int failed = 0;
            string folder = "Assets/Data/Localization/CSV";
            string assetFolder = "Assets/Data/Localization/Assets";
            var csvGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { folder });
            System.Text.StringBuilder failReport = new System.Text.StringBuilder();
            foreach (var guid in csvGuids)
            {
                string csvPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = System.IO.Path.GetFileNameWithoutExtension(csvPath);
                string assetPath = System.IO.Path.Combine(assetFolder, assetName + ".asset");
                var asset = AssetDatabase.LoadAssetAtPath<UIStringsData>(assetPath);
                var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
                if (asset == null)
                {
                    failed++;
                    failReport.AppendLine($"{assetName}: asset not found");
                    continue;
                }
                if (csv == null)
                {
                    failed++;
                    failReport.AppendLine($"{assetName}: CSV not found");
                    continue;
                }
                if (!UIStringsCsvParser.TryParse(csv.text, out var parsed, out var error))
                {
                    failed++;
                    failReport.AppendLine($"{assetName}: {error ?? "parse error"}");
                    continue;
                }
                string dummy;
                if (!asset.EditorReimportFromCsv(folder, out dummy))
                {
                    failed++;
                    failReport.AppendLine($"{assetName}: {dummy ?? "import error"}");
                    continue;
                }
                imported++;
            }
            AssetDatabase.SaveAssets();
            string failMsg = failReport.Length > 0 ? $"\n\nFailed:\n{failReport}" : "";
            EditorUtility.DisplayDialog("Batch Import", $"Imported: {imported}\nFailed: {failed}{failMsg}", "OK");
        }
    }
}
