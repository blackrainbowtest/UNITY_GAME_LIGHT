//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\BattleSceneEntryPoint.cs                                                   */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:43:25 by UDA                                                                    */
/*   Updated: 2026/01/23 01:43:25 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;
using Game.Battle;
using Game.Battle.Combat;

/// <summary>
/// Entry point for the battle scene. Consumes BattleEntryContext and starts the battle via BattleController.
/// Place this component on the root of the battle scene.
/// </summary>

public class BattleSceneEntryPoint : MonoBehaviour
{
    [Header("Scene Data")]
    [SerializeField] private Game.Battle.EnemySpawnTable enemyTable;
    [SerializeField] private Game.Battle.BattleLocationData location;
    [Header("References")]
    [SerializeField] private BattleController battleController;

    [Header("Debug (Optional)")]
    [SerializeField] private bool useDebugSetup;
    [SerializeField] private BattleMode debugMode = BattleMode.Normal;
    [SerializeField] private Game.Battle.EnemyData debugEnemy;
    [SerializeField] private Game.Battle.EnemyDifficulty debugEnemyDifficulty = Game.Battle.EnemyDifficulty.Normal;

    [System.Serializable]
    private struct DebugPlayer
    {
        public int hpMax;
        public int hp;
        public int mpMax;
        public int mp;
        public int spMax;
        public int sp;
        public int lpMax;
        public int lp;
        public int attack;
        public int magicAttack;
    }

    [SerializeField] private bool overridePlayerSnapshot;
    [SerializeField] private DebugPlayer debugPlayer;
    [SerializeField, Min(0)] private int defaultPlayerBaseAttack = CombatDamageModel.DefaultBaseAttack;
    [SerializeField, Min(0)] private int defaultPlayerMagicDamage = CombatDamageModel.DefaultBaseAttack;
    [SerializeField] private bool setDebugReturnScene = true;
    [SerializeField] private string debugReturnSceneName = "StartCityScene";

    /// <summary>
    /// Точка входа сцены боя.
    /// Собирает все необходимые данные (игрок, враг, локация, режим, сложность),
    /// учитывает debug-настройки и сохранённые контексты,
    /// формирует BattleContext и запускает бой через BattleController.
    /// </summary>
    private void Start()
    {
        // Явная ссылка на BattleController (назначается в инспекторе)
        if (battleController == null)
        {
            Debug.LogError("[BattleSceneEntryPoint] BattleController не назначен в инспекторе!");
            return;
        }

        // If we loaded into battle from a save that had a pending battle marker,
        // restore one-shot battle contexts before consuming them.
        if (!useDebugSetup)
        {
            var save = GameState.Instance?.CurrentSave;
            var pending = save?.sceneState?.pendingBattle;
            if (pending != null && pending.isPending)
            {
                Game.Battle.BattleSaveBridge.TryApplyPendingBattle(save);
                pending.Clear();
            }
        }

        var mode = useDebugSetup ? debugMode : BattleEntryContext.Consume();
        var playerSnapshot = BuildPlayerSnapshotForRun();
        var enemyDifficulty = useDebugSetup
            ? debugEnemyDifficulty
            : Game.Battle.BattleEnemyDifficultyContext.ConsumeOrDefault(Game.Battle.EnemyDifficulty.Normal);

        if (useDebugSetup && setDebugReturnScene && !string.IsNullOrEmpty(debugReturnSceneName))
            BattleExitContext.SetReturnToScene(debugReturnSceneName);


        // Получаем врага из BattleEnemyContext, если он уже выбран
        Game.Battle.EnemyData enemy = (useDebugSetup && debugEnemy != null)
            ? debugEnemy
            : BattleEnemyContext.Consume();
        int enemyLevel = BattleEnemyContext.PeekLevelOrDefault(enemy);
        int enemyRankTier = BattleEnemyContext.PeekRankTierOrDefault(enemy);
        if (enemy == null)
        {
            // Если враг не был выбран заранее — выбираем из таблицы и сохраняем в контекст
            if (enemyTable == null)
            {
                Debug.LogError("[BattleSceneEntryPoint] EnemySpawnTable не назначен и debugEnemy не задан. Нечего спавнить.");
                return;
            }
            var resolver = new Game.Battle.EnemySpawnResolver();
            if (!resolver.Resolve(enemyTable, EnemySpawnConstraints.Default, out enemy, out enemyLevel, out enemyRankTier))
            {
                Debug.LogError("[BattleSceneEntryPoint] Не удалось выбрать врага из таблицы!");
                return;
            }
            BattleEnemyContext.Set(enemy, enemyLevel, enemyRankTier);
        }
        else
        {
            enemyLevel = BattleEnemyContext.PeekLevelOrDefault(enemy);
            enemyRankTier = BattleEnemyContext.PeekRankTierOrDefault(enemy);
        }

        var resolvedLocation = location;
        var contextLocation = BattleLocationContext.Consume();
        if (contextLocation != null)
            resolvedLocation = contextLocation;

        var context = new Game.Battle.BattleContext(
            playerSnapshot,
            enemy,
            resolvedLocation,
            mode,
            enemyDifficulty,
            enemyLevel,
            enemyRankTier
        );

        battleController.StartBattle(context);
    }

    /// <summary>
    /// Формирует снимок боевых параметров игрока для текущего запуска боя.
    /// Если включён debug-режим и задан override — использует тестовые значения,
    /// иначе делегирует построение снимка данным из GameState.
    /// Гарантирует корректные и безопасные значения ресурсов.
    /// </summary>
    private PlayerCombatSnapshot BuildPlayerSnapshotForRun()
    {
        if (useDebugSetup && overridePlayerSnapshot)
        {
            // Avoid divide-by-zero / invalid values in HUD.
            var hpMax = debugPlayer.hpMax > 0 ? debugPlayer.hpMax : 100;
            var mpMax = debugPlayer.mpMax >= 0 ? debugPlayer.mpMax : 0;
            var spMax = debugPlayer.spMax >= 0 ? debugPlayer.spMax : 0;
            var lpMax = debugPlayer.lpMax >= 0 ? debugPlayer.lpMax : 0;

            return new PlayerCombatSnapshot(
                hpMax, Mathf.Clamp(debugPlayer.hp, 0, hpMax),
                mpMax, Mathf.Clamp(debugPlayer.mp, 0, mpMax),
                spMax, Mathf.Clamp(debugPlayer.sp, 0, spMax),
                lpMax, Mathf.Clamp(debugPlayer.lp, 0, lpMax),
                physicalDamage: debugPlayer.attack > 0 ? debugPlayer.attack : defaultPlayerBaseAttack,
                magicDamage: debugPlayer.magicAttack > 0 ? debugPlayer.magicAttack : defaultPlayerMagicDamage
            );
        }

        return BuildPlayerSnapshot();
    }

    /// <summary>
    /// Создаёт боевой снимок игрока на основе текущего сохранения (GameState).
    /// Использует stats игрока как Single Source of Truth,
    /// применяет защитные fallback-значения при ошибках или старых сейвах,
    /// корректирует MP/SP при обнаружении неконсистентных данных.
    /// </summary>
    private PlayerCombatSnapshot BuildPlayerSnapshot()
    {
        // Получаем данные игрока из GameState (Single Source of Truth)
        var save = GameState.Instance.CurrentSave;
        if (save == null || save.player == null || save.player.stats == null)
        {
            Debug.LogError("[BattleSceneEntryPoint] GameState.CurrentSave или player/stats не инициализированы!");
            // Fallback: безопасные значения
            return new PlayerCombatSnapshot(
                100,
                100,
                50,
                50,
                30,
                30,
                10,
                10,
                physicalDamage: defaultPlayerBaseAttack,
                magicDamage: defaultPlayerMagicDamage,
                outfitId: "outfit_01");
        }
        var stats = save.player.stats;
        var outfitId = string.IsNullOrEmpty(save.player.outfitId) ? "outfit_01" : save.player.outfitId;

        var currentMp = stats.mp;
        var currentSp = stats.sp;

        // NOTE: Частая причина нулей — старые сейвы без новых полей.
        // Для старта боя безопасно поднимать текущие ресурсы до максимума,
        // если max задан, а current == 0.
        if (currentMp == 0 && stats.mpMax > 0)
        {
            Debug.LogWarning("[BattleSceneEntryPoint] stats.mp == 0 при stats.mpMax > 0. Используем mpMax как текущий MP для старта боя (возможен старый сейв)."
            );
            currentMp = stats.mpMax;
        }
        if (currentSp == 0 && stats.spMax > 0)
        {
            Debug.LogWarning("[BattleSceneEntryPoint] stats.sp == 0 при stats.spMax > 0. Используем spMax как текущий SP для старта боя (возможен старый сейв)."
            );
            currentSp = stats.spMax;
        }

        var resolvedPhysicalDamage = ResolvePlayerPhysicalDamage(save);
        var resolvedMagicDamage = ResolvePlayerMagicDamage(save, resolvedPhysicalDamage);

        return new PlayerCombatSnapshot(
            stats.hpMax,
            stats.hp,
            stats.mpMax,
            currentMp,
            stats.spMax,
            currentSp,
            stats.lpMax,
            stats.lp,
            physicalDamage: resolvedPhysicalDamage,
            magicDamage: resolvedMagicDamage,
            outfitId: outfitId
        );
    }

    /// <summary>
    /// Future extension point for player physical damage source.
    /// Keep one resolver method so equipment/perks/buffs can be connected without touching battle flow.
    /// </summary>
    private int ResolvePlayerPhysicalDamage(SaveData save)
    {
        // TODO(BattleStats): when equipment system is ready, resolve weapon/offhand/accessory attack bonuses here.
        // TODO(BattleStats): apply additive/multiplicative modifiers from perks, statuses, weather and temporary effects.
        // For now this reads from save stats so profile and battle use the same source.
        var stats = save != null && save.player != null ? save.player.stats : null;
        if (stats == null)
            return CombatDamageModel.NormalizeBaseAttack(defaultPlayerBaseAttack);

        var savedPhysical = stats.physicalDamage > 0 ? stats.physicalDamage : stats.damage;
        if (savedPhysical > 0)
            return CombatDamageModel.NormalizeBaseAttack(savedPhysical);

        return CombatDamageModel.NormalizeBaseAttack(defaultPlayerBaseAttack);
    }

    /// <summary>
    /// Future extension point for player magic damage source.
    /// </summary>
    private int ResolvePlayerMagicDamage(SaveData save, int fallbackPhysicalDamage)
    {
        // TODO(BattleStats): replace this with magic scaling from equipment/perks/statuses.
        var stats = save != null && save.player != null ? save.player.stats : null;
        if (stats == null)
            return CombatDamageModel.NormalizeBaseAttack(defaultPlayerMagicDamage);

        if (stats.magicDamage > 0)
            return CombatDamageModel.NormalizeBaseAttack(stats.magicDamage);

        if (fallbackPhysicalDamage > 0)
            return CombatDamageModel.NormalizeBaseAttack(fallbackPhysicalDamage);

        return CombatDamageModel.NormalizeBaseAttack(defaultPlayerMagicDamage);
    }
}
