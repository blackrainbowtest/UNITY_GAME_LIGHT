using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizedText
{
    [SerializeField]
    private List<LocalizedTextEntry> entries = new();

    public string Get(string languageCode)
    {
        foreach (var entry in entries)
        {
            if (entry.languageCode == languageCode)
                return entry.text;
        }
        // fallback
        return entries.Count > 0 ? entries[0].text : string.Empty;
    }
}

[Serializable]
public class LocalizedTextEntry
{
    public string languageCode; // "ru", "en", "fr"
    [TextArea(3, 10)]
    public string text;
}
