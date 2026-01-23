//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
// 
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Editor/EnemyDataImporter.cs                                                               */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/21 12:00:09 by UDA                                                                    */
/*   Updated: 2026/01/21 12:00:09 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Game.Battle.Combat.Actions;

/// <summary>
/// Editor-only utility used to import enemy definitions from JSON
/// and convert them into EnemyData ScriptableObjects.
/// 
/// This tool is intentionally isolated from runtime code.
/// </summary>
public class EnemyDataImporter : EditorWindow
{
    // Path to the source JSON file containing enemy definitions.
    private string jsonPath = "Assets/Data/enemies.json";

    // Output folder where EnemyData assets will be created or updated.
    private string outputPath = "Assets/GameData/Battle/Data/Enemies";

    [MenuItem("Tools/Import EnemyData from JSON")]
    public static void ShowWindow()
    {
        GetWindow<EnemyDataImporter>("EnemyData Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Import enemies from JSON", EditorStyles.boldLabel);

        jsonPath = EditorGUILayout.TextField("JSON Path", jsonPath);
        outputPath = EditorGUILayout.TextField("Output Folder", outputPath);

        if (GUILayout.Button("Import Enemies"))
        {
            ImportEnemies();
        }
    }

    /// <summary>
    /// Main import pipeline.
    /// Reads JSON, validates data, and synchronizes EnemyData assets.
    /// </summary>
    private void ImportEnemies()
    {
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"EnemyDataImporter: JSON file not found at path '{jsonPath}'.");
            return;
        }

        EnsureOutputFolderExists();

        string json = File.ReadAllText(jsonPath);

        // JsonUtility requires a wrapper object for array deserialization.
        var enemies = JsonUtility.FromJson<EnemyListWrapper>(
            "{\"enemies\":" + json + "}"
        );

        foreach (var enemy in enemies.enemies)
        {
            ImportSingleEnemy(enemy);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"EnemyDataImporter: Imported enemies count = {enemies.enemies.Length}");
    }

    /// <summary>
    /// Imports or updates a single EnemyData asset.
    /// </summary>
    private void ImportSingleEnemy(EnemyJson enemy)
    {
        string assetPath = $"{outputPath}/{enemy.enemyName}.asset";

        var asset = AssetDatabase.LoadAssetAtPath<Game.Battle.EnemyData>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<Game.Battle.EnemyData>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        var icon = LoadSprite(enemy.iconPath);
        var actions = ParseAllowedActions(enemy.allowedActions);

        if (!asset.EditorApplyDefinition(
                newEnemyName: enemy.enemyName,
                newIcon: icon,
                newMaxHp: enemy.maxHp,
                newMaxMp: enemy.maxMp,
                newMaxSp: enemy.maxSp,
                newMaxLp: enemy.maxLp,
                newHp: enemy.hp,
                newMp: enemy.mp,
                newSp: enemy.sp,
                newLp: enemy.lp,
                newAttack: enemy.attack,
                newRegenHpPerTurn: enemy.regenHpPerTurn,
                newRegenMpPerTurn: enemy.regenMpPerTurn,
                newRegenSpPerTurn: enemy.regenSpPerTurn,
                newAllowedActions: actions,
                out string error))
        {
            Debug.LogError($"EnemyDataImporter: Failed to apply '{enemy.enemyName}': {error}");
        }
    }

    /// <summary>
    /// Ensures that the output folder exists in the AssetDatabase.
    /// </summary>
    private void EnsureOutputFolderExists()
    {
        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Loads a sprite by asset path.
    /// </summary>
    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>
    /// Converts string-based action identifiers into CombatActionId enum values.
    /// Ensures backward compatibility and safe defaults.
    /// </summary>
    private static CombatActionId[] ParseAllowedActions(string[] allowedActions)
    {
        // Backward compatible default: physical attacker.
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
                Debug.LogWarning(
                    $"EnemyDataImporter: Unknown allowed action '{raw}'. Skipping."
                );
            }
        }

        // Ensure enemy always has at least one valid action.
        if (list.Count == 0)
        {
            list.Add(CombatActionId.FastAttack);
            list.Add(CombatActionId.NormalAttack);
            list.Add(CombatActionId.HeavyAttack);
        }

        return list.ToArray();
    }

    #region JSON DTOs

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

        // Passive regeneration per enemy turn (LP does not regenerate).
        public int regenHpPerTurn;
        public int regenMpPerTurn;
        public int regenSpPerTurn;

        // Optional action restrictions.
        public string[] allowedActions;
    }

    [System.Serializable]
    private class EnemyListWrapper
    {
        public EnemyJson[] enemies;
    }

    #endregion
}

