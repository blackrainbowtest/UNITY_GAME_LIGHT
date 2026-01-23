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
        [Header("Main")]
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

        [Header("Regen (per enemy turn)")]
        public int regenHpPerTurn;
        public int regenMpPerTurn;
        public int regenSpPerTurn;
        // Добавляй новые поля по мере необходимости

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(outfitId))
                outfitId = "outfit_01";
        }

#if UNITY_EDITOR
        public bool EditorApplyDefinition(
            string newEnemyName,
            Sprite newIcon,
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

            EditorUtility.SetDirty(this);
            return true;
        }
#endif
    }
}
