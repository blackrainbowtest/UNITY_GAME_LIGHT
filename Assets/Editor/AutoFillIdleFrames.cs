/* ************************************************************************** */
/*                                                                            */
/*   File: Assets/Editor/AutoFillIdleFrames.cs                                */
/*                                                        /\_/\               */
/*                                                       ( •.• )              */
/*   By: unluckydungeonadventure.gmail.com                > ^ <               */
/*                                                                            */
/*   Created: 2026/01/08 10:21:37 by UDA                                      */
/*   Updated: 2026/01/08 10:21:37 by UDA                                      */
/*                                                                            */
/* ************************************************************************** */

using UnityEngine;
using UnityEditor;
using System.Linq;

public class AutoFillIdleFrames : EditorWindow
{
    private IdleAnimation animationAsset;
    private DefaultAsset spritesFolder;

    [MenuItem("Tools/Auto Fill Idle Frames")]
    public static void ShowWindow()
    {
        GetWindow<AutoFillIdleFrames>("Auto Fill Idle Frames");
    }

    private void OnGUI()
    {
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

    private void FillFrames()
    {
        if (animationAsset == null || spritesFolder == null)
        {
            Debug.LogError("AutoFillIdleFrames: Asset or folder is not selected.");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(spritesFolder);

        Sprite[] sprites = AssetDatabase
            .FindAssets("t:Sprite", new[] { folderPath })
            .Select(guid =>
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    AssetDatabase.GUIDToAssetPath(guid)))
            .OrderBy(sprite => sprite.name)
            .ToArray();

        animationAsset.EditorSetFrames(sprites);

        EditorUtility.SetDirty(animationAsset);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"AutoFillIdleFrames: Added {sprites.Length} frames to {animationAsset.name}"
        );
    }
}
