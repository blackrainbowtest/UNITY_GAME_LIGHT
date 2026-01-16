using UnityEngine;

namespace Game.Battle
{
    [CreateAssetMenu(menuName = "Game/Battle/Enemy")]
    public class EnemyData : ScriptableObject
    {
        [Header("Main")]
        public string enemyName;
        public Sprite icon;

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
        // Добавляй новые поля по мере необходимости
    }
}
