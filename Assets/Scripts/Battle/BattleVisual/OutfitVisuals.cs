//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\Visual\OutfitVisuals.cs                                                    */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:44:03 by UDA                                                                    */
/*   Updated: 2026/01/23 01:44:03 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

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
        [Tooltip("Optional: if set, you can provide multiple idle animations (e.g. 3 idles). If list has 1 item, it's used as idle. If 2+ items, BattleCharacterView can randomize between them.")]
        public IdleAnimation[] idleVariations;
        [Tooltip("Optional variations for Hit.")]
        public IdleAnimation[] hitVariations;

        [Header("Attacks")]
        [Tooltip("Optional variations for Fast Attack.")]
        public IdleAnimation[] fastAttackVariations;
        [Tooltip("Optional variations for Normal Attack.")]
        public IdleAnimation[] normalAttackVariations;
        [Tooltip("Optional variations for Heavy Attack.")]
        public IdleAnimation[] heavyAttackVariations;

        [Header("Optional")]
        [Tooltip("Optional variations for Cast.")]
        public IdleAnimation[] castVariations;
        [Tooltip("Optional variations for Block.")]
        public IdleAnimation[] blockVariations;
        [Tooltip("Optional variations for Death.")]
        public IdleAnimation[] deathVariations;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(outfitId))
                outfitId = "outfit_01";
        }

        public IdleAnimation[] GetVariationsOrNull(BattleVisualAnimId id)
        {
            IdleAnimation[] list = null;
            switch (id)
            {
                case BattleVisualAnimId.Idle: list = idleVariations; break;
                case BattleVisualAnimId.Hit: list = hitVariations; break;
                case BattleVisualAnimId.FastAttack: list = fastAttackVariations; break;
                case BattleVisualAnimId.NormalAttack: list = normalAttackVariations; break;
                case BattleVisualAnimId.HeavyAttack: list = heavyAttackVariations; break;
                case BattleVisualAnimId.Cast: list = castVariations; break;
                case BattleVisualAnimId.Block: list = blockVariations; break;
                case BattleVisualAnimId.Death: list = deathVariations; break;
            }

            return list != null && list.Length > 0 ? list : null;
        }

        public IdleAnimation[] GetIdleVariationsOrNull()
        {
            return GetVariationsOrNull(BattleVisualAnimId.Idle);
        }

        private static IdleAnimation FirstValidOrFirst(IdleAnimation[] list)
        {
            if (list == null || list.Length == 0)
                return null;

            for (int i = 0; i < list.Length; i++)
            {
                var a = list[i];
                if (a != null && a.IsValid())
                    return a;
            }

            // If nothing is valid, still return the first non-null if present.
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] != null)
                    return list[i];
            }

            return null;
        }

    }
}
