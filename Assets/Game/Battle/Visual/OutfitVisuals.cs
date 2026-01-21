using UnityEngine;

namespace Game.Battle.Visual
{
    [CreateAssetMenu(menuName = "Game/Battle/Visuals/Outfit Visuals")]
    public sealed class OutfitVisuals : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Must match outfitId, e.g. 'outfit_01'.")]
        public string outfitId = "outfit_01";

        [Header("Animations")]
        public IdleAnimation idle;
        public IdleAnimation hit;

        [Header("Attacks")]
        public IdleAnimation fastAttack;
        public IdleAnimation normalAttack;
        public IdleAnimation heavyAttack;

        [Header("Optional")]
        public IdleAnimation cast;
        public IdleAnimation block;
        public IdleAnimation death;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(outfitId))
                outfitId = "outfit_01";
        }

        public IdleAnimation Get(BattleVisualAnimId id)
        {
            switch (id)
            {
                case BattleVisualAnimId.Idle: return idle;
                case BattleVisualAnimId.Hit: return hit;
                case BattleVisualAnimId.FastAttack: return fastAttack;
                case BattleVisualAnimId.NormalAttack: return normalAttack;
                case BattleVisualAnimId.HeavyAttack: return heavyAttack;
                case BattleVisualAnimId.Cast: return cast;
                case BattleVisualAnimId.Block: return block;
                case BattleVisualAnimId.Death: return death;
                default: return null;
            }
        }
    }
}
