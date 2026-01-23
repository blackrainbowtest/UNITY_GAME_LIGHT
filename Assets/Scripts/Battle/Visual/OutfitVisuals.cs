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
