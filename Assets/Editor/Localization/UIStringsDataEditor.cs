/* ************************************************************************** */
/*                                                                            */
/*   File: Assets/Editor/Localization/UIStringsDataEditor.cs                  */
/*                                                        /\_/\               */
/*                                                       ( •.• )              */
/*   By: unluckydungeonadventure.gmail.com                > ^ <               */
/*                                                                            */
/*   Created: 2026/01/08 16:51:50 by UDA                                      */
/*   Updated: 2026/01/08 16:51:50 by UDA                                      */
/*                                                                            */
/* ************************************************************************** */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIStringsData))]
public class UIStringsDataEditor : Editor
{
    private const string CsvRootPath = "Assets/Localization/CSV";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);

        if (GUILayout.Button("Reimport from CSV"))
        {
            var data = (UIStringsData)target;

            if (!data.EditorReimportFromCsv(CsvRootPath, out string error))
            {
                EditorUtility.DisplayDialog("Reimport Failed", error, "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Reimported '{data.name}' from CSV.");
        }
    }
}
