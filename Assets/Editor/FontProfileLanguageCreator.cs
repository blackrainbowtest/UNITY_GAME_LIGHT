/* ************************************************************************** */
/*                                                                            */
/*   File: Assets/Editor/FontProfileLanguageCreator.cs                        */
/*                                                        /\_/\               */
/*                                                       ( •.• )              */
/*   By: unluckydungeonadventure.gmail.com                > ^ <               */
/*                                                                            */
/*   Created: 2026/01/08 10:39:54 by UDA                                      */
/*   Updated: 2026/01/08 10:39:54 by UDA                                      */
/*                                                                            */
/* ************************************************************************** */

using UnityEngine;
using UnityEditor;
using System.IO;

public class FontProfileLanguageCreator : EditorWindow
{
    private string languageCode = "en";

    [MenuItem("Tools/Create FontProfile Language")]
    public static void ShowWindow()
    {
        GetWindow<FontProfileLanguageCreator>("FontProfile Language Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create FontProfile Language", EditorStyles.boldLabel);
        languageCode = EditorGUILayout.TextField("Language Code", languageCode);

        if (GUILayout.Button("Create"))
        {
            CreateLanguage();
        }
    }

    private void CreateLanguage()
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Language code cannot be empty.",
                "OK"
            );
            return;
        }

        string folderPath = $"Assets/_Project/Localization/Fonts/{languageCode}";
        string assetPath = $"{folderPath}/FontProfile.asset";

        if (AssetDatabase.IsValidFolder(folderPath) || File.Exists(assetPath))
        {
            EditorUtility.DisplayDialog(
                "Warning",
                $"Language '{languageCode}' already exists.",
                "OK"
            );
            return;
        }

        Directory.CreateDirectory(folderPath);
        AssetDatabase.Refresh();

        var fontProfile = ScriptableObject.CreateInstance<FontProfile>();
        AssetDatabase.CreateAsset(fontProfile, assetPath);
        AssetDatabase.SaveAssets();

        var fontManager = Object.FindFirstObjectByType<FontManager>();
        if (fontManager == null)
        {
            EditorUtility.DisplayDialog(
                "Warning",
                "FontManager was not found in the scene. Please add the profile manually.",
                "OK"
            );
            return;
        }

        if (!fontManager.EditorTryAddLanguageProfile(
                languageCode,
                fontProfile,
                out string error))
        {
            EditorUtility.DisplayDialog("Error", error, "OK");
            return;
        }

        EditorUtility.SetDirty(fontManager);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Done",
            $"Language '{languageCode}' and FontProfile were created successfully.",
            "OK"
        );
    }
}
