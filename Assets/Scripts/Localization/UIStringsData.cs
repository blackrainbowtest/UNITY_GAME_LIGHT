/* ************************************************************************** */
/*                                                                            */
/*   File: Assets/Scripts/Data/Localization/UIStringsData.cs                  */
/*                                                        /\_/\               */
/*                                                       ( •.• )              */
/*   By: unluckydungeonadventure.gmail.com                > ^ <               */
/*                                                                            */
/*   Created: 2026/01/08 13:48:23 by UDA                                      */
/*   Updated: 2026/01/08 13:48:23 by UDA                                      */
/*                                                                            */
/* ************************************************************************** */

using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

[CreateAssetMenu(fileName = "UIStrings", menuName = "Game/Localization/UI Strings")]
public class UIStringsData : ScriptableObject
{
    [SerializeField] private List<LocalizedUIString> strings = new();

    [NonSerialized] private Dictionary<string, LocalizedUIString> cacheByKey;

    [Header("Editor Import")]
    [SerializeField, HideInInspector]
    [Tooltip("CSV file name without extension (e.g. 'ui_battle')")]
    public string sourceCsvName;

    public string Get(string key, string languageCode)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        EnsureCache();
        if (cacheByKey != null && cacheByKey.TryGetValue(key, out var entry) && entry != null)
            return entry.Get(languageCode);

        return key;
    }

    public bool TryGet(string key, string languageCode, out string value)
    {
        value = null;

        if (string.IsNullOrEmpty(key))
            return false;

        EnsureCache();
        if (cacheByKey != null && cacheByKey.TryGetValue(key, out var entry) && entry != null)
        {
            value = entry.Get(languageCode);
            return true;
        }

        return false;
    }

    private void OnEnable()
    {
        BuildCache();
    }

    private void OnValidate()
    {
        BuildCache();
    }

    private void EnsureCache()
    {
        if (cacheByKey == null)
            BuildCache();
    }

    private void BuildCache()
    {
        if (cacheByKey == null)
            cacheByKey = new Dictionary<string, LocalizedUIString>(StringComparer.Ordinal);
        else
            cacheByKey.Clear();

        if (strings == null)
            return;

        for (int i = 0; i < strings.Count; i++)
        {
            var entry = strings[i];
            if (entry == null)
                continue;

            var entryKey = entry.Key;
            if (string.IsNullOrEmpty(entryKey))
                continue;

            if (!cacheByKey.ContainsKey(entryKey))
                cacheByKey.Add(entryKey, entry);
        }
    }

    public void CopyKeysTo(List<string> destination)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        destination.Clear();
        for (int i = 0; i < strings.Count; i++)
        {
            if (strings[i] == null)
                continue;

            string key = strings[i].Key;
            if (!string.IsNullOrEmpty(key))
                destination.Add(key);
        }
    }

#if UNITY_EDITOR
    public bool EditorReimportFromCsv(TextAsset csv, out string error)
    {
        error = null;

        if (csv == null)
        {
            error = "CSV TextAsset is null.";
            return false;
        }

        // Keep the name in sync so other workflows (like the inspector button)
        // can still reimport by name.
        string csvPath = AssetDatabase.GetAssetPath(csv);
        if (!string.IsNullOrWhiteSpace(csvPath))
            sourceCsvName = Path.GetFileNameWithoutExtension(csvPath);
        else if (!string.IsNullOrWhiteSpace(csv.name))
            sourceCsvName = csv.name;

        if (!UIStringsCsvParser.TryParse(csv.text, out var parsed, out error))
            return false;

        return EditorReplaceAll(parsed, out error);
    }

    public bool EditorSetSourceCsvName(string csvName, out string error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(csvName))
        {
            error = "CSV name is null or empty.";
            return false;
        }

        sourceCsvName = csvName.Trim();
        EditorUtility.SetDirty(this);
        return true;
    }

    public bool EditorReimportFromCsv(
        string csvRootPath,
        out string error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(sourceCsvName))
        {
            error = "Source CSV name is not set.";
            return false;
        }

        string csvPath = $"{csvRootPath}/{sourceCsvName}.csv";
        var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);

        if (csv == null)
        {
            error = $"CSV not found at path: {csvPath}";
            return false;
        }

        if (!UIStringsCsvParser.TryParse(csv.text, out var parsed, out error))
            return false;

        return EditorReplaceAll(parsed, out error);
    }

    private bool EditorReplaceAll(
        IReadOnlyList<LocalizedUIString> newStrings,
        out string error)
    {
        error = null;

        if (newStrings == null)
        {
            error = "Imported data is null.";
            return false;
        }

        var keys = new HashSet<string>();
        foreach (var s in newStrings)
        {
            if (!keys.Add(s.Key))
            {
                error = $"Duplicate key '{s.Key}' in imported data.";
                return false;
            }
        }

        strings.Clear();
        strings.AddRange(newStrings);
        BuildCache();
        EditorUtility.SetDirty(this);
        return true;
    }
#endif
}

[Serializable]
public class LocalizedUIStringEntry
{
    public string languageCode;

    [TextArea(3, 10)]
    public string text;

    public LocalizedUIStringEntry(string languageCode, string text)
    {
        this.languageCode = languageCode;
        this.text = text;
    }
}

[Serializable]
public class LocalizedUIString
{
    [SerializeField] private string key;
    [SerializeField] private List<LocalizedUIStringEntry> entries = new();

    public string Key => key;

    public LocalizedUIString(string key)
    {
        this.key = key;
    }

    public void AddEntry(string languageCode, string text)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].languageCode == languageCode)
            {
                entries[i] = new LocalizedUIStringEntry(languageCode, text);
                return;
            }
        }

        entries.Add(new LocalizedUIStringEntry(languageCode, text));
    }

    public string Get(string languageCode)
    {
        if (entries.Count == 0)
            return key;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].languageCode == languageCode)
                return entries[i].text;
        }

        return entries[0].text;
    }
}
