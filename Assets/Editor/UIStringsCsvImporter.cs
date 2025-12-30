using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class UIStringsCsvImporter : EditorWindow
{
    private TextAsset csvFile;
    private UIStringsData targetAsset;

    [MenuItem("Tools/Localization/Import UI Strings from CSV")]
    public static void ShowWindow()
    {
        GetWindow<UIStringsCsvImporter>("UI Strings CSV Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("CSV to UIStringsData Importer", EditorStyles.boldLabel);
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        targetAsset = (UIStringsData)EditorGUILayout.ObjectField("Target UIStringsData", targetAsset, typeof(UIStringsData), false);

        if (GUILayout.Button("Import") && csvFile != null && targetAsset != null)
        {
            ImportCsvToUIStringsData(csvFile, targetAsset);
            EditorUtility.SetDirty(targetAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("UIStringsData updated from CSV!");
        }
    }

    private void ImportCsvToUIStringsData(TextAsset csv, UIStringsData asset)
    {
        asset.strings.Clear();
        var lines = csv.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return;
        var header = lines[0].Split(';');
        var langCodes = header.Skip(1).ToArray();

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue; // пропуск строк только с пробелами и \n
            line = line.Trim();
            if (line.StartsWith("#") || line.StartsWith("//")) continue; // поддержка комментариев

            var row = line.Split(';');
            if (row.Length < 1) continue;
            var key = row[0].Trim();
            if (string.IsNullOrEmpty(key)) continue;
            var uiString = new LocalizedUIString { key = key, entries = new List<LocalizedTextEntry>() };
            for (int j = 0; j < langCodes.Length; j++)
            {
                string lang = langCodes[j].Trim();
                string text = (j + 1 < row.Length) ? row[j + 1].Trim() : "";
                // Поддержка \n как переноса строки
                text = text.Replace("\\n", "\n");
                if (string.IsNullOrEmpty(text)) text = key; // fallback: если пропущено, ставим ключ
                uiString.entries.Add(new LocalizedTextEntry { languageCode = lang, text = text });
            }
            asset.strings.Add(uiString);
        }
        // TODO: заменить на полноценный CSV-парсер с поддержкой кавычек и многострочных ячеек
    }
}
