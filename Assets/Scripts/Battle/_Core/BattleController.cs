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
using Game.Battle.Statuses;
using Logger = UDA2.Logging.Logger;

using Game.Battle.UI;
using Game.Battle.Visual;

namespace Game.Battle
{
    public partial class BattleController : MonoBehaviour, IBattleUIActions
    {
        private Coroutine playerActionRoutine;

        private BattleContext context;
        private bool battleStarted;
        private BattleEscapeSystem escapeSystem;
        private BattleVisualExecutor visualExecutor;
        private BattleEndConditionSystem endConditionSystem;
        private BattleTurnSystem turnSystem;
        private BattleActionResolutionSystem actionResolutionSystem;
        private BattleInputCommandHandler inputCommandHandler;
        private BattleTurnFlowRunner turnFlowRunner;

        private Coroutine enemyTurnRoutine;
        private readonly System.Random rng = new System.Random();

        private CombatState combatState;
        private BattleCombatEngine combatEngine;
        private CombatActionRegistry actionRegistry;

        [Header("Scene References")]
        [SerializeField] private BattleEnvironmentController environmentController;
        [SerializeField] private BattleHUDController hudController;
        [Tooltip("Optional. If assigned, all battle modals instantiated at runtime will be parented here (e.g. Canvas/Modals).")]
        [SerializeField] private Transform modalsRoot;
        [Tooltip("Battle results modal reference. Can be either a scene instance (Hierarchy) OR a prefab asset (Project). If a prefab is assigned, BattleController will instantiate it into the scene at runtime.")]
        [SerializeField] private BattleResultModalController resultModal;
        [Tooltip("Outcome animation modal reference. Can be either a scene instance (Hierarchy) OR a prefab asset (Project). If a prefab is assigned, BattleController will instantiate it into the scene at runtime.")]
        [SerializeField] private BattleOutcomeAnimationModalController outcomeAnimationModal;
        [Tooltip("Centralized status definitions (icons, localization keys, resource effects).")]
        [SerializeField] private BattleStatusCatalog statusCatalog;
        private bool warnedMissingStatusCatalog;

        [Header("Visuals (Optional)")]
        [SerializeField] private BattleCharacterView playerView;
        [SerializeField] private BattleCharacterView enemyView;

        [Tooltip("Optional parent for spawned spell projectiles (keeps hierarchy tidy).")]
        [SerializeField] private Transform projectilesRoot;

        [SerializeField] private CharacterVisualProfile playerVisualProfile;

        [Header("Exit")]
        [SerializeField] private string tutorialReturnSceneName = "StartCityScene";
        [SerializeField] private string defaultReturnSceneName = "StartCityScene";

        [Header("Turns")]
        [SerializeField] private float enemyTurnDelaySeconds = 0.35f;
        [SerializeField] private float endOfRoundDelaySeconds = 0.8f;

        [Header("Escape")]
        [Range(0f, 1f)]
        [SerializeField] private float minEscapeChance = 0.05f;
        [Range(0f, 1f)]
        [SerializeField] private float maxEscapeChance = 0.95f;
        [SerializeField] private BattleVisualAnimId escapeSuccessAnim = BattleVisualAnimId.ActionAct2;
        [Range(0f, 1f)]
        [SerializeField] private float escapeStaminaWeight = 0.6f;
        [Range(0f, 1f)]
        [SerializeField] private float escapeLustWeight = 0.4f;
        [Header("Escape Failed Sequence")]
        private Coroutine escapeSuccessRoutine;
        [SerializeField] private BattleVisualAnimId escapeFailFallAnim = BattleVisualAnimId.ActionActFail;
        [SerializeField] private int escapeFailLpPenalty = 20;

        private Coroutine escapeFailedRoutine;
        private Coroutine surrenderRoutine;

        private void Awake()
        {
            EnsureEscapeSystem();
            EnsureVisualExecutor();
            EnsureEndConditionSystem();
            EnsureTurnSystem();
            EnsureActionResolutionSystem();
            EnsureInputCommandHandler();

            if (resultModal == null)
            {
#if UNITY_2023_1_OR_NEWER
                resultModal = FindAnyObjectByType<BattleResultModalController>(FindObjectsInactive.Include);
#elif UNITY_2022_2_OR_NEWER
                resultModal = FindAnyObjectByType<BattleResultModalController>(FindObjectsInactive.Include);
#else
                resultModal = FindObjectOfType<BattleResultModalController>(includeInactive: true);
#endif
            }

            if (outcomeAnimationModal == null)
            {
#if UNITY_2023_1_OR_NEWER
                outcomeAnimationModal = FindAnyObjectByType<BattleOutcomeAnimationModalController>(FindObjectsInactive.Include);
#elif UNITY_2022_2_OR_NEWER
                outcomeAnimationModal = FindAnyObjectByType<BattleOutcomeAnimationModalController>(FindObjectsInactive.Include);
#else
                outcomeAnimationModal = FindObjectOfType<BattleOutcomeAnimationModalController>(includeInactive: true);
#endif
            }

            // If a prefab-asset (Project) was accidentally assigned instead of a scene instance,
            // Unity will refuse parenting/spawning under it ("Prefab Asset" warnings).
            // Auto-instantiate such references into the battle scene Canvas.
            resultModal = EnsureSceneInstance(resultModal, "resultModal");
            outcomeAnimationModal = EnsureSceneInstance(outcomeAnimationModal, "outcomeAnimationModal");

            if (statusCatalog == null && hudController != null)
                statusCatalog = hudController.GetStatusCatalog();
        }

        private Transform ResolveModalParent()
        {
            if (modalsRoot != null)
                return modalsRoot;

            // Prefer HUD's canvas.
            if (hudController != null)
            {
                var canvas = hudController.GetComponentInParent<Canvas>(includeInactive: true);
                if (canvas != null)
                    return canvas.transform;
            }

            // Fallback: any canvas in scene.
#if UNITY_2023_1_OR_NEWER
            var anyCanvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
#else
            var anyCanvas = FindObjectOfType<Canvas>(includeInactive: true);
#endif
            if (anyCanvas != null)
                return anyCanvas.transform;

            return null;
        }

        private T EnsureSceneInstance<T>(T modal, string fieldName) where T : MonoBehaviour
        {
            if (modal == null)
                return null;

            bool looksLikePrefabAsset = false;

            // Runtime heuristic: prefab assets typically have no real scene path and are not loaded.
            // (This also covers objects from Prefab Stage.)
            var scene = modal.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
                looksLikePrefabAsset = true;

            // Persistent "DontDestroyOnLoad" scene is also wrong for battle UI.
            if (string.Equals(scene.name, "DontDestroyOnLoad", System.StringComparison.OrdinalIgnoreCase))
                looksLikePrefabAsset = true;

#if UNITY_EDITOR
            // More reliable in Editor.
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(modal))
                looksLikePrefabAsset = true;
#endif

            if (!looksLikePrefabAsset)
                return modal;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Logger.LogInfo($"[BattleController] '{fieldName}' is a prefab/persistent reference ('{modal.name}'). Instantiating a scene instance for battle UI.");
#endif

            var parent = ResolveModalParent();
            var instance = Instantiate(modal);
            instance.name = modal.name; // keep readable name

            // Keep hidden immediately (prevents any accidental visible frame on battle start).
            instance.gameObject.SetActive(false);

            if (parent != null)
            {
                instance.transform.SetParent(parent, worldPositionStays: false);
            }
            else
            {
                Logger.LogWarning($"[BattleController] No modal parent resolved for '{fieldName}'. Assign BattleController.modalsRoot (Canvas/Modals) or ensure there is an active Canvas in the scene.");
            }

            return instance;
        }

        private void OnDisable()
        {
            if (enemyTurnRoutine != null)
            {
                StopCoroutine(enemyTurnRoutine);
                enemyTurnRoutine = null;
            }
        }

        public void StartBattle(BattleContext battleContext)
        {
            Logger.LogInfo("[BattleController] StartBattle called");
            if (battleStarted)
            {
                Logger.LogWarning("BattleController: Battle already started");
                return;
            }

            resolvedReturnSceneName = null;

            context = battleContext;
            battleStarted = true;
            EnsureTurnSystem().Reset();

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

            EnsureTurnSystem().BeginPlayerTurn();
            ResetPerTurnNonCombatActions();
            hudController?.SetInputEnabled(true);
        }

        private void RestorePlayerInputWithoutNewTurnReset()
        {
            EnsureTurnSystem().BeginPlayerTurn();
            hudController?.SetInputEnabled(true);
        }

        private BattleEscapeSystem EnsureEscapeSystem()
        {
            if (escapeSystem == null)
                escapeSystem = new BattleEscapeSystem(minEscapeChance, maxEscapeChance, escapeStaminaWeight, escapeLustWeight);
            return escapeSystem;
        }

        private BattleVisualExecutor EnsureVisualExecutor()
        {
            if (visualExecutor == null)
                visualExecutor = new BattleVisualExecutor(playerView, enemyView, projectilesRoot);
            return visualExecutor;
        }

        private BattleEndConditionSystem EnsureEndConditionSystem()
        {
            if (endConditionSystem == null)
                endConditionSystem = new BattleEndConditionSystem();
            return endConditionSystem;
        }

        private BattleTurnFlowRunner EnsureTurnFlowRunner()
        {
            var endSystem = EnsureEndConditionSystem();
            if (turnFlowRunner == null)
                turnFlowRunner = new BattleTurnFlowRunner(endSystem);

            return turnFlowRunner;
        }

        private BattleTurnSystem EnsureTurnSystem()
        {
            if (turnSystem == null)
                turnSystem = new BattleTurnSystem();
            return turnSystem;
        }

        private BattleActionResolutionSystem EnsureActionResolutionSystem()
        {
            if (actionResolutionSystem == null)
                actionResolutionSystem = new BattleActionResolutionSystem();
            return actionResolutionSystem;
        }

        private BattleInputCommandHandler EnsureInputCommandHandler()
        {
            if (inputCommandHandler == null)
            {
                inputCommandHandler = new BattleInputCommandHandler(
                    isBattleStarted: () => battleStarted,
                    isPlayerTurn: () => turnSystem != null && turnSystem.IsPlayerTurn,
                    onAttack: HandleAttackPressed,
                    onCombatActionSelected: HandleCombatActionSelected,
                    onItem: HandleItemPressed,
                    onRun: HandleRunPressed,
                    onSurrender: HandleSurrenderPressed,
                    onSkipTurn: HandleSkipTurnPressed,
                    onExit: HandleExitPressed);
            }

            return inputCommandHandler;
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
        private void ExecutePlayerAction(CombatActionId actionId)
        {
            if (!EnsureTurnSystem().IsPlayerTurn)
                return;

            if (playerActionRoutine != null)
                return;

            // Lock input immediately to prevent spamming while enemy is about to act.
            EnsureTurnSystem().BeginEnemyTurn();
            hudController?.SetInputEnabled(false);

            playerActionRoutine = StartCoroutine(ExecutePlayerActionRoutine(actionId));
        }

        private IEnumerator ExecutePlayerActionRoutine(CombatActionId actionId)
        {
            if (!EnsureActionResolutionSystem().TryResolvePlayerAction(
                combatEngine,
                actionRegistry,
                combatState,
                actionId,
                out var action,
                out var resolution,
                out var failure))
            {
                if (failure == BattleActionResolveFailure.BattleSystemsNotInitialized)
                {
                    Logger.LogError("BattleController: actionRegistry is not initialized");
                }
                else if (failure == BattleActionResolveFailure.ActionNotFound)
                {
                    Logger.LogError($"Action not found: {actionId}");
                }
                else if (failure == BattleActionResolveFailure.ActionRejected)
                {
                    var rejectedResult = resolution != null ? resolution.Result.ToString() : "Unknown";
                    Logger.LogInfo($"Action rejected: {rejectedResult}");
                }

                RestorePlayerInputWithoutNewTurnReset();
                playerActionRoutine = null;
                yield break;
            }

            // 1) Play attacker animation and (if HP damage happened) target Hit starting together.
            yield return EnsureVisualExecutor().PlayActionWithTargetHitAndWait(actionId, actorIsPlayer: true, combatState, resolution.State);

            // 2) Apply results after animation.
            combatState = ClampPlayerResourcesToMax(resolution.State);
            ApplyPostActionEffects(actionId, actorIsPlayer: true);

            if (combatState.EnemyBlockArmor <= 0)
                RemoveStatus(enemyStatuses, StatusEffectId.Block);

            PushHudState();

            playerActionRoutine = null;

            if (EnsureTurnFlowRunner().TryResolveAfterPlayerAction(context, combatState, actionId, out var playerActionResolution))
            {
                FinishBattle(playerActionResolution.PlayerWon, playerActionResolution.Reason, playerActionResolution.WinningActionId);
                yield break;
            }

            BeginEnemyTurn();
            yield break;
        }

        private void BeginEnemyTurn()
        {
            if (!battleStarted)
                return;

            if (enemyTurnRoutine != null)
                return;

            EnsureTurnSystem().BeginEnemyTurn();
            hudController?.SetInputEnabled(false);
            enemyTurnRoutine = StartCoroutine(EnemyTurnRoutine());
        }

        private IEnumerator EnemyTurnRoutine()
        {
            // Give time for the player's damage popup to play before enemy acts.
            if (enemyTurnDelaySeconds > 0f)
                yield return new WaitForSeconds(enemyTurnDelaySeconds);

            // Resolve enemy action (but apply AFTER playing its animation).
            if (TryResolveEnemyAction(out var enemyActionId, out var enemyAction, out var enemyResolution))
            {
                // Play enemy animation and (if HP damage happened) player Hit starting together.
                yield return EnsureVisualExecutor().PlayActionWithTargetHitAndWait(enemyActionId, actorIsPlayer: false, combatState, enemyResolution.State);

                combatState = ClampEnemyResourcesToMax(enemyResolution.State);
                ApplyPostActionEffects(enemyActionId, actorIsPlayer: false);

                // If block armor got fully consumed, remove the status icon immediately.
                if (combatState.PlayerBlockArmor <= 0)
                    RemoveStatus(playerStatuses, StatusEffectId.Block);

                if (combatState.EnemyBlockArmor <= 0)
                    RemoveStatus(enemyStatuses, StatusEffectId.Block);

                PushHudState();

                if (EnsureTurnFlowRunner().TryResolveAfterEnemyAction(context, combatState, enemyActionId, out var enemyActionResolution))
                {
                    FinishBattle(enemyActionResolution.PlayerWon, enemyActionResolution.Reason, enemyActionResolution.WinningActionId);
                    enemyTurnRoutine = null;
                    yield break;
                }
            }

            enemyTurnRoutine = null;

            if (!battleStarted)
                yield break;

            if (EnsureTurnFlowRunner().TryResolveAfterEnemyRound(context, combatState, out var postEnemyTurnResolution))
            {
                FinishBattle(postEnemyTurnResolution.PlayerWon, postEnemyTurnResolution.Reason, postEnemyTurnResolution.WinningActionId);
                yield break;
            }

            // Give time for the enemy's damage popup to play before applying end-of-round effects.
            if (endOfRoundDelaySeconds > 0f)
                yield return new WaitForSeconds(endOfRoundDelaySeconds);

            ApplyEndOfRoundEffects();
            PushHudState();

            if (EnsureTurnFlowRunner().TryResolveAfterEnemyRound(context, combatState, out var postRoundResolution))
            {
                FinishBattle(postRoundResolution.PlayerWon, postRoundResolution.Reason, postRoundResolution.WinningActionId);
                yield break;
            }

            BeginPlayerTurn();
        }

        private bool TryResolveEnemyAction(
            out CombatActionId actionId,
            out CombatActionData action,
            out CombatResolution resolution)
        {
            if (!battleStarted)
            {
                actionId = default;
                action = null;
                resolution = null;
                return false;
            }

            if (!EnsureActionResolutionSystem().TryResolveEnemyAction(
                context,
                combatEngine,
                actionRegistry,
                combatState,
                rng,
                out actionId,
                out action,
                out resolution,
                out var failure))
            {
                if (failure == BattleActionResolveFailure.BattleSystemsNotInitialized)
                {
                    Logger.LogError("BattleController: combatEngine/actionRegistry not initialized");
                }
                else if (failure == BattleActionResolveFailure.EnemyNoActionPicked)
                {
                    Logger.LogInfo("[BattleController] Enemy skips turn (no affordable/allowed actions)");
                }
                else if (failure == BattleActionResolveFailure.ActionNotFound)
                {
                    Logger.LogError($"[BattleController] Enemy action not found in registry: {actionId}");
                }
                else if (failure == BattleActionResolveFailure.ActionRejected)
                {
                    var rejectedResult = resolution != null ? resolution.Result.ToString() : "Unknown";
                    var actionIdText = action != null ? action.Id.ToString() : actionId.ToString();
                    Logger.LogInfo($"[BattleController] Enemy action rejected: {actionIdText} -> {rejectedResult}");
                }
                return false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Battle] Enemy used: {actionId}");
#endif

            return true;
        }

        // Enemy actions are handled inside EnemyTurnRoutine() to ensure visuals play before state updates.
    }
}
