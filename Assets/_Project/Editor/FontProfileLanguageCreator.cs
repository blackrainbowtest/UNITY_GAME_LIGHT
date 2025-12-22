using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class FontProfileLanguageCreator : EditorWindow
{
    private string languageCode = "en";

    [MenuItem("Tools/Create FontProfile Language")]
    public static void ShowWindow()
    {
        GetWindow<FontProfileLanguageCreator>("FontProfile Language Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Создать новый язык для FontProfile", EditorStyles.boldLabel);
        languageCode = EditorGUILayout.TextField("Language Code", languageCode);

        if (GUILayout.Button("Создать"))
        {
            CreateLanguage();
        }
    }

    void CreateLanguage()
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            EditorUtility.DisplayDialog("Ошибка", "Введите код языка!", "OK");
            return;
        }

        string folderPath = $"Assets/_Project/Localization/Fonts/{languageCode}";
        string assetPath = $"{folderPath}/FontProfile.asset";

        if (AssetDatabase.IsValidFolder(folderPath) || File.Exists(assetPath))
        {
            EditorUtility.DisplayDialog("Внимание", $"Язык '{languageCode}' уже существует!", "OK");
            return;
        }

        Directory.CreateDirectory(folderPath);
        AssetDatabase.Refresh();

        var fontProfile = ScriptableObject.CreateInstance<FontProfile>();
        AssetDatabase.CreateAsset(fontProfile, assetPath);
        AssetDatabase.SaveAssets();

        // Найти FontManager в сцене
        var fontManager = FindObjectOfType<FontManager>();
        if (fontManager != null)
        {
            var profiles = fontManager.GetType().GetField("fontProfiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(fontManager) as System.Collections.IList;

            var entryType = typeof(FontManager).GetNestedType("LanguageFontProfile");
            var entry = System.Activator.CreateInstance(entryType);
            entryType.GetField("languageCode").SetValue(entry, languageCode);
            entryType.GetField("fontProfile").SetValue(entry, fontProfile);

            profiles.Add(entry);
            EditorUtility.SetDirty(fontManager);
            AssetDatabase.SaveAssets();
        }
        else
        {
            EditorUtility.DisplayDialog("Внимание", "FontManager не найден в сцене! Добавьте профиль вручную.", "OK");
        }

        EditorUtility.DisplayDialog("Готово", $"Язык '{languageCode}' и FontProfile созданы.", "OK");
    }
}
