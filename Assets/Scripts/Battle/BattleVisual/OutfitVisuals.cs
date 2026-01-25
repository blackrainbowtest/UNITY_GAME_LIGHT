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
        [Tooltip("Optional variations for Counter Attack.")]
        public IdleAnimation[] counterAttackVariations;

        [Header("Magic")]
        [Tooltip("Optional variations for generic Cast (fallback for spells if specific spell animation is not set).")]
        public IdleAnimation[] castVariations;
        [Tooltip("Optional variations for Fire Spell.")]
        public IdleAnimation[] fireSpellVariations;
        [Tooltip("Optional variations for Ice Spell.")]
        public IdleAnimation[] iceSpellVariations;
        [Tooltip("Optional variations for Holy Spell.")]
        public IdleAnimation[] holySpellVariations;
        [Tooltip("Optional variations for Dark Spell.")]
        public IdleAnimation[] darkSpellVariations;

        [Header("Seduction")]
        [Tooltip("Optional variations for Seduction Act 1.")]
        public IdleAnimation[] seductionAct1Variations;
        [Tooltip("Optional variations for Seduction Act 2.")]
        public IdleAnimation[] seductionAct2Variations;
        [Tooltip("Optional variations for Seduction Act 3.")]
        public IdleAnimation[] seductionAct3Variations;
        [Tooltip("Optional variations for Seduction Act 4.")]
        public IdleAnimation[] seductionAct4Variations;

        [Header("Actions")]
        [Tooltip("Optional variations for Action Act 1.")]
        public IdleAnimation[] actionAct1Variations;
        [Tooltip("Optional variations for Action Act 2.")]
        public IdleAnimation[] actionAct2Variations;
        [Tooltip("Optional variations for Action Act 3.")]
        public IdleAnimation[] actionAct3Variations;
        [Tooltip("Optional variations for Action Act 4.")]
        public IdleAnimation[] actionAct4Variations;

        [Header("Optional")]
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

                case BattleVisualAnimId.CounterAttack: list = counterAttackVariations; break;

                case BattleVisualAnimId.Cast: list = castVariations; break;
                case BattleVisualAnimId.FireSpell: list = fireSpellVariations != null && fireSpellVariations.Length > 0 ? fireSpellVariations : castVariations; break;
                case BattleVisualAnimId.IceSpell: list = iceSpellVariations != null && iceSpellVariations.Length > 0 ? iceSpellVariations : castVariations; break;
                case BattleVisualAnimId.HolySpell: list = holySpellVariations != null && holySpellVariations.Length > 0 ? holySpellVariations : castVariations; break;
                case BattleVisualAnimId.DarkSpell: list = darkSpellVariations != null && darkSpellVariations.Length > 0 ? darkSpellVariations : castVariations; break;

                case BattleVisualAnimId.SeductionAct1: list = seductionAct1Variations != null && seductionAct1Variations.Length > 0 ? seductionAct1Variations : castVariations; break;
                case BattleVisualAnimId.SeductionAct2: list = seductionAct2Variations != null && seductionAct2Variations.Length > 0 ? seductionAct2Variations : castVariations; break;
                case BattleVisualAnimId.SeductionAct3: list = seductionAct3Variations != null && seductionAct3Variations.Length > 0 ? seductionAct3Variations : castVariations; break;
                case BattleVisualAnimId.SeductionAct4: list = seductionAct4Variations != null && seductionAct4Variations.Length > 0 ? seductionAct4Variations : castVariations; break;

                case BattleVisualAnimId.ActionAct1: list = actionAct1Variations != null && actionAct1Variations.Length > 0 ? actionAct1Variations : castVariations; break;
                case BattleVisualAnimId.ActionAct2: list = actionAct2Variations != null && actionAct2Variations.Length > 0 ? actionAct2Variations : castVariations; break;
                case BattleVisualAnimId.ActionAct3: list = actionAct3Variations != null && actionAct3Variations.Length > 0 ? actionAct3Variations : castVariations; break;
                case BattleVisualAnimId.ActionAct4: list = actionAct4Variations != null && actionAct4Variations.Length > 0 ? actionAct4Variations : castVariations; break;

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
