using UnityEngine;
using Game.Battle.Combat.Actions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Battle
{
    [CreateAssetMenu(menuName = "Game/Battle/Enemy")]
    public class EnemyData : ScriptableObject
    {
        [System.Serializable]
        public sealed class LootDrop
        {
            [Tooltip("Optional: drag ItemDefinition asset here (resolved by reflection at runtime).")]
            public UnityEngine.Object item;

            [Tooltip("Fallback if ItemDefinition is not assigned. Example: 'gold' or 'potion_hp_small'.")]
            public string itemId;

            [Min(0)] public int minCount = 1;
            [Min(0)] public int maxCount = 1;

            [Range(0f, 1f)]
            public float dropChance = 1f;
        }

        [Header("Main")]
        [Tooltip("Stable identifier used for saves (runtime). If empty, will be auto-filled from enemyName or asset name.")]
        public string id;
        public string enemyName;
        public Sprite icon;

        [Header("Visuals")]
        public string outfitId = "outfit_01";
        public Game.Battle.Visual.CharacterVisualProfile visualProfile;
        public IdleAnimation idleAnimation;

        [Header("Stats")]
        public int hp;
        public int mp;
        public int sp;
        public int lp;
        public int maxHp;
        public int maxMp;
        public int maxSp;
        public int maxLp;
        public int attack;

        [Header("AI")]
        public CombatActionId[] allowedActions;

        [Header("Rewards")]
        [Min(0)]
        [Tooltip("Guaranteed gold gained on victory (before multipliers). If 0, runtime may use fallback rules.")]
        public int goldReward = 0;

        [Min(0)]
        public int expReward = 0;

        [Tooltip("Possible drops on victory. Each entry rolls independently by chance.")]
        public LootDrop[] lootTable;

        [Header("Regen (per enemy turn)")]
        public int regenHpPerTurn;
        public int regenMpPerTurn;
        public int regenSpPerTurn;
        // Добавляй новые поля по мере необходимости

        private void OnEnable()
        {
            EnsureId();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(outfitId))
                outfitId = "outfit_01";

            if (lootTable != null)
            {
                for (int i = 0; i < lootTable.Length; i++)
                {
                    var e = lootTable[i];
                    if (e == null) continue;

                    if (e.maxCount < e.minCount)
                        e.maxCount = e.minCount;

                    // Convenience: if an ItemDefinition asset is assigned, auto-fill itemId as a fallback.
                    if (string.IsNullOrWhiteSpace(e.itemId) && e.item != null)
                    {
                        var resolved = TryResolveItemIdFromObject(e.item);
                        if (!string.IsNullOrWhiteSpace(resolved))
                            e.itemId = resolved.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(e.itemId))
                        e.itemId = e.itemId.Trim();
                }
            }

            EnsureId();
        }

        private static string TryResolveItemIdFromObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return null;

            try
            {
                var t = obj.GetType();
                var prop = t.GetProperty("Id");
                if (prop == null)
                    return null;

                return prop.GetValue(obj) as string;
            }
            catch
            {
                return null;
            }
        }

        private void EnsureId()
        {
            if (!string.IsNullOrEmpty(id))
                return;

            var source = !string.IsNullOrEmpty(enemyName) ? enemyName : name;
            id = SanitizeId(source);
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "enemy";

            value = value.Trim().ToLowerInvariant();
            value = value.Replace(' ', '_');
            return value;
        }

#if UNITY_EDITOR
        public bool EditorApplyDefinition(
            string newEnemyName,
            Sprite newIcon,
            string newOutfitId,
            int newMaxHp,
            int newMaxMp,
            int newMaxSp,
            int newMaxLp,
            int newHp,
            int newMp,
            int newSp,
            int newLp,
            int newAttack,
            int newRegenHpPerTurn,
            int newRegenMpPerTurn,
            int newRegenSpPerTurn,
            CombatActionId[] newAllowedActions,
            int newGoldReward,
            int newExpReward,
            LootDrop[] newLootTable,
            out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(newEnemyName))
            {
                error = "Enemy name is null or empty.";
                return false;
            }

            enemyName = newEnemyName.Trim();
            icon = newIcon;

            outfitId = string.IsNullOrWhiteSpace(newOutfitId) ? "outfit_01" : newOutfitId.Trim();

            maxHp = newMaxHp;
            maxMp = newMaxMp;
            maxSp = newMaxSp;
            maxLp = newMaxLp;

            hp = newHp;
            mp = newMp;
            sp = newSp;
            lp = newLp;

            attack = newAttack;

            regenHpPerTurn = newRegenHpPerTurn;
            regenMpPerTurn = newRegenMpPerTurn;
            regenSpPerTurn = newRegenSpPerTurn;

            allowedActions = newAllowedActions;

            goldReward = Mathf.Max(0, newGoldReward);
            expReward = Mathf.Max(0, newExpReward);
            lootTable = newLootTable;

            EditorUtility.SetDirty(this);
            return true;
        }
#endif
    }
}
