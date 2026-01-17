using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Battle.Combat;
using Game.Battle.Combat.Actions;
using Game.Battle.Combat.EnemyAI;

using Game.Battle.UI;

namespace Game.Battle
{
    public class BattleController : MonoBehaviour, IBattleUIActions
    {
        private enum TurnPhase
        {
            NotStarted = 0,
            PlayerTurn = 1,
            EnemyTurn = 2,
            BattleOver = 3
        }

        private BattleContext context;
        private bool battleStarted;

        private TurnPhase turnPhase = TurnPhase.NotStarted;
        private Coroutine enemyTurnRoutine;
        private readonly System.Random rng = new System.Random();

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

        [Header("Turns")]
        [SerializeField] private float enemyTurnDelaySeconds = 0.35f;

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
            turnPhase = TurnPhase.PlayerTurn;

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

            BeginPlayerTurn();
        }

        private void BeginPlayerTurn()
        {
            if (!battleStarted)
                return;

            turnPhase = TurnPhase.PlayerTurn;
            ApplyPlayerPassiveRegen();
            PushHudState();
            hudController?.SetInputEnabled(true);
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

            if (turnPhase != TurnPhase.PlayerTurn)
                return;

            ExecutePlayerAction(CombatActionId.NormalAttack);
        }

        public void OnCombatActionSelected(CombatActionId actionId)
        {
            if (!battleStarted)
                return;

            if (turnPhase != TurnPhase.PlayerTurn)
                return;

            ExecutePlayerAction(actionId);
        }

        public void OnItemPressed()
        {
            Debug.Log("Item pressed");
        }

        public void OnRunPressed()
        {
            if (!battleStarted)
                return;

            Debug.Log("BattleController: Run pressed (escape placeholder)");
            battleStarted = false;
            ExitBattle();
        }

        public void OnSurrenderPressed()
        {
            if (!battleStarted)
                return;

            Debug.Log("BattleController: Surrender pressed");
            FinishBattle(playerWon: false);
        }

        public void OnSkipTurnPressed()
        {
            if (!battleStarted)
                return;

            if (turnPhase != TurnPhase.PlayerTurn)
                return;

            BeginEnemyTurn();
        }

        public void OnExitPressed()
        {
            Debug.Log("Exit pressed");
        }
        
        private void ExecutePlayerAction(CombatActionId actionId)
        {
            if (turnPhase != TurnPhase.PlayerTurn)
                return;

            // Lock input immediately to prevent spamming while enemy is about to act.
            turnPhase = TurnPhase.EnemyTurn;
            hudController?.SetInputEnabled(false);

            if (actionRegistry == null)
            {
                Debug.LogError("BattleController: actionRegistry is not initialized");

                turnPhase = TurnPhase.PlayerTurn;
                hudController?.SetInputEnabled(true);
                return;
            }

            var action = actionRegistry.Get(actionId);
            if (action == null)
            {
                Debug.LogError($"Action not found: {actionId}");

                turnPhase = TurnPhase.PlayerTurn;
                hudController?.SetInputEnabled(true);
                return;
            }

            var resolution = combatEngine.ResolvePlayerAction(combatState, action);

            if (resolution.Result != CombatActionResult.Executed)
            {
                Debug.Log($"Action rejected: {resolution.Result}");

                turnPhase = TurnPhase.PlayerTurn;
                hudController?.SetInputEnabled(true);
                return;
            }

            combatState = ClampPlayerResourcesToMax(resolution.State);
            PushHudState();

            if (combatState.IsEnemyDead)
            {
                FinishBattle(playerWon: true);
                return;
            }

            BeginEnemyTurn();
        }

        private void BeginEnemyTurn()
        {
            if (!battleStarted)
                return;

            if (enemyTurnRoutine != null)
                return;

            turnPhase = TurnPhase.EnemyTurn;
            hudController?.SetInputEnabled(false);
            enemyTurnRoutine = StartCoroutine(EnemyTurnRoutine());
        }

        private IEnumerator EnemyTurnRoutine()
        {
            ApplyEnemyPassiveRegen();
            PushHudState();

            if (enemyTurnDelaySeconds > 0f)
                yield return new WaitForSeconds(enemyTurnDelaySeconds);

            ExecuteEnemyTurn();

            enemyTurnRoutine = null;

            if (!battleStarted)
                yield break;

            if (combatState.IsPlayerDead)
            {
                FinishBattle(playerWon: false);
                yield break;
            }

            BeginPlayerTurn();
        }

        private void ApplyPlayerPassiveRegen()
        {
            if (context?.Player == null)
                return;

            // LP does not regenerate.
            var hp = Mathf.Clamp(combatState.PlayerHp + Mathf.Max(0, context.Player.RegenHpPerTurn), 0, context.Player.MaxHP);
            var mp = Mathf.Clamp(combatState.PlayerMp + Mathf.Max(0, context.Player.RegenMpPerTurn), 0, context.Player.MaxMP);
            var sp = Mathf.Clamp(combatState.PlayerSp + Mathf.Max(0, context.Player.RegenSpPerTurn), 0, context.Player.MaxSP);

            combatState = combatState
                .WithPlayerHp(hp)
                .WithPlayerMp(mp)
                .WithPlayerSp(sp);
        }

        private void ApplyEnemyPassiveRegen()
        {
            if (context?.Enemy == null)
                return;

            // LP does not regenerate.
            var hp = Mathf.Clamp(combatState.EnemyHp + Mathf.Max(0, context.Enemy.regenHpPerTurn), 0, context.Enemy.maxHp);
            var mp = Mathf.Clamp(combatState.EnemyMp + Mathf.Max(0, context.Enemy.regenMpPerTurn), 0, context.Enemy.maxMp);
            var sp = Mathf.Clamp(combatState.EnemySp + Mathf.Max(0, context.Enemy.regenSpPerTurn), 0, context.Enemy.maxSp);

            combatState = combatState
                .WithEnemyHp(hp)
                .WithEnemyMp(mp)
                .WithEnemySp(sp);
        }

        private void ExecuteEnemyTurn()
        {
            if (!battleStarted)
                return;

            if (combatEngine == null || actionRegistry == null)
            {
                Debug.LogError("BattleController: combatEngine/actionRegistry not initialized");
                return;
            }

            if (combatState.IsEnemyDead || combatState.IsPlayerDead)
                return;

            var actionId = EnemyActionSelector.SelectEnemyAction(
                context.EnemyDifficulty,
                context.Enemy,
                actionRegistry,
                combatState,
                rng);

            if (!actionId.HasValue)
            {
                Debug.Log("[BattleController] Enemy skips turn (no affordable/allowed actions)");
                return;
            }

            var action = actionRegistry.Get(actionId.Value);
            if (action == null)
            {
                Debug.LogError($"[BattleController] Enemy action not found in registry: {actionId.Value}");
                return;
            }

            var resolution = combatEngine.ResolveEnemyAction(combatState, action);
            if (resolution.Result != CombatActionResult.Executed)
            {
                Debug.Log($"[BattleController] Enemy action rejected: {action.Id} -> {resolution.Result}");
                return;
            }

            combatState = ClampEnemyResourcesToMax(resolution.State);
            PushHudState();

            if (combatState.IsPlayerDead)
            {
                FinishBattle(playerWon: false);
            }
        }

        private CombatState ClampPlayerResourcesToMax(CombatState state)
        {
            if (context?.Player == null)
                return state;

            var clampedMp = Mathf.Clamp(state.PlayerMp, 0, context.Player.MaxMP);
            var clampedSp = Mathf.Clamp(state.PlayerSp, 0, context.Player.MaxSP);
            var clampedLp = Mathf.Clamp(state.PlayerLp, 0, context.Player.MaxLP);

            if (clampedMp == state.PlayerMp && clampedSp == state.PlayerSp && clampedLp == state.PlayerLp)
                return state;

            return state
                .WithPlayerMp(clampedMp)
                .WithPlayerSp(clampedSp)
                .WithPlayerLp(clampedLp);
        }

        private CombatState ClampEnemyResourcesToMax(CombatState state)
        {
            if (context?.Enemy == null)
                return state;

            var clampedMp = Mathf.Clamp(state.EnemyMp, 0, context.Enemy.maxMp);
            var clampedSp = Mathf.Clamp(state.EnemySp, 0, context.Enemy.maxSp);
            var clampedLp = Mathf.Clamp(state.EnemyLp, 0, context.Enemy.maxLp);

            if (clampedMp == state.EnemyMp && clampedSp == state.EnemySp && clampedLp == state.EnemyLp)
                return state;

            return state
                .WithEnemyMp(clampedMp)
                .WithEnemySp(clampedSp)
                .WithEnemyLp(clampedLp);
        }

        private void FinishBattle(bool playerWon)
        {
            battleStarted = false;
            turnPhase = TurnPhase.BattleOver;
            hudController?.SetInputEnabled(false);

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
