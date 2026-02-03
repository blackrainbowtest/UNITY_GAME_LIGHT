using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public Meta meta = new Meta();
    public Player player = new Player();
    public Inventory inventory = new Inventory();
    public Storage storage = new Storage();
    public Progress progress = new Progress();
    public TimeState time = new TimeState();
    public SceneState sceneState = new SceneState();

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
    }

    [Serializable]
    public class SceneState
    {
        /// <summary>
        /// Anchor "main city" scene to return to from secondary locations.
        /// </summary>
        public string lastMainSceneName;

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
        save.meta.version = version;

        // Time defaults
        save.time.day = 1;
        save.time.minuteOfDay = 8 * 60;

        return save;
    }
}
