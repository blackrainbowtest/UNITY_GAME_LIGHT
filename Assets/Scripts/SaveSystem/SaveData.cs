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
        public Stats stats = new Stats();
        public List<string> statusEffects = new List<string>();
    }

    [Serializable]
    public class Stats
    {
        public int hp;
        public int hpMax;
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
        save.player.sceneName = string.IsNullOrEmpty(sceneName)
            ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            : sceneName;
        save.player.stats.hp = 100;
        save.player.stats.hpMax = 100;
        save.meta.version = version;
        return save;
    }
}
