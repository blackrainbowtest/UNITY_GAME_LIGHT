using UnityEngine;

/// <summary>
/// Orchestrates battle lifecycle.
/// Entry point for starting a battle.
/// Contains no combat logic.
/// </summary>
using Game.Battle.UI;

namespace Game.Battle
{
    public class BattleController : MonoBehaviour, IBattleUIActions
    {
        private BattleContext context;
        private bool battleStarted;

        [Header("Scene References")]
        [SerializeField] private BattleEnvironmentController environmentController;
        [SerializeField] private BattleHUDController hudController;

        public void StartBattle(BattleContext battleContext)
        {
            Debug.Log("[BattleController] StartBattle called");
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

            hudController?.UpdateState(new BattleHUDState
            {
                PlayerHp = context.Player.CurrentHP,
                PlayerHpMax = context.Player.MaxHP,
                EnemyHp = context.Enemy.maxHp,      // только maxHp, текущего нет
                EnemyHpMax = context.Enemy.maxHp
            });
        }

        private void InitializeParticipants()
        {
            Debug.Log("Initializing participants");
        }

        private void Start()
        {
            BindHUD(hudController);
            
        }

        private void InitializeEnvironment()
        {
            Debug.Log("Initializing environment");
            if (environmentController == null)
            {
                Debug.LogError("BattleController: BattleEnvironmentController not assigned");
                return;
            }
            environmentController.Apply(context.Location);
        }

        private void InitializeUI()
        {
            Debug.Log("Initializing UI");
        }

        /// <summary>
        /// Binds the HUD to battle actions. Passes IBattleUIActions implementation to the HUD.
        /// </summary>
        private void BindHUD(BattleHUDController hud)
        {
            if (hud == null)
            {
                Debug.LogError("BattleController: HUDController not assigned");
                return;
            }
            hud.SetActions(this);
        }

        public void OnAttackPressed()
        {
            Debug.Log("Attack pressed");
        }

        public void OnItemPressed()
        {
            Debug.Log("Item pressed");
        }

        public void OnExitPressed()
        {
            Debug.Log("Exit pressed");
        }
    }
}
