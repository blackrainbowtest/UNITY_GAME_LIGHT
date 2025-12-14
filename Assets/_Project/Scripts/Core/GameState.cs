using System;
using System.Collections.Generic;

namespace UDA2.Core
{
    [Serializable]
    public class GameState
    {
        public MetaState meta;
        public PlayerState player;
        public WorldState world;
        public QuestState quests;
        public InventoryState inventory;
        public AchievementState achievements;
        public CraftState craft;
    }

    [Serializable]
    public class MetaState
    {
        public string version;
        public string saveTime;
        public int playTimeSeconds;
    }

    [Serializable]
    public class PlayerState
    {
        public string id;
        public string name;
        public int level;
        public int exp;
        public PlayerStats stats;
        public PlayerPosition position;
        public PlayerCondition state;
    }

    [Serializable]
    public class PlayerStats
    {
        public int hp;
        public int hpMax;
        public int stamina;
        public int staminaMax;
        public int mana;
        public int manaMax;
    }

    [Serializable]
    public class PlayerPosition
    {
        public string scene;
        public int tileX;
        public int tileY;
    }

    [Serializable]
    public class PlayerCondition
    {
        public bool isResting;
        public float fatigue;
        public List<string> injuries;
    }

    [Serializable]
    public class WorldState
    {
        public WorldTime time;
        public string currentCityId;
        public List<string> visitedCities;
    }

    [Serializable]
    public class WorldTime
    {
        public int day;
        public int hour;
        public int minute;
    }

    [Serializable]
    public class QuestState
    {
        public List<ActiveQuest> active;
        public List<string> completed;
    }

    [Serializable]
    public class ActiveQuest
    {
        public string questId;
        public string type;
        public Dictionary<string, int> progress;
    }

    [Serializable]
    public class InventoryState
    {
        public int gold;
        public List<ItemStack> items;
        public EquipmentState equipment;
    }

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public class EquipmentState
    {
        public string armorSet;
        public string weapon;
    }

    [Serializable]
    public class CraftState
    {
        public List<string> knownRecipes;
    }

    [Serializable]
    public class AchievementState
    {
        public List<UnlockedAchievement> unlocked;
        public Dictionary<string, ProgressAchievement> progress;
        public AchievementStats stats;
    }

    [Serializable]
    public class UnlockedAchievement
    {
        public string id;
        public string dateUnlocked;
    }

    [Serializable]
    public class ProgressAchievement
    {
        public int current;
        public int required;
    }

    [Serializable]
    public class AchievementStats
    {
        public int totalEnemiesKilled;
        public int totalHerbsCollected;
        public int totalQuestsCompleted;
        public int totalRests;
        public int totalCrafts;
    }
}
