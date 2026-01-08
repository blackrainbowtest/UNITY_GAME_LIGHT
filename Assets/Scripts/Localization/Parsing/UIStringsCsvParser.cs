/* ************************************************************************** */
/*                                                                            */
/*   File: Assets/Scripts/Localization/Parsing/UIStringsCsvParser.cs          */
/*                                                        /\_/\               */
/*                                                       ( •.• )              */
/*   By: unluckydungeonadventure.gmail.com                > ^ <               */
/*                                                                            */
/*   Created: 2026/01/08 16:19:35 by UDA                                      */
/*   Updated: 2026/01/08 16:19:35 by UDA                                      */
/*                                                                            */
/* ************************************************************************** */

using System;
using System.Collections.Generic;

public static class UIStringsCsvParser
{
    private static readonly string[] DefaultLanguages = { "ru", "en", "fr" };

    public static bool TryParse(
        string csvText,
        out List<LocalizedUIString> result,
        out string error)
    {
        result = new List<LocalizedUIString>();
        error = null;

        if (string.IsNullOrWhiteSpace(csvText))
        {
            error = "CSV text is empty.";
            return false;
        }

        string[] languages = null;
        bool headerParsed = false;
        var usedKeys = new HashSet<string>();

        string[] lines = csvText.Split('\n');

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            if (!line.StartsWith("*"))
                continue;

            string[] cells = line.Substring(1).Split(';');

            if (!headerParsed)
            {
                languages = cells.Length > 1
                    ? cells[1..]
                    : DefaultLanguages;

                headerParsed = true;
                continue;
            }

            string key = cells[0].Trim();
            if (string.IsNullOrEmpty(key))
                continue;

            if (!usedKeys.Add(key))
            {
                error = $"Duplicate key '{key}' found in CSV.";
                return false;
            }

            var entry = new LocalizedUIString(key);

            for (int i = 0; i < languages.Length; i++)
            {
                string value = i + 1 < cells.Length
                    ? cells[i + 1].Replace("\\n", "\n").Trim()
                    : string.Empty;

                entry.AddEntry(languages[i], value);
            }

            result.Add(entry);
        }

        if (result.Count == 0)
        {
            error = "No localization entries found in CSV.";
            return false;
        }

        return true;
    }
}
