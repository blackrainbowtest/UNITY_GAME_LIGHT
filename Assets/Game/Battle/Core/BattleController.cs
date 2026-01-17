using System;
using System.Collections;
using System.Collections.Generic;
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
        private readonly struct EndOfRoundEffect
        {
            public string SourceId { get; }

            public int PlayerHpDelta { get; }
            public int PlayerMpDelta { get; }
            public int PlayerSpDelta { get; }
            public int PlayerLpDelta { get; }

            public int EnemyHpDelta { get; }
            public int EnemyMpDelta { get; }
            public int EnemySpDelta { get; }
            public int EnemyLpDelta { get; }

            public EndOfRoundEffect(
                string sourceId,
                int playerHpDelta, int playerMpDelta, int playerSpDelta, int playerLpDelta,
                int enemyHpDelta, int enemyMpDelta, int enemySpDelta, int enemyLpDelta)
            {
                SourceId = sourceId;

                PlayerHpDelta = playerHpDelta;
                PlayerMpDelta = playerMpDelta;
                PlayerSpDelta = playerSpDelta;
                PlayerLpDelta = playerLpDelta;

                EnemyHpDelta = enemyHpDelta;
                EnemyMpDelta = enemyMpDelta;
                EnemySpDelta = enemySpDelta;
                EnemyLpDelta = enemyLpDelta;
            }
        }

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

        private readonly List<EndOfRoundEffect> pendingEndOfRoundEffects = new List<EndOfRoundEffect>(8);

        /// <summary>
        /// Queues end-of-round resource changes (poison/burn/auras/etc).
        /// All queued effects are summed and applied once in ApplyEndOfRoundEffects(), so HUD shows one net delta.
        /// </summary>
        public void QueueEndOfRoundEffect(
            string sourceId,
            int playerHpDelta = 0,
            int playerMpDelta = 0,
            int playerSpDelta = 0,
            int playerLpDelta = 0,
            int enemyHpDelta = 0,
            int enemyMpDelta = 0,
            int enemySpDelta = 0,
            int enemyLpDelta = 0)
        {
            pendingEndOfRoundEffects.Add(new EndOfRoundEffect(
                sourceId,
                playerHpDelta, playerMpDelta, playerSpDelta, playerLpDelta,
                enemyHpDelta, enemyMpDelta, enemySpDelta, enemyLpDelta));
        }

        public void ClearEndOfRoundEffects()
        {
            pendingEndOfRoundEffects.Clear();
        }

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

            var beforePlayerMp = combatState.PlayerMp;
            var beforePlayerSp = combatState.PlayerSp;
            var beforePlayerLp = combatState.PlayerLp;
            var beforeEnemyHp = combatState.EnemyHp;

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

        private void ApplyEndOfRoundEffects()
        {
            // End-of-round effects are applied as a single batch so HUD shows one net delta.

            var totalPlayerHpDelta = 0;
            var totalPlayerMpDelta = 0;
            var totalPlayerSpDelta = 0;
            var totalPlayerLpDelta = 0;

            var totalEnemyHpDelta = 0;
            var totalEnemyMpDelta = 0;
            var totalEnemySpDelta = 0;
            var totalEnemyLpDelta = 0;

            // 1) External queued effects (poison/burn/auras/etc).
            for (var i = 0; i < pendingEndOfRoundEffects.Count; i++)
            {
                var e = pendingEndOfRoundEffects[i];
                totalPlayerHpDelta += e.PlayerHpDelta;
                totalPlayerMpDelta += e.PlayerMpDelta;
                totalPlayerSpDelta += e.PlayerSpDelta;
                totalPlayerLpDelta += e.PlayerLpDelta;

                totalEnemyHpDelta += e.EnemyHpDelta;
                totalEnemyMpDelta += e.EnemyMpDelta;
                totalEnemySpDelta += e.EnemySpDelta;
                totalEnemyLpDelta += e.EnemyLpDelta;
            }

            // 2) Passive regen (treated as part of end-of-round batch).
            if (context?.Player != null)
            {
                totalPlayerHpDelta += Mathf.Max(0, context.Player.RegenHpPerTurn);
                totalPlayerMpDelta += Mathf.Max(0, context.Player.RegenMpPerTurn);
                totalPlayerSpDelta += Mathf.Max(0, context.Player.RegenSpPerTurn);
            }

            if (context?.Enemy != null)
            {
                totalEnemyHpDelta += Mathf.Max(0, context.Enemy.regenHpPerTurn);
                totalEnemyMpDelta += Mathf.Max(0, context.Enemy.regenMpPerTurn);
                totalEnemySpDelta += Mathf.Max(0, context.Enemy.regenSpPerTurn);
            }

            // Apply totals (clamped to max pools).
            if (context?.Player != null)
            {
                var hp = Mathf.Clamp(combatState.PlayerHp + totalPlayerHpDelta, 0, context.Player.MaxHP);
                var mp = Mathf.Clamp(combatState.PlayerMp + totalPlayerMpDelta, 0, context.Player.MaxMP);
                var sp = Mathf.Clamp(combatState.PlayerSp + totalPlayerSpDelta, 0, context.Player.MaxSP);
                var lp = Mathf.Clamp(combatState.PlayerLp + totalPlayerLpDelta, 0, context.Player.MaxLP);

                combatState = combatState
                    .WithPlayerHp(hp)
                    .WithPlayerMp(mp)
                    .WithPlayerSp(sp)
                    .WithPlayerLp(lp);
            }

            if (context?.Enemy != null)
            {
                var hp = Mathf.Clamp(combatState.EnemyHp + totalEnemyHpDelta, 0, context.Enemy.maxHp);
                var mp = Mathf.Clamp(combatState.EnemyMp + totalEnemyMpDelta, 0, context.Enemy.maxMp);
                var sp = Mathf.Clamp(combatState.EnemySp + totalEnemySpDelta, 0, context.Enemy.maxSp);
                var lp = Mathf.Clamp(combatState.EnemyLp + totalEnemyLpDelta, 0, context.Enemy.maxLp);

                combatState = combatState
                    .WithEnemyHp(hp)
                    .WithEnemyMp(mp)
                    .WithEnemySp(sp)
                    .WithEnemyLp(lp);
            }

            pendingEndOfRoundEffects.Clear();
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

            var clampedHp = Mathf.Clamp(state.PlayerHp, 0, context.Player.MaxHP);
            var clampedMp = Mathf.Clamp(state.PlayerMp, 0, context.Player.MaxMP);
            var clampedSp = Mathf.Clamp(state.PlayerSp, 0, context.Player.MaxSP);
            var clampedLp = Mathf.Clamp(state.PlayerLp, 0, context.Player.MaxLP);

            if (clampedHp == state.PlayerHp && clampedMp == state.PlayerMp && clampedSp == state.PlayerSp && clampedLp == state.PlayerLp)
                return state;

            return state
                .WithPlayerHp(clampedHp)
                .WithPlayerMp(clampedMp)
                .WithPlayerSp(clampedSp)
                .WithPlayerLp(clampedLp);
        }

        private CombatState ClampEnemyResourcesToMax(CombatState state)
        {
            if (context?.Enemy == null)
                return state;

            var clampedHp = Mathf.Clamp(state.EnemyHp, 0, context.Enemy.maxHp);
            var clampedMp = Mathf.Clamp(state.EnemyMp, 0, context.Enemy.maxMp);
            var clampedSp = Mathf.Clamp(state.EnemySp, 0, context.Enemy.maxSp);
            var clampedLp = Mathf.Clamp(state.EnemyLp, 0, context.Enemy.maxLp);

            if (clampedHp == state.EnemyHp && clampedMp == state.EnemyMp && clampedSp == state.EnemySp && clampedLp == state.EnemyLp)
                return state;

            return state
                .WithEnemyHp(clampedHp)
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
        private void PushHudState(bool showDeltas = true)
        {
            var hudState = BattleHUDStateFactory.Create(
                context.Player,
                context.Enemy,
                combatState
            );
            hudController?.UpdateState(hudState, showDeltas);
        }
    }
}
