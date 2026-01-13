using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// Temporary debug launcher for testing BattleController.
    /// Will be removed later.
    /// </summary>
    public class BattleTestLauncher : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;
        [SerializeField] private EnemySpawnTable enemySpawnTable;
        [SerializeField] private BattleLocationData testLocation;

        private void Start()
        {
            if (battleController == null)
            {
                Debug.LogError("BattleTestLauncher: BattleController not assigned");
                return;
            }

            if (enemySpawnTable == null)
            {
                Debug.LogError("BattleTestLauncher: EnemySpawnTable not assigned");
                return;
            }

            var resolver = new EnemySpawnResolver();
            var enemy = resolver.Resolve(enemySpawnTable);

            if (enemy == null)
            {
                Debug.LogError("BattleTestLauncher: Failed to resolve enemy");
                return;
            }

            Debug.Log($"[BattleTestLauncher] Selected enemy: {enemy.name}");

            var battleMode = BattleEntryContext.Consume();
            Debug.Log($"[BattleTestLauncher] Battle mode: {battleMode}");

            var playerSnapshot = new PlayerCombatSnapshot(
                maxHp: 100,
                currentHp: 100
            );

            var context = new BattleContext(
                player: playerSnapshot,
                enemy: enemy,
                location: testLocation,
                mode: battleMode
            );

            battleController.StartBattle(context);
        }
    }
}
