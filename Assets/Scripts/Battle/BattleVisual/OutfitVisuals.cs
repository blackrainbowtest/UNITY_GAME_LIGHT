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
using UnityEngine.Serialization;
using Game.Battle.Combat.Actions;
using Game.Battle.Statuses;
using UDA2.Audio;

namespace Game.Battle.Visual
{
    [CreateAssetMenu(menuName = "Game/Battle/Visuals/Outfit Visuals")]
    public sealed class OutfitVisuals : ScriptableObject
    {
        public enum StatusEffectTarget
        {
            Player = 0,
            Enemy = 1
        }

        [System.Serializable]
        public struct ActionStatusEffectConfig
        {
            [Tooltip("Action that can apply this status.")]
            public CombatActionId actionId;

            [Tooltip("Status to apply.")]
            public StatusEffectId statusId;

            [Tooltip("Who receives this status when action is executed.")]
            public StatusEffectTarget target;

            [Tooltip("Enable random duration instead of fixed turns.")]
            public bool randomTurns;

            [Min(0)]
            [Tooltip("Fixed duration in turns when Random Turns is disabled.")]
            public int turns;

            [Min(0)]
            [Tooltip("Minimum duration in turns when Random Turns is enabled.")]
            public int randomTurnsMin;

            [Min(0)]
            [Tooltip("Maximum duration in turns when Random Turns is enabled.")]
            public int randomTurnsMax;
        }

        [System.Serializable]
        public struct EscapeRunMotionConfig
        {
            [Tooltip("If false, no movement is applied during successful escape animation.")]
            public bool enabled;

            [Tooltip("Frame index (1-based) when movement to the left should start during successful escape animation.")]
            [Min(1)] public int startAtFrame;

            [Tooltip("Movement speed to the left in world units per second.")]
            [Min(0f)] public float speedLeftUnitsPerSecond;

            public bool IsEnabled => enabled && startAtFrame > 0 && speedLeftUnitsPerSecond > 0f;
        }

        [System.Serializable]
        public struct HitTimingConfig
        {
            [Tooltip("Attacker animation id (e.g. FastAttack, FireSpell, SeductionAct1, etc).")]
            public BattleVisualAnimId attackAnimId;

            [Tooltip("Frame when the hit should be applied (1 = immediately on animation start). Set -1 to ignore target hit animation.")]
            public int hitAtFrame;

            [Tooltip("Optional action cue played during the attacker animation.")]
            public AudioCue actionCue;

            [Tooltip("Frame for actionCue (1 = immediately). <=0 means use hitAtFrame.")]
            public int actionCueAtFrame;

            [Tooltip("If enabled, the target will play LustHit variations instead of Hit when the hit moment is reached.")]
            public bool useLustHit;
        }

        [System.Serializable]
        public struct SpellProjectileConfig
        {
            [Tooltip("Prefab to spawn as projectile. Should have BattleSpellProjectile component (or it will be added at runtime).")]
            public GameObject projectilePrefab;

            [Tooltip("Optional animated projectile frames. If set, projectile will auto-play this animation while traveling.")]
            public IdleAnimation projectileAnimation;

            [Tooltip("When to spawn projectile relative to the caster animation, in frames (1 = immediately on animation start). Values <= 1 spawn immediately.")]
            public int spawnAtFrame;

            [Tooltip("When to trigger impact (damage/hit) relative to the projectile animation, in frames. -1 = last frame. 1..N = exact frame. 0 or less (except -1) = at the end (fallback).")]
            public int impactAtFrame;

            [Tooltip("Pixels-per-unit conversion used for the pixel-based offsets and distances below.")]
            [Min(0.01f)] public float pixelsPerUnit;

            [Tooltip("Spawn offset relative to caster position, in pixels. X is automatically mirrored for enemy casts.")]
            public Vector2 spawnOffsetPixels;

            [Tooltip("How far the projectile should travel, in pixels (to the right for player, to the left for enemy).")]
            [Min(0f)] public float travelDistancePixels;

            [Tooltip("How long (seconds) the projectile should travel before being destroyed.")]
            [Min(0.01f)] public float travelTimeSeconds;

            public bool IsEnabled => projectilePrefab != null;

            public float FrameDelaySeconds(float casterFps)
            {
                if (spawnAtFrame <= 1)
                    return 0f;
                if (casterFps <= 0f)
                    return 0f;
                return (spawnAtFrame - 1) / casterFps;
            }

            public float ToUnits(float pixels)
            {
                var ppu = pixelsPerUnit > 0f ? pixelsPerUnit : 100f;
                return pixels / ppu;
            }
        }

        [Header("Identity")]
        [Tooltip("Must match outfitId, e.g. 'outfit_01'.")]
        public string outfitId = "outfit_01";

        [Header("Animations")]
        [Tooltip("Optional: if set, you can provide multiple idle animations (e.g. 3 idles). If list has 1 item, it's used as idle. If 2+ items, BattleCharacterView can randomize between them.")]
        public IdleAnimation[] idleVariations;
        [Tooltip("Optional variations for Hit.")]
        public IdleAnimation[] hitVariations;
        [Tooltip("Optional variations for LustHit (emotional hit).")]
        public IdleAnimation[] lustHitVariations;

        [Header("Hit Timing")]
        [Tooltip("Per-attack frame timing for when the target hit animation should start. If an entry is missing, defaults to frame 1 (immediate) and physical Hit.")]
        public HitTimingConfig[] hitTimings;

        [Header("Hit Audio (Optional)")]
        [Tooltip("Sound played when this character receives a regular Hit.")]
        public AudioCue hitCue;
        [Tooltip("Sound played when this character receives a LustHit. Falls back to hitCue if empty.")]
        public AudioCue lustHitCue;

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

        [Header("Magic Projectiles (Optional)")]
        [Tooltip("Projectile settings for generic Cast (also used as fallback for spells if their projectile is not set).")]
        public SpellProjectileConfig castProjectile;
        [Tooltip("Projectile settings for Fire Spell.")]
        public SpellProjectileConfig fireSpellProjectile;
        [Tooltip("Projectile settings for Ice Spell.")]
        public SpellProjectileConfig iceSpellProjectile;
        [Tooltip("Projectile settings for Holy Spell.")]
        public SpellProjectileConfig holySpellProjectile;
        [Tooltip("Projectile settings for Dark Spell.")]
        public SpellProjectileConfig darkSpellProjectile;

        [Header("Action Status Effects (Optional)")]
        [Tooltip("Per-action status effects. Supports multiple statuses per action with fixed or random duration and target side selection.")]
        public ActionStatusEffectConfig[] actionStatusEffects;

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
        [Tooltip("Optional variations for Inventory.")]
        public IdleAnimation[] actionAct1Variations;
        [Tooltip("Optional variations for Run.")]
        public IdleAnimation[] actionAct2Variations;
        [Tooltip("Optional variations for Give up.")]
        public IdleAnimation[] actionAct3Variations;
        [Tooltip("Optional variations for Skip.")]
        public IdleAnimation[] actionAct4Variations;
        [Tooltip("Optional variations for Action fail (e.g. failed escape).")]
        public IdleAnimation[] actionActFailVariations;
        [Tooltip("Optional movement config for successful Run (Escape).")]
        public EscapeRunMotionConfig escapeRunMotion;

        [Header("Inventory")]
        [Tooltip("Inventory open animation. If empty, fallback is Inventory variation [0].")]
        public IdleAnimation[] inventoryOpenVariations;
        [Tooltip("Inventory search/loop animation. If empty, fallback is Inventory variation [1].")]
        public IdleAnimation[] inventorySearchVariations;
        [Tooltip("Inventory close animation. If empty, fallback is Inventory variation [2].")]
        public IdleAnimation[] inventoryCloseVariations;

        [Header("Optional")]
        [Tooltip("Optional variations for Block.")]
        public IdleAnimation[] blockVariations;
        [FormerlySerializedAs("deathVariations")]
        [Tooltip("Optional variations for Lose (normal defeat/surrender/failed escape).")]
        public IdleAnimation[] loseVariations;
        [Tooltip("Optional variations for LustLose (defeat by LP).")]
        public IdleAnimation[] lustLoseVariations;

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
                case BattleVisualAnimId.LustHit: list = lustHitVariations; break;
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
                case BattleVisualAnimId.ActionActFail: list = actionActFailVariations; break;

                // Inventory flow uses explicit inventory fields first.
                // Fallback: ActionAct1 variations by fixed index:
                // [0] = Act1 (open), [1] = Act1_1 (search), [2] = Act1_2 (close).
                case BattleVisualAnimId.InventoryOpen:
                    list = inventoryOpenVariations != null && inventoryOpenVariations.Length > 0
                        ? inventoryOpenVariations
                        : WrapSingle(PickIndexedOrFirstValid(actionAct1Variations, 0) ?? FirstValidOrFirst(castVariations));
                    break;
                case BattleVisualAnimId.InventorySearch:
                    list = inventorySearchVariations != null && inventorySearchVariations.Length > 0
                        ? inventorySearchVariations
                        : WrapSingle(PickIndexedOrFirstValid(actionAct1Variations, 1) ?? FirstValidOrFirst(castVariations));
                    break;
                case BattleVisualAnimId.InventoryClose:
                    list = inventoryCloseVariations != null && inventoryCloseVariations.Length > 0
                        ? inventoryCloseVariations
                        : WrapSingle(PickIndexedOrFirstValid(actionAct1Variations, 2) ?? FirstValidOrFirst(castVariations));
                    break;

                case BattleVisualAnimId.Block: list = blockVariations; break;
                case BattleVisualAnimId.Lose: list = loseVariations; break;
                case BattleVisualAnimId.LustLose: list = lustLoseVariations != null && lustLoseVariations.Length > 0 ? lustLoseVariations : loseVariations; break;
                case BattleVisualAnimId.Death: list = loseVariations; break;
            }

            return list != null && list.Length > 0 ? list : null;
        }

        public bool TryGetHitTiming(BattleVisualAnimId attackAnimId, out HitTimingConfig timing)
        {
            timing = default;

            if (hitTimings == null || hitTimings.Length == 0)
                return false;

            // Iterate from the end so the newest/lowest entry in Inspector overrides older duplicates.
            for (int i = hitTimings.Length - 1; i >= 0; i--)
            {
                if (hitTimings[i].attackAnimId != attackAnimId)
                    continue;

                timing = hitTimings[i];
                return true;
            }

            return false;
        }

        public AudioCue GetReceivedHitCue(BattleVisualAnimId hitAnimId)
        {
            if (hitAnimId == BattleVisualAnimId.LustHit)
                return lustHitCue != null ? lustHitCue : hitCue;

            return hitCue;
        }

        public bool TryGetProjectileConfig(BattleVisualAnimId id, out SpellProjectileConfig config)
        {
            config = default;

            switch (id)
            {
                case BattleVisualAnimId.Cast:
                    config = castProjectile;
                    return config.IsEnabled;

                case BattleVisualAnimId.FireSpell:
                    config = fireSpellProjectile.IsEnabled ? fireSpellProjectile : castProjectile;
                    return config.IsEnabled;

                case BattleVisualAnimId.IceSpell:
                    config = iceSpellProjectile.IsEnabled ? iceSpellProjectile : castProjectile;
                    return config.IsEnabled;

                case BattleVisualAnimId.HolySpell:
                    config = holySpellProjectile.IsEnabled ? holySpellProjectile : castProjectile;
                    return config.IsEnabled;

                case BattleVisualAnimId.DarkSpell:
                    config = darkSpellProjectile.IsEnabled ? darkSpellProjectile : castProjectile;
                    return config.IsEnabled;
            }

            return false;
        }

        public bool TryGetEscapeRunMotion(out EscapeRunMotionConfig config)
        {
            config = escapeRunMotion;
            return config.IsEnabled;
        }

        public bool TryGetActionStatusEffects(CombatActionId actionId, out ActionStatusEffectConfig[] configs)
        {
            configs = null;

            if (actionStatusEffects == null || actionStatusEffects.Length == 0)
                return false;

            int count = 0;
            for (int i = 0; i < actionStatusEffects.Length; i++)
            {
                if (actionStatusEffects[i].actionId == actionId)
                    count++;
            }

            if (count == 0)
                return false;

            configs = new ActionStatusEffectConfig[count];
            int dst = 0;
            for (int i = 0; i < actionStatusEffects.Length; i++)
            {
                if (actionStatusEffects[i].actionId != actionId)
                    continue;

                configs[dst++] = actionStatusEffects[i];
            }

            return true;
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

        private static IdleAnimation PickIndexedOrFirstValid(IdleAnimation[] list, int index)
        {
            if (list == null || list.Length == 0)
                return null;

            if (index >= 0 && index < list.Length)
            {
                var atIndex = list[index];
                if (atIndex != null && atIndex.IsValid())
                    return atIndex;
            }

            return FirstValidOrFirst(list);
        }

        private static IdleAnimation[] WrapSingle(IdleAnimation anim)
        {
            if (anim == null)
                return null;

            return new[] { anim };
        }

    }
}
