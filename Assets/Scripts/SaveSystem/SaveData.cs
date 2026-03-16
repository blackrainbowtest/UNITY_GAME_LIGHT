using System;
using System.Collections.Generic;
using Game.Progression;

[Serializable]
public class SaveData
{
    public Meta meta = new Meta();
    public Player player = new Player();
    public Inventory inventory = new Inventory();
    public Storage storage = new Storage();
    public Progress progress = new Progress();
    public AchievementStats achievementStats = new AchievementStats();
    public TimeState time = new TimeState();
    public SceneState sceneState = new SceneState();
    public LocationStructuresState locationStructures = new LocationStructuresState();

    [Serializable]
    public class AchievementStats
    {
        /// <summary>
        /// Total real-world play time in seconds (lifetime, across all sessions of this save).
        /// </summary>
        public int realTimePlayedSeconds;

        /// <summary>
        /// Number of completed battles in this save.
        /// </summary>
        public int battlesFinished;

        public int battlesWon;
        public int battlesLost;
        public int battlesSurrendered;
        public int escapesSuccessful;
        public int escapesFailed;

        /// <summary>
        /// Total mob kills (player victories with a valid enemy target).
        /// </summary>
        public int totalMobKills;

        /// <summary>
        /// Cumulative rewards earned from battle outcomes.
        /// </summary>
        public int totalGoldEarned;
        public int totalExpEarned;

        /// <summary>
        /// Per-enemy kill counters for achievement checks.
        /// </summary>
        public List<MobKillEntry> mobKillsByEnemyId = new List<MobKillEntry>();
    }

    [Serializable]
    public class MobKillEntry
    {
        public string enemyId;
        public int kills;
    }

    [Serializable]
    public class QuestKillBaselineEntry
    {
        public string questId;
        public string enemyId;
        public int killsAtAccept;
    }

    [Serializable]
    public class LocationStructuresState
    {
        public int bedLevel = 0;
        public int campfireLevel = 0;
        public int workbenchLevel = 0;
        public int storageLevel = 0;
        public int bedActionUsedDay = 0;
    }

    [Serializable]
    public class TimeState
    {
        /// <summary>
        /// 1-based day counter (Day 1 is the first day).
        /// </summary>
        public int day = 1;

        /// <summary>
        /// Minutes since 00:00 within the current day. Range: 0..1439.
        /// </summary>
        public int minuteOfDay = 8 * 60;
    }

    [Serializable]
    public class Meta
    {
        public string version = "0.1.0";
        public string saveTime;
        public int playTimeSeconds;
    }

    [Serializable]
    public class Player
    {
        public string id;
        public string name;
        public int level = 1;
        public int exp = 0;
        public string sceneName;
        public string outfitId = "outfit_01";
        public Stats stats = new Stats();
        public List<string> statusEffects = new List<string>();

        public Equipment equipment = new Equipment();

        [Serializable]
        public class Equipment
        {
            // Left side (accessories / utility)
            public string bagItemId;
            public string ring1ItemId;
            public string ring2ItemId;
            public string amuletItemId;

            public string weaponItemId;

            // Right side (armor)
            public string helmetItemId;
            public string armorItemId;
            public string pantsItemId;
            public string bootsItemId;
        }

        /// <summary>
        /// Sets the player's scene name. Only allows non-null, non-empty values.
        /// </summary>
        public void SetSceneName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                UnityEngine.Debug.LogError("Player.SetSceneName: value is null or empty");
                return;
            }
            sceneName = value;
        }
    }

    [Serializable]
    public class Stats
    {
        // Legacy field kept for backward compatibility with older saves.
        public int damage;
        public int physicalDamage;
        public int magicDamage;

        public int hp;
        public int hpMax;
        public int mp;
        public int mpMax;
        public int sp;
        public int spMax;
        public int lp;
        public int lpMax;
    }

    [Serializable]
    public class Inventory
    {
        public int gold;
        public int manaCrystals;
        public int demonCrystals;
        public List<Item> items = new List<Item>();
    }

    [Serializable]
    public class Storage
    {
        /// <summary>
        /// Base storage items (e.g., town stash). Capacity is enforced by runtime rules.
        /// </summary>
        public List<Item> items = new List<Item>();
    }

    [Serializable]
    public class Item
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public class Progress
    {
        public Dictionary<string, bool> flags = new Dictionary<string, bool>();
        public string introResult;

        public AdventurerRank adventurerRank = AdventurerRank.None;
        public GuildState guild = new GuildState();

        /// <summary>
        /// Sets the intro result. Only allows non-null, non-empty values.
        /// </summary>
        public void SetIntroResult(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                UnityEngine.Debug.LogError("Progress.SetIntroResult: value is null or empty");
                return;
            }
            introResult = value;
        }

        public void SetAdventurerRank(AdventurerRank value)
        {
            adventurerRank = value;
        }
    }

    [Serializable]
    public class GuildState
    {
        /// <summary>
        /// Day number of the last daily board refresh processed at/after 12:00.
        /// </summary>
        public int lastQuestRefreshDay = 0;

        /// <summary>
        /// Current active quest ids shown on the guild board.
        /// </summary>
        public List<string> activeQuestIds = new List<string>();

        /// <summary>
        /// Accepted/selected quests currently tracked by the player.
        /// </summary>
        public List<string> selectedQuestIds = new List<string>();

        /// <summary>
        /// Quest ids completed by the player.
        /// </summary>
        public List<string> completedQuestIds = new List<string>();

        /// <summary>
        /// Quest ids failed/cancelled by the player.
        /// </summary>
        public List<string> failedQuestIds = new List<string>();

        /// <summary>
        /// Remaining quest ids in the current no-repeat random cycle.
        /// </summary>
        public List<string> remainingQuestPoolIds = new List<string>();

        /// <summary>
        /// Kill counters snapshot at acceptance time for kill-based quest requirements.
        /// Progress for a quest is (current kills - killsAtAccept) per enemy id.
        /// </summary>
        public List<QuestKillBaselineEntry> questKillBaselines = new List<QuestKillBaselineEntry>();

        /// <summary>
        /// Completed quests counted toward the next rank upgrade requirement.
        /// Reset when rank is upgraded.
        /// </summary>
        public int completedQuestsSinceLastRank = 0;

        /// <summary>
        /// Lifetime completed quest count (analytics/progression).
        /// </summary>
        public int completedQuestsTotal = 0;
    }

    [Serializable]
    public class SceneState
    {
        /// <summary>
        /// The scene the player was in right before the current one.
        /// </summary>
        public string previousSceneName;

        /// <summary>
        /// Anchor "main city" scene to return to from secondary locations.
        /// </summary>
        public string lastMainSceneName;

        /// <summary>
        /// Last known shelter scene for the current save.
        /// Used by Home button to return player to city-specific shelter.
        /// </summary>
        public string lastShelterSceneName;

        /// <summary>
        /// If set, loading this save should restore battle entry contexts and then load battle.
        /// Used for autosaves before battles (tutorial, encounters, etc.).
        /// </summary>
        public PendingBattle pendingBattle = new PendingBattle();

        /// <summary>
        /// If true, SaveSlotsManager should autosave when entering a suitable scene.
        /// Used for cases like: after finishing tutorial battle and returning to StartCityScene.
        /// </summary>
        public bool requestAutosaveOnSceneEnter;

        /// <summary>
        /// Optional: only autosave when entering this exact scene name.
        /// If null/empty, any save-allowed scene can trigger the autosave.
        /// </summary>
        public string requestAutosaveSceneName;

        public void RequestAutosave(string sceneName = null)
        {
            requestAutosaveOnSceneEnter = true;
            requestAutosaveSceneName = sceneName;
        }

        public void ClearAutosaveRequest()
        {
            requestAutosaveOnSceneEnter = false;
            requestAutosaveSceneName = null;
        }
    }

    [Serializable]
    public class PendingBattle
    {
        public bool isPending;
        public string battleSceneName = "FightScene";

        // Stored as strings to keep SaveData independent from battle assembly types.
        public string battleMode; // e.g. "Tutorial", "Normal"

        // Optional content identifiers; can be resolved via a runtime database.
        public string enemyId;
        public string locationId;

        // Optional: where to return after battle.
        public string returnSceneName;

        // Optional: difficulty name ("Easy"/"Normal"/"Hard") or empty for default.
        public string enemyDifficulty;

        public void Clear()
        {
            isPending = false;
            battleMode = null;
            enemyId = null;
            locationId = null;
            returnSceneName = null;
            enemyDifficulty = null;
        }
    }

    public static SaveData CreateDefault(string version)
    {
        var save = new SaveData();
        return CreateDefault(version, null);

    }

    public static SaveData CreateDefault(string version, string sceneName)
    {
        var save = new SaveData();
        save.player.name = "Airin";
        save.player.id = System.Guid.NewGuid().ToString();
        save.player.level = 1;
        save.player.exp = 0;
        save.player.outfitId = "outfit_01";
        save.player.sceneName = string.IsNullOrEmpty(sceneName)
            ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            : sceneName;
        save.player.stats.hp = 100;
        save.player.stats.hpMax = 100;
        save.player.stats.mp = 40;
        save.player.stats.mpMax = 40;
        save.player.stats.sp = 50;
        save.player.stats.spMax = 60;
        save.player.stats.lp = 0;
        save.player.stats.lpMax = 100;
        save.player.stats.damage = 10;
        save.player.stats.physicalDamage = 10;
        save.player.stats.magicDamage = 10;
        save.meta.version = version;

        // Time defaults
        save.time.day = 1;
        save.time.minuteOfDay = 8 * 60;

        // DELETEME: starter consumable for quick testing (battle inventory consumption).
        // Give the player 1 small HP potion on a brand new save.
        // if (save.inventory != null)
        // {
        //     if (save.inventory.items == null)
        //         save.inventory.items = new List<Item>();

        //     bool hasPotion = false;
        //     for (int i = 0; i < save.inventory.items.Count; i++)
        //     {
        //         var it = save.inventory.items[i];
        //         if (it == null) continue;
        //         if (string.Equals(it.itemId, "potion_hp_small", StringComparison.OrdinalIgnoreCase))
        //         {
        //             it.count = Math.Max(1, it.count);
        //             hasPotion = true;
        //             break;
        //         }
        //     }

        //     if (!hasPotion)
        //         save.inventory.items.Add(new Item { itemId = "potion_hp_small", count = 1 });
        // }

        return save;
    }
}
