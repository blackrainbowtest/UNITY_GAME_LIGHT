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
        public enum CueSelectionMode
        {
            [InspectorName("Случайный из списка")]
            RandomFromList = 0,
            [InspectorName("Точный из списка")]
            SpecificFromList = 1,
        }

        public enum StatusEffectTarget
        {
            [InspectorName("Игрок")]
            Player = 0,
            [InspectorName("Враг")]
            Enemy = 1
        }

        [System.Serializable]
        public struct CueEventConfig
        {
            [Tooltip("Кадр, на котором должен проиграться звук (1 = сразу). <=0 использует кадр по умолчанию/legacy.")]
            public int cueAtFrame;

            [Tooltip("Режим выбора: случайный звук из списка или точный индекс.")]
            public CueSelectionMode selectionMode;

            [Tooltip("Используется только в режиме точного выбора. Индекс 0-based в списке звуков.")]
            public int cueIndex;
        }

        public struct ResolvedCueEvent
        {
            public AudioCue cue;
            public int cueAtFrame;
        }

        [System.Serializable]
        public struct ActionStatusEffectConfig
        {
            [Tooltip("Действие, которое может наложить этот статус.")]
            public CombatActionId actionId;

            [Tooltip("Статус для наложения.")]
            public StatusEffectId statusId;

            [Tooltip("Кто получает статус при выполнении действия.")]
            public StatusEffectTarget target;

            [Tooltip("Использовать случайную длительность вместо фиксированной.")]
            public bool randomTurns;

            [Min(0)]
            [Tooltip("Фиксированная длительность в ходах, когда случайная длительность выключена.")]
            public int turns;

            [Min(0)]
            [Tooltip("Минимальная длительность в ходах при случайной длительности.")]
            public int randomTurnsMin;

            [Min(0)]
            [Tooltip("Максимальная длительность в ходах при случайной длительности.")]
            public int randomTurnsMax;
        }

        [System.Serializable]
        public struct EscapeRunMotionConfig
        {
            [Tooltip("Если выключено, движение при успешном побеге не применяется.")]
            public bool enabled;

            [Tooltip("Кадр (1-based), с которого начинается движение влево при успешном побеге.")]
            [Min(1)] public int startAtFrame;

            [Tooltip("Скорость движения влево в world units в секунду.")]
            [Min(0f)] public float speedLeftUnitsPerSecond;

            public bool IsEnabled => enabled && startAtFrame > 0 && speedLeftUnitsPerSecond > 0f;
        }

        [System.Serializable]
        public struct HitTimingConfig
        {
            [Tooltip("ID анимации атакующего (например FastAttack, FireSpell, SeductionAct1 и т.д.).")]
            public BattleVisualAnimId attackAnimId;

            [Tooltip("Кадр применения удара (1 = сразу в начале анимации). -1 отключает анимацию попадания цели.")]
            public int hitAtFrame;

            [Tooltip("Необязательный звук действия во время анимации атакующего.")]
            public AudioCue actionCue;

            [Tooltip("Необязательный список звуков действия. Поддерживает случайный/точный выбор через actionCueEvents.")]
            public AudioCue[] actionCues;

            [Tooltip("Кадр для actionCue (1 = сразу). <=0 использует hitAtFrame.")]
            public int actionCueAtFrame;

            [Tooltip("Необязательный список событий звука. Каждое событие выбирает случайный/точный звук из actionCues и имеет свой кадр.")]
            public CueEventConfig[] actionCueEvents;

            [Tooltip("Если включено, цель проигрывает вариации LustHit вместо Hit в момент удара.")]
            public bool useLustHit;
        }

        [System.Serializable]
        public struct AnimationCueConfig
        {
            [Tooltip("Тип анимации, к которой относится этот звук.")]
            public BattleVisualAnimId animId;

            [Tooltip("Необязательная конкретная вариация. Если задана, звук используется только для этой IdleAnimation.")]
            public IdleAnimation animation;

            [Tooltip("Звук SFX для старта этой анимации.")]
            public AudioCue cue;

            [Tooltip("Необязательный список звуков. Каждый раз выбирается случайный валидный звук (если список пуст/невалиден, используется поле Cue).")]
            public AudioCue[] cues;

            [Tooltip("Кадр воспроизведения звука (1 = сразу). <=0 тоже означает сразу.")]
            public int cueAtFrame;

            [Tooltip("Необязательный список событий звука. Каждое событие выбирает случайный/точный звук из Cues и имеет свой кадр.")]
            public CueEventConfig[] cueEvents;

            [Tooltip("Если включено, звук зацикливается, пока активен этот анимационный стейт.")]
            public bool loopWhileStateActive;
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

        [Header("Идентификация")]
        [Tooltip("Должен совпадать с outfitId, например 'outfit_01'.")]
        public string outfitId = "outfit_01";

        [Header("Анимации")]
        [Tooltip("Опционально: можно задать несколько idle-анимаций (например 3). Если в списке 1 элемент, он используется как idle. Если 2+ элемента, BattleCharacterView может выбирать случайно.")]
        public IdleAnimation[] idleVariations;
        [Tooltip("Опциональные вариации для Hit.")]
        public IdleAnimation[] hitVariations;
        [Tooltip("Опциональные вариации для LustHit (эмоциональное попадание).")]
        public IdleAnimation[] lustHitVariations;

        [Header("Тайминг Удара")]
        [Tooltip("Покадровый тайминг для старта анимации попадания цели. Если записи нет, используется кадр 1 (сразу) и обычный Hit.")]
        public HitTimingConfig[] hitTimings;

        [Header("Звук Попадания (Опционально)")]
        [Tooltip("Звук, который проигрывается, когда персонаж получает обычный Hit.")]
        public AudioCue hitCue;
        [Tooltip("Звук, который проигрывается при LustHit. Если пусто, используется hitCue.")]
        public AudioCue lustHitCue;

        [Header("Звуки Анимаций (Опционально)")]
        [Tooltip("SFX на анимацию. Используйте поле Animation, чтобы назначать разные звуки для конкретных вариаций (например для каждого idle-варианта).")]
        public AnimationCueConfig[] animationCues;

        [Header("Атаки")]
        [Tooltip("Опциональные вариации для Fast Attack.")]
        public IdleAnimation[] fastAttackVariations;
        [Tooltip("Опциональные вариации для Normal Attack.")]
        public IdleAnimation[] normalAttackVariations;
        [Tooltip("Опциональные вариации для Heavy Attack.")]
        public IdleAnimation[] heavyAttackVariations;
        [Tooltip("Опциональные вариации для Counter Attack.")]
        public IdleAnimation[] counterAttackVariations;

        [Header("Магия")]
        [Tooltip("Опциональные вариации для общего Cast (fallback для заклинаний, если спец-анимация не задана).")]
        public IdleAnimation[] castVariations;
        [Tooltip("Опциональные вариации для Fire Spell.")]
        public IdleAnimation[] fireSpellVariations;
        [Tooltip("Опциональные вариации для Ice Spell.")]
        public IdleAnimation[] iceSpellVariations;
        [Tooltip("Опциональные вариации для Holy Spell.")]
        public IdleAnimation[] holySpellVariations;
        [Tooltip("Опциональные вариации для Dark Spell.")]
        public IdleAnimation[] darkSpellVariations;

        [Header("Магические Снаряды (Опционально)")]
        [Tooltip("Настройки снаряда для общего Cast (также fallback для заклинаний, если их снаряд не задан).")]
        public SpellProjectileConfig castProjectile;
        [Tooltip("Настройки снаряда для Fire Spell.")]
        public SpellProjectileConfig fireSpellProjectile;
        [Tooltip("Настройки снаряда для Ice Spell.")]
        public SpellProjectileConfig iceSpellProjectile;
        [Tooltip("Настройки снаряда для Holy Spell.")]
        public SpellProjectileConfig holySpellProjectile;
        [Tooltip("Настройки снаряда для Dark Spell.")]
        public SpellProjectileConfig darkSpellProjectile;

        [Header("Статусы Действий (Опционально)")]
        [Tooltip("Статусы на действие. Поддерживает несколько статусов на одно действие, фиксированную или случайную длительность и выбор стороны цели.")]
        public ActionStatusEffectConfig[] actionStatusEffects;

        [Header("Соблазнение")]
        [Tooltip("Опциональные вариации для Seduction Act 1.")]
        public IdleAnimation[] seductionAct1Variations;
        [Tooltip("Опциональные вариации для Seduction Act 2.")]
        public IdleAnimation[] seductionAct2Variations;
        [Tooltip("Опциональные вариации для Seduction Act 3.")]
        public IdleAnimation[] seductionAct3Variations;
        [Tooltip("Опциональные вариации для Seduction Act 4.")]
        public IdleAnimation[] seductionAct4Variations;

        [Header("Действия")]
        [Tooltip("Опциональные вариации для Inventory.")]
        public IdleAnimation[] actionAct1Variations;
        [Tooltip("Опциональные вариации для Run.")]
        public IdleAnimation[] actionAct2Variations;
        [Tooltip("Опциональные вариации для Give up.")]
        public IdleAnimation[] actionAct3Variations;
        [Tooltip("Опциональные вариации для Skip.")]
        public IdleAnimation[] actionAct4Variations;
        [Tooltip("Опциональные вариации для Action fail (например неудачный побег).")]
        public IdleAnimation[] actionActFailVariations;
        [Tooltip("Опциональные настройки движения для успешного Run (Escape).")]
        public EscapeRunMotionConfig escapeRunMotion;

        [Header("Инвентарь")]
        [Tooltip("Анимация открытия инвентаря. Если пусто, fallback = Inventory variation [0].")]
        public IdleAnimation[] inventoryOpenVariations;
        [Tooltip("Анимация поиска/цикла инвентаря. Если пусто, fallback = Inventory variation [1].")]
        public IdleAnimation[] inventorySearchVariations;
        [Tooltip("Анимация закрытия инвентаря. Если пусто, fallback = Inventory variation [2].")]
        public IdleAnimation[] inventoryCloseVariations;

        [Header("Дополнительно")]
        [Tooltip("Опциональные вариации для Block.")]
        public IdleAnimation[] blockVariations;
        [FormerlySerializedAs("deathVariations")]
        [Tooltip("Опциональные вариации для Lose (обычное поражение/сдача/неудачный побег).")]
        public IdleAnimation[] loseVariations;
        [Tooltip("Опциональные вариации для LustLose (поражение по LP).")]
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

        public bool TryGetAnimationCue(BattleVisualAnimId animId, IdleAnimation animation, out AudioCue cue, out int cueAtFrame, out bool loopWhileStateActive)
        {
            cue = null;
            cueAtFrame = -1;
            loopWhileStateActive = false;

            if (!TryGetAnimationCueEvents(animId, animation, out var events, out loopWhileStateActive))
                return false;

            if (events == null || events.Length == 0)
                return false;

            cue = events[0].cue;
            cueAtFrame = events[0].cueAtFrame;
            return cue != null;
        }

        public bool TryGetAnimationCueEvents(BattleVisualAnimId animId, IdleAnimation animation, out ResolvedCueEvent[] cueEvents, out bool loopWhileStateActive)
        {
            cueEvents = null;
            loopWhileStateActive = false;

            if (animationCues == null || animationCues.Length == 0)
                return false;

            // Prefer exact variation match first.
            for (int i = animationCues.Length - 1; i >= 0; i--)
            {
                var entry = animationCues[i];
                if (entry.animId != animId)
                    continue;

                if (entry.animation == null || entry.animation != animation)
                    continue;

                loopWhileStateActive = entry.loopWhileStateActive;
                return TryBuildResolvedCueEvents(entry.cue, entry.cues, entry.cueAtFrame, entry.cueEvents, fallbackFrame: 1, out cueEvents);
            }

            // Fallback: generic by animId.
            for (int i = animationCues.Length - 1; i >= 0; i--)
            {
                var entry = animationCues[i];
                if (entry.animId != animId)
                    continue;

                if (entry.animation != null)
                    continue;

                loopWhileStateActive = entry.loopWhileStateActive;
                return TryBuildResolvedCueEvents(entry.cue, entry.cues, entry.cueAtFrame, entry.cueEvents, fallbackFrame: 1, out cueEvents);
            }

            return false;
        }

        public bool TryGetHitActionCueEvents(BattleVisualAnimId attackAnimId, out ResolvedCueEvent[] cueEvents)
        {
            cueEvents = null;

            if (!TryGetHitTiming(attackAnimId, out var timing))
                return false;

            int fallbackFrame = timing.hitAtFrame > 0 ? timing.hitAtFrame : 1;
            return TryBuildResolvedCueEvents(timing.actionCue, timing.actionCues, timing.actionCueAtFrame, timing.actionCueEvents, fallbackFrame, out cueEvents);
        }

        private static AudioCue PickRandomAnimationCue(AnimationCueConfig entry)
        {
            if (entry.cues != null && entry.cues.Length > 0)
            {
                int start = Random.Range(0, entry.cues.Length);
                for (int i = 0; i < entry.cues.Length; i++)
                {
                    var candidate = entry.cues[(start + i) % entry.cues.Length];
                    if (candidate != null && candidate.Clip != null)
                        return candidate;
                }
            }

            if (entry.cue != null && entry.cue.Clip != null)
                return entry.cue;

            return null;
        }

        private static bool TryBuildResolvedCueEvents(
            AudioCue legacyCue,
            AudioCue[] cuePool,
            int legacyFrame,
            CueEventConfig[] eventConfigs,
            int fallbackFrame,
            out ResolvedCueEvent[] resolved)
        {
            resolved = null;

            var list = new System.Collections.Generic.List<ResolvedCueEvent>(4);

            int defaultFrame = legacyFrame > 0 ? legacyFrame : (fallbackFrame > 0 ? fallbackFrame : 1);

            if (eventConfigs != null && eventConfigs.Length > 0)
            {
                for (int i = 0; i < eventConfigs.Length; i++)
                {
                    var cfg = eventConfigs[i];
                    var cue = PickCueFromPool(legacyCue, cuePool, cfg.selectionMode, cfg.cueIndex);
                    if (cue == null || cue.Clip == null)
                        continue;

                    int frame = cfg.cueAtFrame > 0 ? cfg.cueAtFrame : defaultFrame;
                    list.Add(new ResolvedCueEvent { cue = cue, cueAtFrame = frame });
                }
            }
            else
            {
                var cue = PickCueFromPool(legacyCue, cuePool, CueSelectionMode.RandomFromList, 0);
                if (cue != null && cue.Clip != null)
                    list.Add(new ResolvedCueEvent { cue = cue, cueAtFrame = defaultFrame });
            }

            if (list.Count == 0)
                return false;

            resolved = list.ToArray();
            return true;
        }

        private static AudioCue PickCueFromPool(AudioCue legacyCue, AudioCue[] cuePool, CueSelectionMode mode, int index)
        {
            if (cuePool != null && cuePool.Length > 0)
            {
                if (mode == CueSelectionMode.SpecificFromList)
                {
                    int idx = Mathf.Clamp(index, 0, cuePool.Length - 1);
                    var chosen = cuePool[idx];
                    if (chosen != null && chosen.Clip != null)
                        return chosen;
                }
                else
                {
                    int start = Random.Range(0, cuePool.Length);
                    for (int i = 0; i < cuePool.Length; i++)
                    {
                        var chosen = cuePool[(start + i) % cuePool.Length];
                        if (chosen != null && chosen.Clip != null)
                            return chosen;
                    }
                }
            }

            return (legacyCue != null && legacyCue.Clip != null) ? legacyCue : null;
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
