using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class EnemyDataImporter : EditorWindow
{
    private string jsonPath = "Assets/Data/enemies.json";
    private string outputPath = "Assets/Game/Battle/Data/Enemies";

    [MenuItem("Tools/Import EnemyData from JSON")]
    public static void ShowWindow()
    {
        GetWindow<EnemyDataImporter>("EnemyData Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Импорт врагов из JSON", EditorStyles.boldLabel);
        jsonPath = EditorGUILayout.TextField("JSON Path", jsonPath);
        outputPath = EditorGUILayout.TextField("Output Folder", outputPath);

        if (GUILayout.Button("Импортировать врагов"))
        {
            ImportEnemies();
        }
    }

    private void ImportEnemies()
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"JSON файл не найден: {jsonPath}");
            return;
        }
        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            AssetDatabase.Refresh();
        }
        string json = File.ReadAllText(jsonPath);
        var enemies = JsonUtility.FromJson<EnemyListWrapper>("{\"enemies\":" + json + "}");
        foreach (var enemy in enemies.enemies)
        {
            var asset = ScriptableObject.CreateInstance<Game.Battle.EnemyData>();
            asset.enemyName = enemy.enemyName;
            asset.maxHp = enemy.maxHp;
            asset.maxMp = enemy.maxMp;
            asset.maxSp = enemy.maxSp;
            asset.maxLp = enemy.maxLp;
            asset.attack = enemy.attack;
			asset.hp = enemy.hp;
			asset.mp = enemy.mp;
			asset.sp = enemy.sp;
			asset.lp = enemy.lp;
            asset.icon = AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(enemy.iconPath);
            string assetPath = $"{outputPath}/{enemy.enemyName}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Импортировано врагов: {enemies.enemies.Length}");
    }

    [System.Serializable]
    private class EnemyJson
    {
        public string enemyName;
        public string iconPath;
        public int maxHp;
        public int maxMp;
        public int maxSp;
        public int maxLp;
        public int hp;
        public int mp;
        public int sp;
        public int lp;
        public int attack;
    }
    [System.Serializable]
    private class EnemyListWrapper
    {
        public EnemyJson[] enemies;
    }
}
