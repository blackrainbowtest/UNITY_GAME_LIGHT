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
#endif

[CreateAssetMenu(fileName = "UIStrings", menuName = "Game/Localization/UI Strings")]
public class UIStringsData : ScriptableObject
{
    [SerializeField] private List<LocalizedUIString> strings = new();

#if UNITY_EDITOR
    [Header("Editor Import")]
    [SerializeField]
    [Tooltip("CSV file name without extension (e.g. 'ui_battle')")]
    private string sourceCsvName;
#endif

    public string Get(string key, string languageCode)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        for (int i = 0; i < strings.Count; i++)
        {
            if (strings[i].Key == key)
                return strings[i].Get(languageCode);
        }

        return key;
    }

#if UNITY_EDITOR
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
        EditorUtility.SetDirty(this);
        return true;
    }
#endif
}

[Serializable]
public class LocalizedUIString
{
    [SerializeField] private string key;
    [SerializeField] private List<LocalizedTextEntry> entries = new();

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
                entries[i] = new LocalizedTextEntry(languageCode, text);
                return;
            }
        }

        entries.Add(new LocalizedTextEntry(languageCode, text));
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

[Serializable]
public struct LocalizedTextEntry
{
    public string languageCode;
    public string text;

    public LocalizedTextEntry(string languageCode, string text)
    {
        this.languageCode = languageCode;
        this.text = text;
    }
}
