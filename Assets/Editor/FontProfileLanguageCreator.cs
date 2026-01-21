//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
// 
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/FontProfileLanguageCreator.cs                                                      */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/21 12:12:13 by UDA                                                                    */
/*   Updated: 2026/01/21 12:12:13 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility for creating a language-specific FontProfile
/// and registering it inside FontManager.
/// 
/// This tool exists to keep localization font setup consistent
/// and avoid manual asset wiring.
/// </summary>
public class FontProfileLanguageCreator : EditorWindow
{
    // ISO-like language code (e.g. "en", "ru", "jp").
    private string languageCode = "en";

    [MenuItem("Tools/Create FontProfile Language")]
    public static void ShowWindow()
    {
        GetWindow<FontProfileLanguageCreator>("FontProfile Language Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create FontProfile Language", EditorStyles.boldLabel);

        languageCode = EditorGUILayout.TextField(
            "Language Code",
            languageCode
        );

        if (GUILayout.Button("Create"))
        {
            CreateLanguage();
        }
    }

    /// <summary>
    /// Main creation pipeline:
    /// - validates input
    /// - creates folder and FontProfile asset
    /// - registers the profile in FontManager
    /// </summary>
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

        string folderPath =
            $"Assets/_Project/Localization/Fonts/{languageCode}";
        string assetPath =
            $"{folderPath}/FontProfile.asset";

        // Prevent accidental overwrites or duplicated languages.
        if (AssetDatabase.IsValidFolder(folderPath)
            || File.Exists(assetPath))
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

        var fontProfile =
            ScriptableObject.CreateInstance<FontProfile>();
        AssetDatabase.CreateAsset(fontProfile, assetPath);
        AssetDatabase.SaveAssets();

        // FontManager owns the language-to-profile mapping.
        // This tool must not modify that mapping directly.
        var fontManager =
            Object.FindFirstObjectByType<FontManager>();

        if (fontManager == null)
        {
            EditorUtility.DisplayDialog(
                "Warning",
                "FontManager was not found in the scene. " +
                "Please add the profile manually.",
                "OK"
            );
            return;
        }

        // IMPORTANT:
        // Registration goes through an editor-only method
        // to avoid direct data mutation.
        if (!fontManager.EditorTryAddLanguageProfile(
                languageCode,
                fontProfile,
                out string error))
        {
            EditorUtility.DisplayDialog(
                "Error",
                error,
                "OK"
            );
            return;
        }

        EditorUtility.SetDirty(fontManager);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Done",
            $"Language '{languageCode}' and FontProfile " +
            "were created successfully.",
            "OK"
        );
    }
}
