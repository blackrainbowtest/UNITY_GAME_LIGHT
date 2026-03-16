using System;
using System.Collections.Generic;
using UDA2.SaveSystem.Guild;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GuildMobKillAmount))]
public sealed class GuildMobKillAmountDrawer : PropertyDrawer
{
    private static UnityEngine.Object[] _enemyAssets;
    private static string[] _enemyIds;
    private static string[] _enemyLabels;
    private static double _lastRefreshTime;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var enemyProp = property.FindPropertyRelative("enemy");
        var enemyIdProp = property.FindPropertyRelative("enemyId");
        var amountProp = property.FindPropertyRelative("amount");

        RefreshCacheIfNeeded();

        var rowRect = EditorGUI.PrefixLabel(position, label);
        float leftWidth = Mathf.Max(140f, rowRect.width * 0.72f);
        var enemyRect = new Rect(rowRect.x, rowRect.y, leftWidth - 4f, rowRect.height);
        var amountRect = new Rect(rowRect.x + leftWidth, rowRect.y, rowRect.width - leftWidth, rowRect.height);

        DrawEnemyPopup(enemyRect, enemyProp, enemyIdProp);
        amountProp.intValue = Mathf.Max(1, EditorGUI.IntField(amountRect, amountProp.intValue));
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    private static void DrawEnemyPopup(Rect rect, SerializedProperty enemyProp, SerializedProperty enemyIdProp)
    {
        if (_enemyIds == null || _enemyIds.Length == 0)
        {
            EditorGUI.ObjectField(rect, enemyProp, GUIContent.none);
            return;
        }

        string currentId = ResolveCurrentEnemyId(enemyProp, enemyIdProp);
        int index = Array.IndexOf(_enemyIds, currentId);

        var labelsToUse = _enemyLabels;
        var idsToUse = _enemyIds;
        var assetsToUse = _enemyAssets;

        if (index < 0 && !string.IsNullOrWhiteSpace(currentId))
        {
            labelsToUse = new string[_enemyLabels.Length + 1];
            idsToUse = new string[_enemyIds.Length + 1];
            assetsToUse = new UnityEngine.Object[_enemyAssets.Length + 1];

            labelsToUse[0] = $"<missing> {currentId}";
            idsToUse[0] = currentId;
            assetsToUse[0] = enemyProp.objectReferenceValue;

            Array.Copy(_enemyLabels, 0, labelsToUse, 1, _enemyLabels.Length);
            Array.Copy(_enemyIds, 0, idsToUse, 1, _enemyIds.Length);
            Array.Copy(_enemyAssets, 0, assetsToUse, 1, _enemyAssets.Length);
            index = 0;
        }

        if (index < 0)
            index = 0;

        int newIndex = EditorGUI.Popup(rect, index, labelsToUse);
        if (newIndex < 0 || newIndex >= idsToUse.Length)
            return;

        enemyIdProp.stringValue = idsToUse[newIndex] ?? string.Empty;
        enemyProp.objectReferenceValue = assetsToUse[newIndex];
    }

    private static string ResolveCurrentEnemyId(SerializedProperty enemyProp, SerializedProperty enemyIdProp)
    {
        if (enemyProp != null && enemyProp.objectReferenceValue != null)
        {
            var id = GetEnemyId(enemyProp.objectReferenceValue);
            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        return enemyIdProp != null ? (enemyIdProp.stringValue ?? string.Empty) : string.Empty;
    }

    private static void RefreshCacheIfNeeded()
    {
        if (_enemyIds != null && EditorApplication.timeSinceStartup - _lastRefreshTime < 1.0d)
            return;

        _lastRefreshTime = EditorApplication.timeSinceStartup;

        var assets = new List<UnityEngine.Object> { null };
        var ids = new List<string> { string.Empty };
        var labels = new List<string> { "<none>" };

        string[] guids = AssetDatabase.FindAssets("t:EnemyData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var enemyAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (enemyAsset == null)
                continue;

            string id = GetEnemyId(enemyAsset);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            string enemyName = GetEnemyName(enemyAsset);
            if (string.IsNullOrWhiteSpace(enemyName))
                enemyName = id;

            assets.Add(enemyAsset);
            ids.Add(id);
            labels.Add($"{enemyName} ({id})");
        }

        if (ids.Count > 2)
        {
            var entries = new List<(UnityEngine.Object asset, string id, string label)>();
            for (int i = 1; i < ids.Count; i++)
                entries.Add((assets[i], ids[i], labels[i]));

            entries.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase));

            for (int i = 1; i < ids.Count; i++)
            {
                assets[i] = entries[i - 1].asset;
                ids[i] = entries[i - 1].id;
                labels[i] = entries[i - 1].label;
            }
        }

        _enemyAssets = assets.ToArray();
        _enemyIds = ids.ToArray();
        _enemyLabels = labels.ToArray();
    }

    private static string GetEnemyId(UnityEngine.Object enemyAsset)
    {
        if (enemyAsset == null)
            return string.Empty;

        var so = new SerializedObject(enemyAsset);
        var idProp = so.FindProperty("id");
        if (idProp == null || string.IsNullOrWhiteSpace(idProp.stringValue))
            return string.Empty;

        return idProp.stringValue.Trim();
    }

    private static string GetEnemyName(UnityEngine.Object enemyAsset)
    {
        if (enemyAsset == null)
            return string.Empty;

        var so = new SerializedObject(enemyAsset);
        var nameProp = so.FindProperty("enemyName");
        if (nameProp != null && !string.IsNullOrWhiteSpace(nameProp.stringValue))
            return nameProp.stringValue.Trim();

        return enemyAsset.name;
    }
}
