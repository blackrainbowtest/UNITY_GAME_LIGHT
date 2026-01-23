//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
// 
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/AutoFillIdleFrames.cs                                                              */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/21 11:56:54 by UDA                                                                    */
/*   Updated: 2026/01/21 11:56:54 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */


using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Editor utility window used to automatically populate idle animation frames
/// from a selected sprites folder.
/// 
/// This tool exists to remove manual, error-prone frame assignment
/// and enforce consistent ordering rules across the project.
/// </summary>
public class AutoFillIdleFrames : EditorWindow
{
    // Target animation asset that will receive the generated frame list.
    private IdleAnimation animationAsset;

    // Folder that contains sprite assets used as animation frames.
    private DefaultAsset spritesFolder;

    [MenuItem("Tools/Auto Fill Idle Frames")]
    public static void ShowWindow()
    {
        GetWindow<AutoFillIdleFrames>("Auto Fill Idle Frames");
    }

    private void OnGUI()
    {
        // Asset reference is intentionally not editable at runtime.
        // This window operates purely in editor context.
        animationAsset = (IdleAnimation)EditorGUILayout.ObjectField(
            "Idle Animation Asset",
            animationAsset,
            typeof(IdleAnimation),
            false
        );

        spritesFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Sprites Folder",
            spritesFolder,
            typeof(DefaultAsset),
            false
        );

        if (GUILayout.Button("Fill Frames"))
        {
            FillFrames();
        }
    }

    /// <summary>
    /// Collects all Sprite assets from the selected folder,
    /// sorts them deterministically by name,
    /// and assigns them to the target IdleAnimation asset.
    /// </summary>
    private void FillFrames()
    {
        if (animationAsset == null || spritesFolder == null)
        {
            Debug.LogError(
                "AutoFillIdleFrames: IdleAnimation asset or sprites folder is not selected."
            );
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(spritesFolder);

        // Defensive check: prevents accidental execution on invalid paths
        // (for example, if a non-folder asset is assigned).
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError(
                "AutoFillIdleFrames: Invalid folder path."
            );
            return;
        }

        // Load all sprites located in the folder and its subfolders.
        // Sorting by name ensures stable frame order across machines and source control.
        var rawSprites = AssetDatabase
            .FindAssets("t:Sprite", new[] { folderPath })
            .Select(guid =>
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    AssetDatabase.GUIDToAssetPath(guid)))
            .ToArray();

        // Prefer numeric ordering when sprite names end with a frame number
        // (e.g. idle_001, idle_002, ... or idle_outfit_01_01, idle_outfit_01_02, ...).
        var parsed = rawSprites
            .Select(s => new
            {
                Sprite = s,
                HasIndex = TryGetTrailingNumber(s != null ? s.name : null, out int index),
                Index = index
            })
            .ToArray();

        bool anyIndexed = parsed.Any(p => p.HasIndex);
        bool allIndexed = parsed.All(p => p.HasIndex);

        if (anyIndexed && !allIndexed)
        {
            Debug.LogWarning(
                "AutoFillIdleFrames: Mixed sprite naming detected (some frames have trailing numbers, some don't). Falling back to name sort."
            );
        }

        Sprite[] sprites = (allIndexed
                ? parsed.OrderBy(p => p.Index).ThenBy(p => p.Sprite.name)
                : parsed.OrderBy(p => p.Sprite != null ? p.Sprite.name : string.Empty))
            .Select(p => p.Sprite)
            .Where(s => s != null)
            .ToArray();

        // IMPORTANT:
        // Frames are assigned through a dedicated editor-only method.
        // Direct data mutation is intentionally avoided to keep animation logic centralized.
        animationAsset.EditorSetFrames(sprites);

        // Marks the asset as modified so Unity persists the change.
        EditorUtility.SetDirty(animationAsset);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"AutoFillIdleFrames: Added {sprites.Length} frames to {animationAsset.name}"
        );
    }

    private static bool TryGetTrailingNumber(string name, out int number)
    {
        number = 0;
        if (string.IsNullOrEmpty(name))
            return false;

        // Capture last contiguous digit group at the end of the string.
        var match = Regex.Match(name, @"(\d+)$");
        if (!match.Success)
            return false;

        return int.TryParse(match.Groups[1].Value, out number);
    }
}

