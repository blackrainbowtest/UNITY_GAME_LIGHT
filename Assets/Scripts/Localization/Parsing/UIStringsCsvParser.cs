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


        string[] lines = csvText.Split('\n');
        string[] languages = null;
        bool headerParsed = false;
        var usedKeys = new HashSet<string>();
        LocalizedUIString currentEntry = null;
        List<string> currentValues = null;
        int expectedValueLines = 0;
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            string rawLine = lines[lineIndex].TrimEnd('\r','\n');
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                lineIndex++;
                continue;
            }
            if (rawLine.StartsWith("*"))
            {
                string[] cells = rawLine.Substring(1).Split(';');
                if (!headerParsed)
                {
                    languages = cells.Length > 1 ? cells[1..] : DefaultLanguages;
                    expectedValueLines = languages.Length;
                    headerParsed = true;
                    lineIndex++;
                    continue;
                }
                // Save previous entry if present
                if (currentEntry != null && currentValues != null)
                {
                    for (int i = 0; i < languages.Length; i++)
                    {
                        string value = i < currentValues.Count ? currentValues[i].Replace("\\n", "\n").Trim() : string.Empty;
                        currentEntry.AddEntry(languages[i], value);
                    }
                    result.Add(currentEntry);
                }
                string key = cells[0].Trim();
                if (string.IsNullOrEmpty(key))
                {
                    lineIndex++;
                    continue;
                }
                if (!usedKeys.Add(key))
                {
                    error = $"Duplicate key '{key}' found in CSV.";
                    return false;
                }
                currentEntry = new LocalizedUIString(key);
                currentValues = new List<string>();
                lineIndex++;
                // Collect value lines
                int valueLines = 0;
                while (valueLines < expectedValueLines && lineIndex < lines.Length)
                {
                    string valueLine = lines[lineIndex].TrimEnd('\r','\n');
                    if (valueLine.StartsWith("*")) break;
                    currentValues.Add(valueLine);
                    valueLines++;
                    lineIndex++;
                }
            }
            else
            {
                lineIndex++;
            }
        }
        // Add last entry if present
        if (currentEntry != null && currentValues != null)
        {
            for (int i = 0; i < languages.Length; i++)
            {
                string value = i < currentValues.Count ? currentValues[i].Replace("\\n", "\n").Trim() : string.Empty;
                currentEntry.AddEntry(languages[i], value);
            }
            result.Add(currentEntry);
        }

        if (result.Count == 0)
        {
            error = "No localization entries found in CSV.";
            return false;
        }

        return true;
    }
}
