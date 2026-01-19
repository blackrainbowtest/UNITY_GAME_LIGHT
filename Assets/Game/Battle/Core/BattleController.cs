using System.Collections;
using UnityEngine;
using Game.Battle.Combat;
using Game.Battle.Combat.Actions;
using Game.Battle.Combat.EnemyAI;

using Game.Battle.UI;

namespace Game.Battle
{
    public partial class BattleController : MonoBehaviour, IBattleUIActions
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
        [SerializeField] private float endOfRoundDelaySeconds = 0.8f;

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

            var startPlayerHp = Mathf.Clamp(context.Player.CurrentHP, 0, context.Player.MaxHP);
            var startPlayerMp = Mathf.Clamp(context.Player.CurrentMP, 0, context.Player.MaxMP);
            var startPlayerSp = Mathf.Clamp(context.Player.CurrentSP, 0, context.Player.MaxSP);
            var startPlayerLp = Mathf.Clamp(context.Player.CurrentLP, 0, context.Player.MaxLP);

            var startEnemyHp = Mathf.Clamp(context.Enemy.hp, 0, context.Enemy.maxHp);
            var startEnemyMp = Mathf.Clamp(context.Enemy.mp, 0, context.Enemy.maxMp);
            var startEnemySp = Mathf.Clamp(context.Enemy.sp, 0, context.Enemy.maxSp);
            var startEnemyLp = Mathf.Clamp(context.Enemy.lp, 0, context.Enemy.maxLp);

            combatState = new CombatState(
                playerHp: startPlayerHp,
                playerMp: startPlayerMp,
                playerSp: startPlayerSp,
                playerLp: startPlayerLp,
                enemyHp: startEnemyHp,
                enemyMp: startEnemyMp,
                enemySp: startEnemySp,
                enemyLp: startEnemyLp,
                playerBlockedLastTurn: false // первый ход — не блокировал
            );

            Debug.Log("Battle started");
            Debug.Log($"Enemy: {context.Enemy.name}");
            Debug.Log($"Mode: {context.Mode}");

            InitializeParticipants();
            InitializeEnvironment();
            InitializeUI();

            ClearEndOfRoundEffects();
            ClearAllStatuses();

            PushHudState();

            BeginPlayerTurn();
        }

        private void BeginPlayerTurn()
        {
            if (!battleStarted)
                return;

            turnPhase = TurnPhase.PlayerTurn;
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
            ApplyPostActionEffects(actionId, actorIsPlayer: true);
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
            // Give time for the player's damage popup to play before enemy acts.
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

            // Give time for the enemy's damage popup to play before applying end-of-round effects.
            if (endOfRoundDelaySeconds > 0f)
                yield return new WaitForSeconds(endOfRoundDelaySeconds);

            ApplyEndOfRoundEffects();
            PushHudState();

            BeginPlayerTurn();
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
            ApplyPostActionEffects(actionId.Value, actorIsPlayer: false);
            PushHudState();

            if (combatState.IsPlayerDead)
            {
                FinishBattle(playerWon: false);
            }
        }
    }
}
