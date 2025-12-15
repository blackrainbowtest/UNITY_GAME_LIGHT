using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIStrings", menuName = "Game/Localization/UI Strings")]
public class UIStringsData : ScriptableObject
{
    public List<LocalizedUIString> strings = new();

    public string Get(string key, string languageCode)
    {
        foreach (var s in strings)
        {
            if (s.key == key)
                return s.Get(languageCode);
        }
        return key; // fallback: возвращаем ключ, если не найдено
    }
}

[Serializable]
public class LocalizedUIString
{
    public string key; // например, "new_game", "load_game"
    public List<LocalizedTextEntry> entries = new();

    public string Get(string languageCode)
    {
        foreach (var entry in entries)
        {
            if (entry.languageCode == languageCode)
                return entry.text;
        }
        return entries.Count > 0 ? entries[0].text : key;
    }
}
