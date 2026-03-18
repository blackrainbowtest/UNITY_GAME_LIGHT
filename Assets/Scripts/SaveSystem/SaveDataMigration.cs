using UnityEngine;

public static class SaveDataMigration
{
    /// <summary>
    /// Ensures loaded SaveData is structurally valid and contains sane defaults for newly added fields.
    /// Call this right after deserialization.
    /// </summary>
    public static SaveData Apply(SaveData save)
    {
        return Apply(save, out _, logChanges: true);
    }

    public static SaveData Apply(SaveData save, out bool didMigrate, bool logChanges = true)
    {
        didMigrate = false;
        var migrated = false;

        if (save == null)
            return null;

        var changes = string.Empty;

        void Mark(string message)
        {
            migrated = true;
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
        if (save.storage == null) { save.storage = new SaveData.Storage(); Mark("storage initialized"); }
        if (save.progress == null) { save.progress = new SaveData.Progress(); Mark("progress initialized"); }
        if (save.achievementStats == null) { save.achievementStats = new SaveData.AchievementStats(); Mark("achievementStats initialized"); }
        if (save.progress.guild == null) { save.progress.guild = new SaveData.GuildState(); Mark("progress.guild initialized"); }
        if (save.time == null) { save.time = new SaveData.TimeState(); Mark("time initialized"); }
        if (save.sceneState == null) { save.sceneState = new SaveData.SceneState(); Mark("sceneState initialized"); }
        if (save.sceneState.pendingBattle == null) { save.sceneState.pendingBattle = new SaveData.PendingBattle(); Mark("sceneState.pendingBattle initialized"); }
        if (save.locationStructures == null) { save.locationStructures = new SaveData.LocationStructuresState(); Mark("locationStructures initialized"); }

        if (save.achievementStats.mobKillsByEnemyId == null)
        {
            save.achievementStats.mobKillsByEnemyId = new System.Collections.Generic.List<SaveData.MobKillEntry>();
            Mark("achievementStats.mobKillsByEnemyId initialized");
        }

        if (save.achievementStats.realTimePlayedSeconds < 0)
        {
            save.achievementStats.realTimePlayedSeconds = 0;
            Mark("achievementStats.realTimePlayedSeconds clamped");
        }

        if (save.achievementStats.battlesFinished < 0)
        {
            save.achievementStats.battlesFinished = 0;
            Mark("achievementStats.battlesFinished clamped");
        }

        if (save.achievementStats.battlesWon < 0)
        {
            save.achievementStats.battlesWon = 0;
            Mark("achievementStats.battlesWon clamped");
        }

        if (save.achievementStats.battlesLost < 0)
        {
            save.achievementStats.battlesLost = 0;
            Mark("achievementStats.battlesLost clamped");
        }

        if (save.achievementStats.battlesSurrendered < 0)
        {
            save.achievementStats.battlesSurrendered = 0;
            Mark("achievementStats.battlesSurrendered clamped");
        }

        if (save.achievementStats.escapesSuccessful < 0)
        {
            save.achievementStats.escapesSuccessful = 0;
            Mark("achievementStats.escapesSuccessful clamped");
        }

        if (save.achievementStats.escapesFailed < 0)
        {
            save.achievementStats.escapesFailed = 0;
            Mark("achievementStats.escapesFailed clamped");
        }

        if (save.achievementStats.totalMobKills < 0)
        {
            save.achievementStats.totalMobKills = 0;
            Mark("achievementStats.totalMobKills clamped");
        }

        if (save.achievementStats.totalGoldEarned < 0)
        {
            save.achievementStats.totalGoldEarned = 0;
            Mark("achievementStats.totalGoldEarned clamped");
        }

        if (save.achievementStats.totalExpEarned < 0)
        {
            save.achievementStats.totalExpEarned = 0;
            Mark("achievementStats.totalExpEarned clamped");
        }

        if (save.meta.playTimeSeconds < 0)
        {
            save.meta.playTimeSeconds = 0;
            Mark("meta.playTimeSeconds clamped");
        }

        if (save.achievementStats.realTimePlayedSeconds == 0 && save.meta.playTimeSeconds > 0)
        {
            save.achievementStats.realTimePlayedSeconds = save.meta.playTimeSeconds;
            Mark("achievementStats.realTimePlayedSeconds synced from meta.playTimeSeconds");
        }

        if (SanitizeMobKillList(save.achievementStats.mobKillsByEnemyId))
            Mark("achievementStats.mobKillsByEnemyId sanitized");

        if (save.progress.guild.activeQuestIds == null)
        {
            save.progress.guild.activeQuestIds = new System.Collections.Generic.List<string>();
            Mark("progress.guild.activeQuestIds initialized");
        }

        if (save.progress.guild.selectedQuestIds == null)
        {
            save.progress.guild.selectedQuestIds = new System.Collections.Generic.List<string>();
            Mark("progress.guild.selectedQuestIds initialized");
        }

        if (save.progress.guild.completedQuestIds == null)
        {
            save.progress.guild.completedQuestIds = new System.Collections.Generic.List<string>();
            Mark("progress.guild.completedQuestIds initialized");
        }

        if (save.progress.guild.failedQuestIds == null)
        {
            save.progress.guild.failedQuestIds = new System.Collections.Generic.List<string>();
            Mark("progress.guild.failedQuestIds initialized");
        }

        if (save.progress.guild.remainingQuestPoolIds == null)
        {
            save.progress.guild.remainingQuestPoolIds = new System.Collections.Generic.List<string>();
            Mark("progress.guild.remainingQuestPoolIds initialized");
        }

        if (save.progress.guild.lastQuestRefreshDay < 0)
        {
            save.progress.guild.lastQuestRefreshDay = 0;
            Mark("progress.guild.lastQuestRefreshDay clamped");
        }

        if (save.progress.guild.completedQuestsSinceLastRank < 0)
        {
            save.progress.guild.completedQuestsSinceLastRank = 0;
            Mark("progress.guild.completedQuestsSinceLastRank clamped");
        }

        if (save.progress.guild.completedQuestsTotal < 0)
        {
            save.progress.guild.completedQuestsTotal = 0;
            Mark("progress.guild.completedQuestsTotal clamped");
        }

        if (SanitizeQuestIdList(save.progress.guild.activeQuestIds))
            Mark("progress.guild.activeQuestIds sanitized");

        if (SanitizeQuestIdList(save.progress.guild.selectedQuestIds))
            Mark("progress.guild.selectedQuestIds sanitized");

        if (SanitizeQuestIdList(save.progress.guild.completedQuestIds))
            Mark("progress.guild.completedQuestIds sanitized");

        if (SanitizeQuestIdList(save.progress.guild.failedQuestIds))
            Mark("progress.guild.failedQuestIds sanitized");

        if (SanitizeQuestIdList(save.progress.guild.remainingQuestPoolIds))
            Mark("progress.guild.remainingQuestPoolIds sanitized");

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

        // Level/EXP sanity (EXP is stored as progress within the current level).
        if (save.player.level <= 0)
        {
            save.player.level = 1;
            Mark("player.level defaulted");
        }

        if (save.player.exp < 0)
        {
            save.player.exp = 0;
            Mark("player.exp clamped");
        }

        var beforeLevel = save.player.level;
        var beforeExp = save.player.exp;
        Game.Progression.PlayerExperience.Normalize(ref save.player.level, ref save.player.exp);
        if (save.player.level != beforeLevel || save.player.exp != beforeExp)
            Mark("player exp/level normalized");

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

        var defaultDamage = 10;
        if (stats.damage <= 0)
        {
            stats.damage = defaultDamage;
            Mark("stats.damage defaulted");
        }

        if (stats.physicalDamage <= 0)
        {
            stats.physicalDamage = stats.damage > 0 ? stats.damage : defaultDamage;
            Mark("stats.physicalDamage defaulted");
        }

        if (stats.magicDamage <= 0)
        {
            stats.magicDamage = stats.physicalDamage > 0 ? stats.physicalDamage : defaultDamage;
            Mark("stats.magicDamage defaulted");
        }

        if (stats.damage < 0)
        {
            stats.damage = 0;
            Mark("stats.damage clamped");
        }

        if (stats.physicalDamage < 0)
        {
            stats.physicalDamage = 0;
            Mark("stats.physicalDamage clamped");
        }

        if (stats.magicDamage < 0)
        {
            stats.magicDamage = 0;
            Mark("stats.magicDamage clamped");
        }

        var ls = save.locationStructures;
        if (ls.bedLevel < 0)
        {
            ls.bedLevel = 0;
            Mark("locationStructures.bedLevel defaulted");
        }

        if (ls.campfireLevel < 0)
        {
            ls.campfireLevel = 0;
            Mark("locationStructures.campfireLevel defaulted");
        }

        if (ls.workbenchLevel < 0)
        {
            ls.workbenchLevel = 0;
            Mark("locationStructures.workbenchLevel defaulted");
        }

        if (ls.storageLevel < 0)
        {
            ls.storageLevel = 0;
            Mark("locationStructures.storageLevel defaulted");
        }

        didMigrate = migrated;

        if (didMigrate && logChanges)
            Debug.LogWarning($"[SaveDataMigration] Applied: {changes}");

        return save;
    }

    private static bool SanitizeQuestIdList(System.Collections.Generic.List<string> ids)
    {
        if (ids == null)
            return false;

        var changed = false;
        var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        var sanitized = new System.Collections.Generic.List<string>(ids.Count);

        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
            {
                changed = true;
                continue;
            }

            sanitized.Add(id);
        }

        if (!changed)
            return false;

        ids.Clear();
        ids.AddRange(sanitized);
        return true;
    }

    private static bool SanitizeMobKillList(System.Collections.Generic.List<SaveData.MobKillEntry> entries)
    {
        if (entries == null)
            return false;

        var changed = false;
        var totals = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || string.IsNullOrWhiteSpace(e.enemyId) || e.kills <= 0)
            {
                changed = true;
                continue;
            }

            var key = e.enemyId.Trim();
            if (string.IsNullOrEmpty(key))
            {
                changed = true;
                continue;
            }

            if (totals.TryGetValue(key, out var current))
            {
                totals[key] = current + e.kills;
                changed = true;
            }
            else
            {
                totals[key] = e.kills;
            }
        }

        if (!changed)
            return false;

        entries.Clear();
        foreach (var kv in totals)
            entries.Add(new SaveData.MobKillEntry { enemyId = kv.Key, kills = kv.Value });

        return true;
    }
}
