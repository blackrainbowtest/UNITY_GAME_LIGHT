
using UnityEngine;
using Game.Battle.Combat;

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

        private CombatState combatState;
        private BattleCombatEngine combatEngine;

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

            combatEngine = new BattleCombatEngine(
                new CombatConfig(
                    playerBaseDamage: 10,
                    enemyBaseDamage: 7
                )
            );

            combatState = new CombatState(
                playerHp: context.Player.CurrentHP,
                playerMp: context.Player.CurrentMP,
                playerSp: context.Player.CurrentSP,
                playerLp: context.Player.CurrentLP,
                enemyHp: context.Enemy.hp,
                enemyMp: context.Enemy.mp,
                enemySp: context.Enemy.sp,
                enemyLp: context.Enemy.lp
            );

            Debug.Log("Battle started");
            Debug.Log($"Enemy: {context.Enemy.name}");
            Debug.Log($"Mode: {context.Mode}");

            InitializeParticipants();
            InitializeEnvironment();
            InitializeUI();

            PushHudState();
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
            if (!battleStarted)
                return;

            var result = combatEngine.PlayerAttack(combatState);
            combatState = result.state;

            PushHudState();

            if (result.result == CombatResult.PlayerWon)
                Debug.Log("BattleCombat v0.1: Player won");
            else if (result.result == CombatResult.PlayerLost)
                Debug.Log("BattleCombat v0.1: Player lost");
        }

        public void OnItemPressed()
        {
            Debug.Log("Item pressed");
        }

        public void OnExitPressed()
        {
            Debug.Log("Exit pressed");
        }
        private void PushHudState()
        {
            var hudState = BattleHUDStateFactory.Create(
                context.Player,
                context.Enemy,
                combatState
            );
            hudController?.UpdateState(hudState);
        }
    }
}
