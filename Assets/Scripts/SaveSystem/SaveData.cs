using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public Meta meta = new Meta();
    public Player player = new Player();
    public Inventory inventory = new Inventory();
    public Progress progress = new Progress();

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
        return save;
    }
}
