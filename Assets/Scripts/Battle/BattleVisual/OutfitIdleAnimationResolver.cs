using UnityEngine;

namespace Game.Battle.Visual
{
    public static class OutfitIdleAnimationResolver
    {
        public const string DefaultOutfitId = "outfit_01";

        // Convention-based runtime loading.
        // Put IdleAnimation assets under Resources to make this work.
        // Naming convention:
        //   Assets/Resources/Player_outfit_01_Idle.asset
        //   Assets/Resources/Player_outfit_02_Idle.asset
        private const string PlayerIdlePrefix = "Player_";
        private const string PlayerIdleSuffix = "_Idle";

        public static IdleAnimation ResolvePlayerIdle(string outfitId)
        {
            var normalized = string.IsNullOrEmpty(outfitId) ? DefaultOutfitId : outfitId;

            var anim = Resources.Load<IdleAnimation>($"{PlayerIdlePrefix}{normalized}{PlayerIdleSuffix}");
            if (anim != null)
                return anim;

            if (normalized != DefaultOutfitId)
                return Resources.Load<IdleAnimation>($"{PlayerIdlePrefix}{DefaultOutfitId}{PlayerIdleSuffix}");

            return null;
        }
    }
}
