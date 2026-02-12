using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LocationUpgradeTableAsset.ResourceCost))]
public sealed class LocationUpgradeResourceCostDrawer : PropertyDrawer
{
    private static string[] _ids;
    private static string[] _labels;
    private static double _lastRefreshTime;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var itemIdProp = property.FindPropertyRelative("itemId");
        var amountProp = property.FindPropertyRelative("amount");

        RefreshCacheIfNeeded();

        var rowRect = EditorGUI.PrefixLabel(position, label);
        float leftWidth = Mathf.Max(120f, rowRect.width * 0.72f);
        var idRect = new Rect(rowRect.x, rowRect.y, leftWidth - 4f, rowRect.height);
        var amountRect = new Rect(rowRect.x + leftWidth, rowRect.y, rowRect.width - leftWidth, rowRect.height);

        DrawItemIdPopup(idRect, itemIdProp);
        amountProp.intValue = Mathf.Max(1, EditorGUI.IntField(amountRect, amountProp.intValue));
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    private static void DrawItemIdPopup(Rect rect, SerializedProperty itemIdProp)
    {
        if (_ids == null || _ids.Length == 0)
        {
            itemIdProp.stringValue = EditorGUI.TextField(rect, itemIdProp.stringValue ?? string.Empty);
            return;
        }

        string current = itemIdProp.stringValue ?? string.Empty;
        int index = Array.IndexOf(_ids, current);

        string[] labelsToUse = _labels;
        string[] idsToUse = _ids;

        if (index < 0 && !string.IsNullOrWhiteSpace(current))
        {
            labelsToUse = new string[_labels.Length + 1];
            idsToUse = new string[_ids.Length + 1];

            labelsToUse[0] = $"<missing> {current}";
            idsToUse[0] = current;

            Array.Copy(_labels, 0, labelsToUse, 1, _labels.Length);
            Array.Copy(_ids, 0, idsToUse, 1, _ids.Length);
            index = 0;
        }

        if (index < 0)
            index = 0;

        int newIndex = EditorGUI.Popup(rect, index, labelsToUse);
        if (newIndex >= 0 && newIndex < idsToUse.Length)
            itemIdProp.stringValue = idsToUse[newIndex];
    }

    private static void RefreshCacheIfNeeded()
    {
        // Refresh occasionally to avoid heavy AssetDatabase queries every repaint.
        if (_ids != null && EditorApplication.timeSinceStartup - _lastRefreshTime < 1.0d)
            return;

        _lastRefreshTime = EditorApplication.timeSinceStartup;

        var ids = new List<string> { string.Empty };
        var labels = new List<string> { "<none>" };

        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var item = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (item == null)
                continue;

            var so = new SerializedObject(item);
            var idProp = so.FindProperty("id");
            var displayNameProp = so.FindProperty("displayName");

            string id = idProp != null ? idProp.stringValue : string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                continue;

            ids.Add(id);
            string displayName = displayNameProp != null ? displayNameProp.stringValue : string.Empty;
            string display = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            labels.Add($"{display} ({id})");
        }

        // Keep deterministic inspector order.
        if (ids.Count > 2)
        {
            var entries = new List<(string id, string label)>();
            for (int i = 1; i < ids.Count; i++)
                entries.Add((ids[i], labels[i]));

            entries.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase));

            for (int i = 1; i < ids.Count; i++)
            {
                ids[i] = entries[i - 1].id;
                labels[i] = entries[i - 1].label;
            }
        }

        _ids = ids.ToArray();
        _labels = labels.ToArray();
    }
}
