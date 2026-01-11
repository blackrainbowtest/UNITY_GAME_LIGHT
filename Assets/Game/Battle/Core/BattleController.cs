using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// Orchestrates battle lifecycle.
    /// Entry point for starting a battle.
    /// Contains no combat logic.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        private BattleContext context;
        private bool battleStarted;

        public void StartBattle(BattleContext battleContext)
        {
            if (battleStarted)
            {
                Debug.LogWarning("BattleController: Battle already started");
                return;
            }

            context = battleContext;
            battleStarted = true;

            Debug.Log("Battle started");
            Debug.Log($"Enemy: {context.Enemy.name}");
            Debug.Log($"Mode: {context.Mode}");

            InitializeParticipants();
            InitializeEnvironment();
            InitializeUI();
        }

        private void InitializeParticipants()
        {
            Debug.Log("Initializing participants");
        }

        private void InitializeEnvironment()
        {
            Debug.Log("Initializing environment");
        }

        private void InitializeUI()
        {
            Debug.Log("Initializing UI");
        }
    }
}
