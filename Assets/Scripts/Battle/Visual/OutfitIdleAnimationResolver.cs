using UnityEngine;

namespace Game.Battle.Visual
{
    public static class OutfitIdleAnimationResolver
    {
        public const string DefaultOutfitId = "outfit_01";

        // Convention-based runtime loading.
        // Put IdleAnimation assets under Resources to make this work.
        // Example:
        //   Assets/Resources/Animations/Player/outfit_01/Idle.asset
        //   Assets/Resources/Animations/Player/outfit_02/Idle.asset
        private const string PlayerIdleBasePath = "Animations/Player";

        public static IdleAnimation ResolvePlayerIdle(string outfitId)
        {
            var normalized = string.IsNullOrEmpty(outfitId) ? DefaultOutfitId : outfitId;

            var anim = Resources.Load<IdleAnimation>($"{PlayerIdleBasePath}/{normalized}/Idle");
            if (anim != null)
                return anim;

            if (normalized != DefaultOutfitId)
                return Resources.Load<IdleAnimation>($"{PlayerIdleBasePath}/{DefaultOutfitId}/Idle");

            return null;
        }
    }
}
