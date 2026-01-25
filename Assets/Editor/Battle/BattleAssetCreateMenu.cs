using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Battle
{
    public static class BattleAssetCreateMenu
    {
        [MenuItem("Assets/Create/Game/Battle/Visuals/Idle Animation", priority = 10)]
        public static void CreateIdleAnimation()
        {
            CreateAssetInSelection<IdleAnimation>("IdleAnimation.asset");
        }

        private static void CreateAssetInSelection<T>(string defaultFileName) where T : ScriptableObject
        {
            var folderPath = GetSelectedFolderPathOrAssetsRoot();
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, defaultFileName));

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        private static string GetSelectedFolderPathOrAssetsRoot()
        {
            var selection = Selection.activeObject;
            if (selection == null)
                return "Assets";

            var path = AssetDatabase.GetAssetPath(selection);
            if (string.IsNullOrEmpty(path))
                return "Assets";

            if (Directory.Exists(path))
                return path;

            return Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        }
    }
}
