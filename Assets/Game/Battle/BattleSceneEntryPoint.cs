using UnityEngine;
using Game.Battle;

/// <summary>
/// Entry point for the battle scene. Consumes BattleEntryContext and starts the battle via BattleController.
/// Place this component on the root of the battle scene.
/// </summary>

public class BattleSceneEntryPoint : MonoBehaviour
{
    [Header("Scene Data")]
    [SerializeField] private EnemySpawnTable enemyTable;
    [SerializeField] private BattleLocationData location;
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

        // Выбираем врага по таблице
        var resolver = new EnemySpawnResolver();
        var enemy = resolver.Resolve(enemyTable);
        if (enemy == null)
        {
            Debug.LogError("[BattleSceneEntryPoint] Не удалось выбрать врага из таблицы!");
            return;
        }

        var context = new BattleContext(
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
            return new PlayerCombatSnapshot(100, 100);
        }
        int maxHp = save.player.stats.hpMax;
        int currentHp = save.player.stats.hp;
        return new PlayerCombatSnapshot(maxHp, currentHp);
    }
}
