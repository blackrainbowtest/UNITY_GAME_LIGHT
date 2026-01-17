using UnityEngine;
using Game.Battle;

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

    private void Start()
    {
        // Явная ссылка на BattleController (назначается в инспекторе)
        if (battleController == null)
        {
            Debug.LogError("[BattleSceneEntryPoint] BattleController не назначен в инспекторе!");
            return;
        }

        var mode = BattleEntryContext.Consume();
        var playerSnapshot = BuildPlayerSnapshot();


        // Получаем врага из BattleEnemyContext, если он уже выбран
        Game.Battle.EnemyData enemy = BattleEnemyContext.Consume();
        if (enemy == null)
        {
            // Если враг не был выбран заранее — выбираем из таблицы и сохраняем в контекст
            var resolver = new Game.Battle.EnemySpawnResolver();
            enemy = resolver.Resolve(enemyTable);
            if (enemy == null)
            {
                Debug.LogError("[BattleSceneEntryPoint] Не удалось выбрать врага из таблицы!");
                return;
            }
            BattleEnemyContext.Set(enemy);
        }

        var context = new Game.Battle.BattleContext(
            playerSnapshot,
            enemy,
            location,
            mode
        );

        battleController.StartBattle(context);
    }

    private PlayerCombatSnapshot BuildPlayerSnapshot()
    {
        // Получаем данные игрока из GameState (Single Source of Truth)
        var save = GameState.Instance.CurrentSave;
        if (save == null || save.player == null || save.player.stats == null)
        {
            Debug.LogError("[BattleSceneEntryPoint] GameState.CurrentSave или player/stats не инициализированы!");
            // Fallback: безопасные значения
            return new PlayerCombatSnapshot(100, 100, 50, 50, 30, 30, 10, 10);
        }
        var stats = save.player.stats;

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

        return new PlayerCombatSnapshot(
            stats.hpMax,
            stats.hp,
            stats.mpMax,
            currentMp,
            stats.spMax,
            currentSp,
            stats.lpMax,
            stats.lp
        );
    }
}
