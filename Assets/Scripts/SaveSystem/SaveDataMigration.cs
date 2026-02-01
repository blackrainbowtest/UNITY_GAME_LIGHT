using UnityEngine;

public static class SaveDataMigration
{
    /// <summary>
    /// Ensures loaded SaveData is structurally valid and contains sane defaults for newly added fields.
    /// Call this right after deserialization.
    /// </summary>
    public static SaveData Apply(SaveData save)
    {
        if (save == null)
            return null;

        var didMigrate = false;
        var changes = string.Empty;

        void Mark(string message)
        {
            didMigrate = true;
            if (string.IsNullOrEmpty(changes))
                changes = message;
            else
                changes += "; " + message;
        }

        if (save.meta == null) { save.meta = new SaveData.Meta(); Mark("meta initialized"); }
        if (save.player == null) { save.player = new SaveData.Player(); Mark("player initialized"); }
        if (save.player.stats == null) { save.player.stats = new SaveData.Stats(); Mark("player.stats initialized"); }
        if (save.player.equipment == null) { save.player.equipment = new SaveData.Player.Equipment(); Mark("player.equipment initialized"); }
        if (save.inventory == null) { save.inventory = new SaveData.Inventory(); Mark("inventory initialized"); }
        if (save.progress == null) { save.progress = new SaveData.Progress(); Mark("progress initialized"); }
        if (save.time == null) { save.time = new SaveData.TimeState(); Mark("time initialized"); }
        if (save.sceneState == null) { save.sceneState = new SaveData.SceneState(); Mark("sceneState initialized"); }
        if (save.sceneState.pendingBattle == null) { save.sceneState.pendingBattle = new SaveData.PendingBattle(); Mark("sceneState.pendingBattle initialized"); }

        // Time sanity
        if (save.time.day <= 0)
        {
            save.time.day = 1;
            Mark("time.day defaulted");
        }

        if (save.time.minuteOfDay < 0 || save.time.minuteOfDay > 1439)
        {
            save.time.minuteOfDay = Mathf.Clamp(save.time.minuteOfDay, 0, 1439);
            Mark("time.minuteOfDay clamped");
        }

        if (string.IsNullOrEmpty(save.player.outfitId))
        {
            save.player.outfitId = "outfit_01";
            Mark("player.outfitId defaulted");
        }

        var stats = save.player.stats;

        // Detect missing max fields (common in older saves or saves created via new SaveData())
        var hpMaxWasMissing = stats.hpMax <= 0;
        var mpMaxWasMissing = stats.mpMax <= 0;
        var spMaxWasMissing = stats.spMax <= 0;
        var lpMaxWasMissing = stats.lpMax <= 0;

        if (hpMaxWasMissing)
        {
            stats.hpMax = 100;
            Mark("hpMax defaulted");
        }
        if (mpMaxWasMissing)
        {
            stats.mpMax = 40;
            Mark("mpMax defaulted");
        }
        if (spMaxWasMissing)
        {
            stats.spMax = 60;
            Mark("spMax defaulted");
        }
        if (lpMaxWasMissing)
        {
            stats.lpMax = 100;
            Mark("lpMax defaulted");
        }

        // If max values were missing, current values are very likely uninitialized too.
        if (hpMaxWasMissing && stats.hp <= 0)
        {
            stats.hp = stats.hpMax;
            Mark("hp defaulted to hpMax");
        }
        if (mpMaxWasMissing && stats.mp <= 0)
        {
            stats.mp = stats.mpMax;
            Mark("mp defaulted to mpMax");
        }
        if (spMaxWasMissing && stats.sp <= 0)
        {
            stats.sp = stats.spMax;
            Mark("sp defaulted to spMax");
        }

        // LP often starts at 0 by design; keep it as-is even if lpMax was missing.

        // Clamp to valid ranges
        var beforeHp = stats.hp;
        var beforeMp = stats.mp;
        var beforeSp = stats.sp;
        var beforeLp = stats.lp;

        stats.hp = Mathf.Clamp(stats.hp, 0, stats.hpMax);
        stats.mp = Mathf.Clamp(stats.mp, 0, stats.mpMax);
        stats.sp = Mathf.Clamp(stats.sp, 0, stats.spMax);
        stats.lp = Mathf.Clamp(stats.lp, 0, stats.lpMax);

        if (stats.hp != beforeHp || stats.mp != beforeMp || stats.sp != beforeSp || stats.lp != beforeLp)
            Mark("stats clamped");

        if (didMigrate)
            Debug.LogWarning($"[SaveDataMigration] Applied: {changes}");

        return save;
    }
}
