#if UNITY_EDITOR
using System;
using System.IO;
using Game.Battle;
using Game.Dungeon;
using Game.Progression;
using UnityEditor;
using UnityEngine;

namespace UDA2.EditorTools
{
    public static class DungeonNewbieLocationsGenerator
    {
        private const string DungeonRoot = "Assets/GameData/Dungeon";
        private const string DungeonLocationsFolder = "Assets/GameData/Dungeon/Locations";
        private const string BattleLocationsFolder = "Assets/GameData/Battle/Data/Locations";
        private const string EncounterTablesFolder = "Assets/GameData/Battle/Encounters/Tables";

        [MenuItem("UDA2/Dungeon/Create Newbie Location Assets")]
        public static void CreateNewbieLocationAssets()
        {
            EnsureFolder("Assets/GameData");
            EnsureFolder("Assets/GameData/Battle");
            EnsureFolder("Assets/GameData/Battle/Data");
            EnsureFolder(BattleLocationsFolder);
            EnsureFolder("Assets/GameData/Battle/Encounters");
            EnsureFolder(EncounterTablesFolder);
            EnsureFolder(DungeonRoot);
            EnsureFolder(DungeonLocationsFolder);

            var entries = new[]
            {
                new LocationSeed("farm", "Farm"),
                new LocationSeed("wheat_field", "Wheat Field"),
                new LocationSeed("valley", "Valley"),
                new LocationSeed("river", "River"),
            };

            var slaver = FindEnemy("Slaver", "slaver");
            var slime = FindEnemy("Slime", "slime");

            UnityEngine.Object last = null;

            for (var i = 0; i < entries.Length; i++)
            {
                var seed = entries[i];

                var battleLocation = CreateOrLoad<BattleLocationData>(
                    $"{BattleLocationsFolder}/bld_{seed.key}.asset",
                    () => ScriptableObject.CreateInstance<BattleLocationData>());

                if (string.IsNullOrWhiteSpace(battleLocation.id))
                    battleLocation.id = $"bld_{seed.key}";
                EditorUtility.SetDirty(battleLocation);

                var table = CreateOrLoad<EnemySpawnTable>(
                    $"{EncounterTablesFolder}/est_{seed.key}.asset",
                    () => ScriptableObject.CreateInstance<EnemySpawnTable>());

                FillSpawnTableIfEmpty(table, slaver, slime);

                var location = CreateOrLoad<DungeonLocationDefinition>(
                    $"{DungeonLocationsFolder}/dld_{seed.key}.asset",
                    () => ScriptableObject.CreateInstance<DungeonLocationDefinition>());

                if (string.IsNullOrWhiteSpace(location.id))
                    location.id = $"dld_{seed.key}";

                location.requiredRank = AdventurerRank.None;
                if (string.IsNullOrWhiteSpace(location.fightSceneName))
                    location.fightSceneName = "FightScene";

                location.returnToActiveSceneAfterBattle = true;

                if (location.encounterPools == null || location.encounterPools.Length == 0)
                {
                    location.encounterPools = new[]
                    {
                        new DungeonEncounterPool
                        {
                            battleLocation = battleLocation,
                            enemyTable = table,
                            weight = 1,
                        }
                    };
                }

                EditorUtility.SetDirty(location);
                last = location;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (last != null)
                Selection.activeObject = last;

            Debug.Log("[Dungeon] Newbie location assets created/updated.");
        }

        [MenuItem("Assets/Create/UDA2/Dungeon/Create Newbie Location Assets", priority = 2200)]
        public static void CreateNewbieLocationAssetsFromCreateMenu()
        {
            CreateNewbieLocationAssets();
        }

        private static void FillSpawnTableIfEmpty(EnemySpawnTable table, EnemyData slaver, EnemyData slime)
        {
            var so = new SerializedObject(table);
            var entries = so.FindProperty("entries");
            if (entries == null || !entries.isArray || entries.arraySize > 0)
                return;

            AddEntry(entries, slaver, 60);
            AddEntry(entries, slime, 40);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
        }

        private static void AddEntry(SerializedProperty array, EnemyData enemy, int weight)
        {
            if (enemy == null)
                return;

            var index = array.arraySize;
            array.InsertArrayElementAtIndex(index);

            var element = array.GetArrayElementAtIndex(index);
            var enemyProp = element.FindPropertyRelative("enemy");
            if (enemyProp != null)
                enemyProp.objectReferenceValue = enemy;

            var weightProp = element.FindPropertyRelative("weight");
            if (weightProp != null)
                weightProp.intValue = Mathf.Max(0, weight);
        }

        private static T CreateOrLoad<T>(string path, Func<T> create) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            var instance = create();
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        private static EnemyData FindEnemy(string assetName, string id)
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(EnemyData)} {assetName}");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (asset == null)
                    continue;

                if (string.Equals(asset.name, assetName, StringComparison.OrdinalIgnoreCase))
                    return asset;

                if (!string.IsNullOrWhiteSpace(asset.id) && string.Equals(asset.id, id, StringComparison.OrdinalIgnoreCase))
                    return asset;
            }

            return null;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct LocationSeed
        {
            public readonly string key;
            public readonly string title;

            public LocationSeed(string key, string title)
            {
                this.key = key;
                this.title = title;
            }
        }
    }
}
#endif
