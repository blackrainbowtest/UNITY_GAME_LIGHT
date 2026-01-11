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
        [SerializeField] private EnemyData testEnemy;
        [SerializeField] private BattleLocationData testLocation;
        [SerializeField] private BattleMode mode = BattleMode.Normal;

        private void Start()
        {
            if (battleController == null)
            {
                Debug.LogError("BattleTestLauncher: BattleController not assigned");
                return;
            }

            var playerSnapshot = new PlayerCombatSnapshot(
                maxHp: 100,
                currentHp: 100
            );

            var context = new BattleContext(
                player: playerSnapshot,
                enemy: testEnemy,
                location: testLocation,
                mode: mode
            );

            battleController.StartBattle(context);
        }
    }
}
