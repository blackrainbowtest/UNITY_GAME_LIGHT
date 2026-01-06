using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveMeta
{
    public string version;
    public string saveTime;
    public int playTimeSeconds;
    public int playerLevel;
}

public static class SaveSlotsManager
{
    private static string SavesDir => Path.Combine(Application.persistentDataPath, "saves");
    private static string GetSlotPath(int slotId)
    {
        if (slotId == 0) return Path.Combine(SavesDir, "slot_auto.json");
        return Path.Combine(SavesDir, $"slot_{slotId:D2}.json");
    }

    public static bool HasSave(int slotId)
    {
        return File.Exists(GetSlotPath(slotId));
    }

    public static void DeleteSlot(int slotId)
    {
        if (slotId == 0) return; // автосейв нельзя удалить
        var path = GetSlotPath(slotId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static SaveMeta GetMeta(int slotId)
    {
        var path = GetSlotPath(slotId);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        // Only parse meta part
        var save = JsonUtility.FromJson<SaveData>(json);
        if (save == null || save.meta == null || save.player == null) return null;
        return new SaveMeta {
            version = save.meta.version,
            saveTime = save.meta.saveTime,
            playTimeSeconds = save.meta.playTimeSeconds,
            playerLevel = save.player.level
        };
    }

    public static void SaveToSlot(int slotId, SaveData data)
    {
        if (slotId < 0 || slotId > 10) throw new ArgumentOutOfRangeException();
        string path = GetSlotPath(slotId);
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        data.meta.saveTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public static SaveData LoadFromSlot(int slotId)
    {
        var path = GetSlotPath(slotId);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }
}
