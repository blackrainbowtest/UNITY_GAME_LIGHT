using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Game.Battle.Combat.Actions;

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
            string assetPath = $"{outputPath}/{enemy.enemyName}.asset";

            var asset = AssetDatabase.LoadAssetAtPath<Game.Battle.EnemyData>(assetPath);
            if (asset == null)
                asset = ScriptableObject.CreateInstance<Game.Battle.EnemyData>();

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

            asset.allowedActions = ParseAllowedActions(enemy.allowedActions);

            asset.regenHpPerTurn = enemy.regenHpPerTurn;
            asset.regenMpPerTurn = enemy.regenMpPerTurn;
            asset.regenSpPerTurn = enemy.regenSpPerTurn;

            if (AssetDatabase.GetAssetPath(asset) == string.Empty)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Импортировано врагов: {enemies.enemies.Length}");
    }

    private static CombatActionId[] ParseAllowedActions(string[] allowedActions)
    {
        // Backward compatible: if JSON does not specify allowedActions,
        // default to a physical attacker (fast/normal/heavy).
        if (allowedActions == null || allowedActions.Length == 0)
        {
            return new[]
            {
                CombatActionId.FastAttack,
                CombatActionId.NormalAttack,
                CombatActionId.HeavyAttack
            };
        }

        var list = new List<CombatActionId>(allowedActions.Length);
        foreach (var raw in allowedActions)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            if (System.Enum.TryParse(raw.Trim(), ignoreCase: true, out CombatActionId id))
            {
                if (!list.Contains(id))
                    list.Add(id);
            }
            else
            {
                Debug.LogWarning($"[EnemyDataImporter] Unknown allowed action '{raw}'. Skipping.");
            }
        }

        // Ensure there is at least something usable.
        if (list.Count == 0)
        {
            list.Add(CombatActionId.FastAttack);
            list.Add(CombatActionId.NormalAttack);
            list.Add(CombatActionId.HeavyAttack);
        }

        return list.ToArray();
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

        // Passive regen per enemy turn (LP does not regenerate).
        public int regenHpPerTurn;
        public int regenMpPerTurn;
        public int regenSpPerTurn;

        // Optional: restrict what the enemy can do.
        // Example: ["FastAttack","HeavyAttack"] or ["FireSpell","DarkSpell"].
        public string[] allowedActions;
    }
    [System.Serializable]
    private class EnemyListWrapper
    {
        public EnemyJson[] enemies;
    }
}
