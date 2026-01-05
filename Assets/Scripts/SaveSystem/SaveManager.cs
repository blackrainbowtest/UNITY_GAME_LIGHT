using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void SaveGame()
    {
        var data = GameState.Instance.CurrentSave;
        data.meta.saveTime = System.DateTime.UtcNow.ToString("o");
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public static bool HasSave() => File.Exists(SavePath);

    public static void LoadGame()
    {
        if (!HasSave()) return;
        var json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json);
        GameState.Instance.CurrentSave = data;
    }
}
