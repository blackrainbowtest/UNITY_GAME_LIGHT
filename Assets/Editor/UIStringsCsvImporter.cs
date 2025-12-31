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
        string text = csv.text;
        int pos = 0;
        int length = text.Length;
        string currentKey = null;
        List<string> currentValues = null;
        string[] langCodes = null;
        bool firstLineParsed = false;

        void AddCurrent()
        {
            if (!string.IsNullOrEmpty(currentKey) && currentValues != null && langCodes != null)
            {
                var uiString = new LocalizedUIString { key = currentKey, entries = new List<LocalizedTextEntry>() };
                for (int i = 0; i < langCodes.Length; i++)
                {
                    string value = (i < currentValues.Count) ? currentValues[i] : "";
                    uiString.entries.Add(new LocalizedTextEntry { languageCode = langCodes[i], text = value });
                }
                asset.strings.Add(uiString);
            }
        }

        while (pos < length)
        {
            // Пропуск пробелов, табов, новых строк
            while (pos < length && (text[pos] == ' ' || text[pos] == '\t' || text[pos] == '\r' || text[pos] == '\n')) pos++;

            // Парсим первую строку с языковыми кодами
            if (!firstLineParsed && pos < length && text[pos] == '*')
            {
                pos++; // Пропустить '*'
                int keyStart = pos;
                while (pos < length && text[pos] != ';') pos++;
                int keyEnd = pos;
                string key = text.Substring(keyStart, keyEnd - keyStart).Trim();
                pos++; // Пропустить ';'
                // Парсим языковые коды
                var codes = key.Split(';');
                if (codes.Length > 1)
                {
                    langCodes = codes.Skip(1).Select(s => s.Trim()).ToArray();
                }
                else
                {
                    langCodes = new string[] { "ru", "en", "fr" };
                }
                firstLineParsed = true;
                currentKey = null;
                currentValues = null;
                continue;
            }
            if (pos < length && text[pos] == '*')
            {
                // Новый ключ
                pos++; // Пропустить '*'
                int keyStart = pos;
                while (pos < length && text[pos] != ';') pos++;
                int keyEnd = pos;
                string key = text.Substring(keyStart, keyEnd - keyStart).Trim();
                pos++; // Пропустить ';'
                AddCurrent();
                currentKey = key;
                currentValues = new List<string>();
                continue;
            }
            else if (pos < length)
            {
                // Значение (до ;)
                int valueStart = pos;
                while (pos < length && text[pos] != ';') pos++;
                int valueEnd = pos;
                string value = text.Substring(valueStart, valueEnd - valueStart).Trim();
                pos++; // Пропустить ';'
                // Поддержка \n как переноса строки
                value = value.Replace("\\n", "\n");
                if (currentValues != null)
                    currentValues.Add(value);
                continue;
            }
        }
        AddCurrent();
    }
}
