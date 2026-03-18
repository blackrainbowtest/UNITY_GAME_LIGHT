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
    public int playerGold;
}

public static class SaveSlotsManager
{
    private static string SavesDir => Path.Combine(Application.persistentDataPath, "saves");
    public static int CurrentRuntimeSlotId { get; private set; } = 0;

    public static int GetRuntimeSaveSlotOrAutosave()
    {
        return CurrentRuntimeSlotId > 0 ? CurrentRuntimeSlotId : 0;
    }

    private static string GetSlotPath(int slotId)
    {
        if (slotId == 0) return Path.Combine(SavesDir, "slot_auto.json");
        return Path.Combine(SavesDir, $"slot_{slotId:D2}.json");
    }

    public static void DeleteAutosave()
    {
        var path = GetSlotPath(0);
        if (File.Exists(path))
            File.Delete(path);
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
            playerLevel = save.player.level,
            playerGold = save.inventory != null ? save.inventory.gold : 0,
        };
    }

    public static void SaveToSlot(int slotId, SaveData data, bool rememberAsCurrentRuntimeSlot = true)
    {
        if (slotId < 0 || slotId > 10) throw new ArgumentOutOfRangeException();

        if (data == null)
        {
            Debug.LogError($"[SaveSlotsManager] SaveToSlot failed: SaveData is null (slotId={slotId}).");
            return;
        }

        data = SaveDataMigration.Apply(data, out _, logChanges: false);
        if (data == null || data.meta == null)
        {
            Debug.LogError($"[SaveSlotsManager] SaveToSlot failed: SaveDataMigration returned invalid data (slotId={slotId}).");
            return;
        }

        string path = GetSlotPath(slotId);
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        // Friendly human-readable time (no ISO 'T'/'Z').
        data.meta.saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        if (rememberAsCurrentRuntimeSlot && slotId > 0)
            CurrentRuntimeSlotId = slotId;
    }

    public static SaveData LoadFromSlot(int slotId)
    {
        var path = GetSlotPath(slotId);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        var save = JsonUtility.FromJson<SaveData>(json);

        save = SaveDataMigration.Apply(save, out var didMigrate, logChanges: true);
        if (didMigrate && save != null)
            PersistMigratedInPlace(path, save);

        if (save != null && slotId > 0)
            CurrentRuntimeSlotId = slotId;

        return save;
    }

    private static void PersistMigratedInPlace(string path, SaveData save)
    {
        if (string.IsNullOrEmpty(path) || save == null)
            return;

        try
        {
            var json = JsonUtility.ToJson(save, true);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSlotsManager] Failed to persist migrated save at '{path}': {ex.Message}");
        }
    }
}
