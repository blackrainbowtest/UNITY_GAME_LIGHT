using UnityEngine;
using Game.Battle.Combat.Actions;

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
    }
}
