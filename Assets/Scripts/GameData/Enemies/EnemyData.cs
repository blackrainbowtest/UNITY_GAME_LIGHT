using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "GameData/Enemy Data", order = 1)]
public class EnemyData : ScriptableObject
{

    [Header("Identity")]
    public string id;
    public string nameKey;
    public string descKey;

    [Header("Visuals")]
    public Sprite sprite;

    [System.Serializable]
    public class StatsData
    {
        public int hpMax;
        public int manaMax;
        public int lustMax;
        public int staminaMax;
    }
    public StatsData baseStats;

    [System.Serializable]
    public class InitialStateData
    {
        public int hp;
        public int mana;
        public int lust;
        public int stamina;
    }
    public InitialStateData initialState;


    [System.Serializable]
    public class ChancesData
    {
        [Range(0f, 1f)] public float crit;
        [Range(0f, 1f)] public float miss;
        [Range(0f, 1f)] public float stun;
    }

    [System.Serializable]
    public class CombatData
    {
        public int baseAttack;
        public int baseDefense;
        public ChancesData chances;
    }
    public CombatData combat;

    [System.Serializable]
    public class DropsData
    {
        public int goldMin;
        public int goldMax;
        public System.Collections.Generic.List<string> items;
    }
    public DropsData drops;
}
