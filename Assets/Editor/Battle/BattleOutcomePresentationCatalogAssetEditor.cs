using System;
using System.Collections.Generic;
using Game.Battle;
using Game.Battle.UI;
using UnityEditor;
using UnityEngine;

namespace UDA2.Editor.Battle
{
    [CustomPropertyDrawer(typeof(BattleOutcomePresentationCatalogAsset.EnemyGroup))]
    public sealed class BattleOutcomeEnemyGroupDrawer : PropertyDrawer
    {
        private static readonly float VSpace = EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;

            var anyEnemyProp = property.FindPropertyRelative("anyEnemy");
            var rulesProp = property.FindPropertyRelative("rules");

            height += VSpace + EditorGUIUtility.singleLineHeight;
            if (anyEnemyProp != null && !anyEnemyProp.boolValue)
                height += VSpace + EditorGUIUtility.singleLineHeight;

            if (rulesProp != null)
            {
                height += VSpace + EditorGUI.GetPropertyHeight(rulesProp, includeChildren: true);
            }

            return height + VSpace;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
                return;

            EditorGUI.BeginProperty(position, label, property);

            var anyEnemyProp = property.FindPropertyRelative("anyEnemy");
            var enemyIdOverrideProp = property.FindPropertyRelative("enemyIdOverride");
            var enemyLegacyProp = property.FindPropertyRelative("enemy");
            var rulesProp = property.FindPropertyRelative("rules");

            var contentRect = EditorGUI.IndentedRect(position);
            float y = contentRect.y;
            var foldoutRect = new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            y += EditorGUIUtility.singleLineHeight + VSpace;

            if (property.isExpanded)
            {
                if (anyEnemyProp != null)
                {
                    var anyEnemyRect = new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(anyEnemyRect, anyEnemyProp);
                    y += EditorGUIUtility.singleLineHeight + VSpace;
                }

                if (enemyIdOverrideProp != null && anyEnemyProp != null && !anyEnemyProp.boolValue)
                {
                    // Migration: if old object reference is still assigned and id is empty, convert once.
                    if (string.IsNullOrWhiteSpace(enemyIdOverrideProp.stringValue) &&
                        enemyLegacyProp != null &&
                        enemyLegacyProp.objectReferenceValue is EnemyData enemyData &&
                        !string.IsNullOrWhiteSpace(enemyData.id))
                    {
                        enemyIdOverrideProp.stringValue = enemyData.id.Trim();
                    }

                    DrawEnemyDropdown(new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight), enemyIdOverrideProp);
                    y += EditorGUIUtility.singleLineHeight + VSpace;
                }

                if (rulesProp != null)
                {
                    var rulesHeight = EditorGUI.GetPropertyHeight(rulesProp, includeChildren: true);
                    var rulesRect = new Rect(contentRect.x, y, contentRect.width, rulesHeight);
                    EditorGUI.PropertyField(rulesRect, rulesProp, includeChildren: true);
                }
            }

            EditorGUI.EndProperty();
        }

        private static void DrawEnemyDropdown(Rect rect, SerializedProperty enemyIdProp)
        {
            var options = EnemyOptionProvider.GetOptions();
            var labels = EnemyOptionProvider.GetLabels(options);

            int selectedIndex = 0;
            var currentId = enemyIdProp != null ? enemyIdProp.stringValue : string.Empty;
            if (!string.IsNullOrWhiteSpace(currentId))
            {
                for (int i = 0; i < options.Count; i++)
                {
                    if (string.Equals(options[i], currentId.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            int newIndex = EditorGUI.Popup(rect, "Enemy", selectedIndex, labels);
            if (enemyIdProp != null && newIndex >= 0 && newIndex < options.Count)
                enemyIdProp.stringValue = options[newIndex];
        }

        private static class EnemyOptionProvider
        {
            private static List<string> cachedIds;
            private static double lastRefreshTime;

            public static List<string> GetOptions()
            {
                // Refresh every 2 seconds while inspector is open.
                if (cachedIds != null && EditorApplication.timeSinceStartup - lastRefreshTime < 2.0)
                    return cachedIds;

                var ids = new List<string> { string.Empty };
                var guids = AssetDatabase.FindAssets("t:EnemyData");
                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                    if (enemy == null)
                        continue;

                    string id = !string.IsNullOrWhiteSpace(enemy.id)
                        ? enemy.id.Trim()
                        : enemy.name;

                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    if (!ids.Contains(id))
                        ids.Add(id);
                }

                ids.Sort((a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
                if (ids.Count == 0 || ids[0] != string.Empty)
                    ids.Insert(0, string.Empty);

                cachedIds = ids;
                lastRefreshTime = EditorApplication.timeSinceStartup;
                return cachedIds;
            }

            public static string[] GetLabels(List<string> options)
            {
                if (options == null || options.Count == 0)
                    return new[] { "<none>" };

                var labels = new string[options.Count];
                for (int i = 0; i < options.Count; i++)
                {
                    labels[i] = string.IsNullOrWhiteSpace(options[i]) ? "<none>" : options[i];
                }

                return labels;
            }
        }
    }

    [CustomPropertyDrawer(typeof(BattleOutcomePresentationCatalogAsset.RuleEntry))]
    public sealed class BattleOutcomeRuleEntryDrawer : PropertyDrawer
    {
        private static readonly float VSpace = EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
                return EditorGUIUtility.singleLineHeight;

            var filterProp = property.FindPropertyRelative("filter");
            var matchProp = property.FindPropertyRelative("match");
            var presentationProp = property.FindPropertyRelative("presentation");

            var anyLocationProp = filterProp != null ? filterProp.FindPropertyRelative("anyLocation") : null;
            var conditionsProp = matchProp != null ? matchProp.FindPropertyRelative("conditions") : null;
            var variantsProp = presentationProp != null ? presentationProp.FindPropertyRelative("variants") : null;

            float height = EditorGUIUtility.singleLineHeight; // header label
            height += VSpace + EditorGUIUtility.singleLineHeight; // anyLocation

            if (anyLocationProp != null && !anyLocationProp.boolValue)
                height += VSpace + EditorGUIUtility.singleLineHeight; // location dropdown

            height += VSpace + EditorGUIUtility.singleLineHeight; // priority

            if (conditionsProp != null)
                height += VSpace + EditorGUI.GetPropertyHeight(conditionsProp, includeChildren: true);

            if (variantsProp != null)
                height += VSpace + EditorGUI.GetPropertyHeight(variantsProp, includeChildren: true);

            return height + VSpace;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
                return;

            EditorGUI.BeginProperty(position, label, property);

            var filterProp = property.FindPropertyRelative("filter");
            var matchProp = property.FindPropertyRelative("match");
            var presentationProp = property.FindPropertyRelative("presentation");

            var anyLocationProp = filterProp != null ? filterProp.FindPropertyRelative("anyLocation") : null;
            var locationIdProp = filterProp != null ? filterProp.FindPropertyRelative("locationId") : null;
            var priorityProp = filterProp != null ? filterProp.FindPropertyRelative("priority") : null;
            var conditionsProp = matchProp != null ? matchProp.FindPropertyRelative("conditions") : null;
            var variantsProp = presentationProp != null ? presentationProp.FindPropertyRelative("variants") : null;

            var contentRect = EditorGUI.IndentedRect(position);
            float y = contentRect.y;

            // Draw a stable header row; avoids nested foldout height glitches in array elements.
            var headerRect = new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(headerRect, label);
            y += EditorGUIUtility.singleLineHeight + VSpace;

            EditorGUI.indentLevel++;
            if (anyLocationProp != null)
            {
                var anyLocationRect = new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(anyLocationRect, anyLocationProp, new GUIContent("Default (Any Location)"));
                y += EditorGUIUtility.singleLineHeight + VSpace;
            }

            if (locationIdProp != null && anyLocationProp != null && !anyLocationProp.boolValue)
            {
                DrawLocationDropdown(new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight), locationIdProp);
                y += EditorGUIUtility.singleLineHeight + VSpace;
            }

            if (priorityProp != null)
            {
                var priorityRect = new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(priorityRect, priorityProp);
                y += EditorGUIUtility.singleLineHeight + VSpace;
            }

            if (conditionsProp != null)
            {
                var conditionsHeight = EditorGUI.GetPropertyHeight(conditionsProp, includeChildren: true);
                var conditionsRect = new Rect(contentRect.x, y, contentRect.width, conditionsHeight);
                EditorGUI.PropertyField(conditionsRect, conditionsProp, includeChildren: true);
                y += conditionsHeight + VSpace;
            }

            if (variantsProp != null)
            {
                var variantsHeight = EditorGUI.GetPropertyHeight(variantsProp, includeChildren: true);
                var variantsRect = new Rect(contentRect.x, y, contentRect.width, variantsHeight);
                EditorGUI.PropertyField(variantsRect, variantsProp, includeChildren: true);
            }
            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        private static void DrawLocationDropdown(Rect rect, SerializedProperty locationIdProp)
        {
            var options = LocationOptionProvider.GetOptions();
            var labels = LocationOptionProvider.GetLabels(options);

            int selectedIndex = 0;
            var currentId = locationIdProp != null ? locationIdProp.stringValue : string.Empty;
            if (!string.IsNullOrWhiteSpace(currentId))
            {
                for (int i = 0; i < options.Count; i++)
                {
                    if (string.Equals(options[i], currentId.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            int newIndex = EditorGUI.Popup(rect, "Location", selectedIndex, labels);
            if (locationIdProp != null && newIndex >= 0 && newIndex < options.Count)
                locationIdProp.stringValue = options[newIndex];
        }

        private static class LocationOptionProvider
        {
            private static List<string> cachedIds;
            private static double lastRefreshTime;

            public static List<string> GetOptions()
            {
                if (cachedIds != null && EditorApplication.timeSinceStartup - lastRefreshTime < 2.0)
                    return cachedIds;

                var ids = new List<string> { string.Empty };
                var guids = AssetDatabase.FindAssets("t:BattleLocationData");
                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var location = AssetDatabase.LoadAssetAtPath<BattleLocationData>(path);
                    if (location == null)
                        continue;

                    string id = !string.IsNullOrWhiteSpace(location.id)
                        ? location.id.Trim()
                        : location.name;

                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    if (!ids.Contains(id))
                        ids.Add(id);
                }

                // Also include dungeon location ids (e.g. dld_farm) so authors can filter by source world location.
                var dungeonGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/GameData/Dungeon/Locations" });
                for (int i = 0; i < dungeonGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(dungeonGuids[i]);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset == null)
                        continue;

                    var so = new SerializedObject(asset);
                    var idProp = so.FindProperty("id");
                    if (idProp == null || idProp.propertyType != SerializedPropertyType.String)
                        continue;

                    var id = idProp.stringValue;
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    id = id.Trim();
                    if (!ids.Contains(id))
                        ids.Add(id);
                }

                ids.Sort((a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
                if (ids.Count == 0 || ids[0] != string.Empty)
                    ids.Insert(0, string.Empty);

                cachedIds = ids;
                lastRefreshTime = EditorApplication.timeSinceStartup;
                return cachedIds;
            }

            public static string[] GetLabels(List<string> options)
            {
                if (options == null || options.Count == 0)
                    return new[] { "default" };

                var labels = new string[options.Count];
                for (int i = 0; i < options.Count; i++)
                    labels[i] = string.IsNullOrWhiteSpace(options[i]) ? "default" : options[i];

                return labels;
            }
        }
    }
}
