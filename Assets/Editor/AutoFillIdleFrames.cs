using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class AutoFillIdleFrames : EditorWindow
{
    public IdleAnimation animationAsset;
    public DefaultAsset spritesFolder;

    [MenuItem("Tools/Auto Fill Idle Frames")]
    public static void ShowWindow()
    {
        GetWindow<AutoFillIdleFrames>("Auto Fill Idle Frames");
    }

    void OnGUI()
    {
        animationAsset = (IdleAnimation)EditorGUILayout.ObjectField("Idle Animation Asset", animationAsset, typeof(IdleAnimation), false);
        spritesFolder = (DefaultAsset)EditorGUILayout.ObjectField("Sprites Folder", spritesFolder, typeof(DefaultAsset), false);

        if (GUILayout.Button("Fill Frames"))
        {
            FillFrames();
        }
    }

    void FillFrames()
    {
        if (animationAsset == null || spritesFolder == null)
        {
            Debug.LogError("Select both asset and folder!");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(spritesFolder);
        var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath })
            .Select(guid => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid)))
            .OrderBy(s => s.name)
            .ToArray();

        animationAsset.frames = sprites;
        EditorUtility.SetDirty(animationAsset);
        AssetDatabase.SaveAssets();
        Debug.Log($"Added {sprites.Length} frames to {animationAsset.name}");
    }
}
