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
    }
}
