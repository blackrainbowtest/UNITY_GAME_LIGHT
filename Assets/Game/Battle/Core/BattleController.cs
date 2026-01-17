using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Battle.Combat;
using Game.Battle.Combat.Actions;

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
        private CombatActionRegistry actionRegistry;

        [Header("Scene References")]
        [SerializeField] private BattleEnvironmentController environmentController;
        [SerializeField] private BattleHUDController hudController;
        [SerializeField] private BattleResultModalController resultModal;

        [Header("Exit")]
        [SerializeField] private string tutorialReturnSceneName = "StartCityScene";
        [SerializeField] private string defaultReturnSceneName = "StartCityScene";

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

            combatEngine = new BattleCombatEngine();
            actionRegistry = new CombatActionRegistry();

            combatState = new CombatState(
                playerHp: context.Player.CurrentHP,
                playerMp: context.Player.CurrentMP,
                playerSp: context.Player.CurrentSP,
                playerLp: context.Player.CurrentLP,
                enemyHp: context.Enemy.hp,
                enemyMp: context.Enemy.mp,
                enemySp: context.Enemy.sp,
                enemyLp: context.Enemy.lp,
                playerBlockedLastTurn: false // первый ход — не блокировал
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

            ExecutePlayerAction(CombatActionId.NormalAttack);
        }

        public void OnCombatActionSelected(CombatActionId actionId)
        {
            if (!battleStarted)
                return;

            ExecutePlayerAction(actionId);
        }

        public void OnItemPressed()
        {
            Debug.Log("Item pressed");
        }

        public void OnExitPressed()
        {
            Debug.Log("Exit pressed");
        }
        
        private void ExecutePlayerAction(CombatActionId actionId)
        {
            if (actionRegistry == null)
            {
                Debug.LogError("BattleController: actionRegistry is not initialized");
                return;
            }

            var action = actionRegistry.Get(actionId);
            if (action == null)
            {
                Debug.LogError($"Action not found: {actionId}");
                return;
            }

            var resolution = combatEngine.ResolvePlayerAction(combatState, action);

            if (resolution.Result != CombatActionResult.Executed)
            {
                Debug.Log($"Action rejected: {resolution.Result}");
                return;
            }

            combatState = resolution.State;
            PushHudState();

            if (combatState.IsEnemyDead)
            {
                FinishBattle(playerWon: true);
                return;
            }

            if (combatState.IsPlayerDead)
            {
                FinishBattle(playerWon: false);
                return;
            }
        }

        private void FinishBattle(bool playerWon)
        {
            battleStarted = false;

            // TODO: Здесь позже подключим реальную генерацию наград (drops/gold) из EnemyData.
            var result = new BattleResultData(
                playerWon: playerWon,
                goldGained: 0,
                itemIds: System.Array.Empty<string>()
            );

            if (resultModal != null)
            {
                resultModal.Show(result, ExitBattle);
            }
            else
            {
                ExitBattle();
            }
        }

        private void ExitBattle()
        {
            var targetScene = ResolveReturnSceneName();
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogError("BattleController: Cannot exit battle, return scene is not set");
                return;
            }

            if (UDA2.SceneFlow.SceneFlowManager.Instance != null)
                UDA2.SceneFlow.SceneFlowManager.Instance.LoadScene(targetScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }

        private string ResolveReturnSceneName()
        {
            if (context != null && context.Mode == BattleMode.Tutorial)
                return tutorialReturnSceneName;

            var exit = BattleExitContext.Consume();
            if (!string.IsNullOrEmpty(exit?.ReturnSceneName))
                return exit.ReturnSceneName;

            // Fallbacks for cases when battle scene is launched directly (e.g. from editor) or
            // when the caller forgot to set BattleExitContext before loading the battle scene.
            var currentScene = SceneManager.GetActiveScene().name;

            var saveScene = GameState.Instance?.CurrentSave?.player?.sceneName;
            if (!string.IsNullOrEmpty(saveScene) && !string.Equals(saveScene, currentScene, StringComparison.Ordinal))
            {
                Debug.LogWarning($"BattleController: BattleExitContext was not set. Falling back to save.player.sceneName='{saveScene}'.");
                return saveScene;
            }

            if (!string.IsNullOrEmpty(defaultReturnSceneName) && !string.Equals(defaultReturnSceneName, currentScene, StringComparison.Ordinal))
            {
                Debug.LogWarning($"BattleController: BattleExitContext was not set. Falling back to defaultReturnSceneName='{defaultReturnSceneName}'.");
                return defaultReturnSceneName;
            }

            return null;
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
