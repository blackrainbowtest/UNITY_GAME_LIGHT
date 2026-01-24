//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\Battle\_Core\BattleController.cs                                                  */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:39:53 by UDA                                                                    */
/*   Updated: 2026/01/23 01:39:53 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

using System.Collections;
using UnityEngine;
using Game.Battle.Combat;
using Game.Battle.Combat.Actions;
using Game.Battle.Combat.EnemyAI;
using Logger = UDA2.Logging.Logger;

using Game.Battle.UI;
using Game.Battle.Visual;

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

        [Header("Visuals (Optional)")]
        [SerializeField] private BattleCharacterView playerView;
        [SerializeField] private BattleCharacterView enemyView;

        [SerializeField] private CharacterVisualProfile playerVisualProfile;

        [Header("Exit")]
        [SerializeField] private string tutorialReturnSceneName = "StartCityScene";
        [SerializeField] private string defaultReturnSceneName = "StartCityScene";

        [Header("Turns")]
        [SerializeField] private float enemyTurnDelaySeconds = 0.35f;
        [SerializeField] private float endOfRoundDelaySeconds = 0.8f;

        public void StartBattle(BattleContext battleContext)
        {
            Logger.LogInfo("[BattleController] StartBattle called");
            if (battleStarted)
            {
                Logger.LogWarning("BattleController: Battle already started");
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

            Logger.LogInfo("Battle started");
            Logger.LogInfo($"Enemy: {context.Enemy.name}");
            Logger.LogInfo($"Mode: {context.Mode}");

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
            Logger.LogInfo("Initializing participants");

            // Optional visuals: play idle loops if views are wired in the scene.
            if (playerView != null && context != null && context.Player != null)
            {
                playerView.SetVisualProfile(playerVisualProfile);
                playerView.SetOutfitId(context.Player.OutfitId);
            }

            if (enemyView != null && context != null && context.Enemy != null)
            {
                enemyView.SetVisualProfile(context.Enemy.visualProfile);
                enemyView.SetOutfitId(context.Enemy.outfitId);
                enemyView.SetIdleAnimation(context.Enemy.idleAnimation);
            }

            playerView?.PlayIdle();
            enemyView?.PlayIdle();
        }

        private void Start()
        {
            BindHUD(hudController);
        }

        private void InitializeEnvironment()
        {
            Logger.LogInfo("Initializing environment");
            if (environmentController == null)
            {
                Logger.LogError("BattleController: BattleEnvironmentController not assigned");
                return;
            }
            environmentController.Apply(context.Location);
        }

        private void InitializeUI()
        {
            Logger.LogInfo("Initializing UI");
        }

        /// <summary>
        /// Binds the HUD to battle actions. Passes IBattleUIActions implementation to the HUD.
        /// </summary>
        private void BindHUD(BattleHUDController hud)
        {
            if (hud == null)
            {
                Logger.LogError("BattleController: HUDController not assigned");
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
            Logger.LogInfo("Item pressed");
        }

        public void OnRunPressed()
        {
            if (!battleStarted)
                return;

            Logger.LogInfo("BattleController: Run pressed (escape placeholder)");
            battleStarted = false;
            ExitBattle();
        }

        public void OnSurrenderPressed()
        {
            if (!battleStarted)
                return;

            Logger.LogInfo("BattleController: Surrender pressed");
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
            if (!battleStarted)
                return;

            Logger.LogInfo("BattleController: Exit pressed");
            battleStarted = false;
            ExitBattle();
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
                Logger.LogError("BattleController: actionRegistry is not initialized");

                turnPhase = TurnPhase.PlayerTurn;
                hudController?.SetInputEnabled(true);
                return;
            }

            var action = actionRegistry.Get(actionId);
            if (action == null)
            {
                Logger.LogError($"Action not found: {actionId}");

                turnPhase = TurnPhase.PlayerTurn;
                hudController?.SetInputEnabled(true);
                return;
            }

            var resolution = combatEngine.ResolvePlayerAction(combatState, action);

            if (resolution.Result != CombatActionResult.Executed)
            {
                Logger.LogInfo($"Action rejected: {resolution.Result}");

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
                Logger.LogError("BattleController: combatEngine/actionRegistry not initialized");
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
                Logger.LogInfo("[BattleController] Enemy skips turn (no affordable/allowed actions)");
                return;
            }

            var action = actionRegistry.Get(actionId.Value);
            if (action == null)
            {
                Logger.LogError($"[BattleController] Enemy action not found in registry: {actionId.Value}");
                return;
            }

            var resolution = combatEngine.ResolveEnemyAction(combatState, action);
            if (resolution.Result != CombatActionResult.Executed)
            {
                Logger.LogInfo($"[BattleController] Enemy action rejected: {action.Id} -> {resolution.Result}");
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
