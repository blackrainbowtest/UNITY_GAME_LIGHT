#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Battle.Editor
{
    public static class BattleContentDatabaseAssetCreator
    {
        private const string ResourcesDir = "Assets/Resources";
        private const string DatabaseFolder = "Assets/Resources/Game/Battle";
        private const string DatabaseAssetPath = "Assets/Resources/Game/Battle/BattleContentDatabase.asset";

        [MenuItem("Tools/Game/Battle/Create or Update Content Database")]
        public static void CreateOrUpdate()
        {
            EnsureFolders();

            var db = AssetDatabase.LoadAssetAtPath<BattleContentDatabase>(DatabaseAssetPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<BattleContentDatabase>();
                AssetDatabase.CreateAsset(db, DatabaseAssetPath);
            }

            var enemies = FindAllAssets<EnemyData>("t:EnemyData");
            var locations = FindAllAssets<BattleLocationData>("t:BattleLocationData");

            var so = new SerializedObject(db);
            so.FindProperty("enemies").arraySize = enemies.Length;
            for (int i = 0; i < enemies.Length; i++)
                so.FindProperty("enemies").GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];

            so.FindProperty("locations").arraySize = locations.Length;
            for (int i = 0; i < locations.Length; i++)
                so.FindProperty("locations").GetArrayElementAtIndex(i).objectReferenceValue = locations[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            Selection.activeObject = db;
            EditorGUIUtility.PingObject(db);

            Debug.Log($"[BattleContentDatabaseAssetCreator] Updated {DatabaseAssetPath} (enemies={enemies.Length}, locations={locations.Length}).");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesDir))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder("Assets/Resources/Game"))
                AssetDatabase.CreateFolder("Assets/Resources", "Game");

            if (!AssetDatabase.IsValidFolder(DatabaseFolder))
                AssetDatabase.CreateFolder("Assets/Resources/Game", "Battle");

            // Also ensure the OS directory exists (Unity should handle it, but keep it safe).
            var full = Path.GetFullPath(DatabaseFolder);
            if (!Directory.Exists(full))
                Directory.CreateDirectory(full);
        }

        private static T[] FindAllAssets<T>(string filter) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets(filter);
            if (guids == null || guids.Length == 0)
                return new T[0];

            var result = new System.Collections.Generic.List<T>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    result.Add(asset);
            }

            return result.ToArray();
        }
    }
}
#endif
