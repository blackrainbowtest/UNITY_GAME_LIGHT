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
using Game.Battle.Statuses;
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

        private Coroutine playerActionRoutine;

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
        [Tooltip("Optional. If assigned, all battle modals instantiated at runtime will be parented here (e.g. Canvas/Modals).")]
        [SerializeField] private Transform modalsRoot;
        [Tooltip("Battle results modal reference. Can be either a scene instance (Hierarchy) OR a prefab asset (Project). If a prefab is assigned, BattleController will instantiate it into the scene at runtime.")]
        [SerializeField] private BattleResultModalController resultModal;
        [Tooltip("Outcome animation modal reference. Can be either a scene instance (Hierarchy) OR a prefab asset (Project). If a prefab is assigned, BattleController will instantiate it into the scene at runtime.")]
        [SerializeField] private BattleOutcomeAnimationModalController outcomeAnimationModal;

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
            ResetPerTurnNonCombatActions();
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
            if (!battleStarted)
                return;

            if (turnPhase != TurnPhase.PlayerTurn)
                return;

            OpenInventoryForBattleItemUse();
        }

        public void OnRunPressed()
        {
            if (!battleStarted)
                return;

            if (turnPhase != TurnPhase.PlayerTurn)
                return;

            if (escapeFailedRoutine != null || escapeSuccessRoutine != null)
                return;

            float chance = CalculateEscapeChance01();
            float roll = (float)rng.NextDouble();

            Logger.LogInfo($"[BattleController] Run pressed. EscapeChance={chance:0.000}, Roll={roll:0.000}");

            if (roll <= chance)
            {
                turnPhase = TurnPhase.EnemyTurn;
                hudController?.SetInputEnabled(false);
                escapeSuccessRoutine = StartCoroutine(EscapeSuccessSequenceRoutine());
            }
            else
            {
                // Escape failed: play fail sequence and continue battle.
                turnPhase = TurnPhase.EnemyTurn;
                hudController?.SetInputEnabled(false);
                escapeFailedRoutine = StartCoroutine(EscapeFailedSequenceRoutine());
            }
        }

        private IEnumerator EscapeSuccessSequenceRoutine()
        {
            Logger.LogInfo($"[BattleController] Escape success sequence: anim={escapeSuccessAnim}");

            int moveStartFrame = -1;
            float moveSpeed = 0f;
            TryGetEscapeRunMotion(out moveStartFrame, out moveSpeed);

            Coroutine moveRoutine = null;
            System.Action onImpact = null;
            if (moveStartFrame > 0 && moveSpeed > 0f && playerView != null)
            {
                onImpact = () =>
                {
                    if (moveRoutine != null)
                        StopCoroutine(moveRoutine);

                    moveRoutine = StartCoroutine(MoveTransformLeftUntilStopped(playerView.transform, moveSpeed));
                };
            }

            yield return PlayCharacterAnimImmediateAndWait(playerView, escapeSuccessAnim, onImpact: onImpact, impactFrameIndexOverride: moveStartFrame);

            if (moveRoutine != null)
                StopCoroutine(moveRoutine);

            escapeSuccessRoutine = null;

            // Escape success: no rewards, outcome handled by escape flow.
            FinishBattle(playerWon: false, reason: BattleFinishReason.EscapeSuccess, winningActionId: null);
        }

        private IEnumerator EscapeFailedSequenceRoutine()
        {
            playerView?.SetAutoIdleFallbackEnabled(false);

            var failAnimToPlay = escapeFailFallAnim == BattleVisualAnimId.ActionAct1
                ? BattleVisualAnimId.ActionActFail
                : escapeFailFallAnim;

            if (escapeFailFallAnim == BattleVisualAnimId.ActionAct1)
                Logger.LogWarning("[BattleController] Escape fail anim is legacy ActionAct1 in serialized data. Overriding to ActionActFail at runtime.");

            Logger.LogInfo($"[BattleController] Escape failed sequence: configured={escapeFailFallAnim}, playing={failAnimToPlay}");

            yield return PlayCharacterAnimAndWait(playerView, failAnimToPlay);

            bool modalClosed = false;
            if (outcomeAnimationModal != null)
            {
                outcomeAnimationModal.Show(BattleFinishReason.EscapeFailed, playerWon: false, winningActionId: null, onClosed: () => modalClosed = true);
                while (!modalClosed)
                    yield return null;
            }

            int maxLp = context?.Player != null ? Mathf.Max(0, context.Player.MaxLP) : 0;
            int nextLp = combatState.PlayerLp + Mathf.Max(0, escapeFailLpPenalty);
            if (maxLp > 0)
                nextLp = Mathf.Min(nextLp, maxLp);

            combatState = combatState.WithPlayerLp(nextLp);
            PushHudState();

            escapeFailedRoutine = null;

            playerView?.SetAutoIdleFallbackEnabled(true);

            if (maxLp > 0 && combatState.PlayerLp >= maxLp)
            {
                FinishBattle(playerWon: false, reason: BattleFinishReason.DefeatByLp, winningActionId: null);
                yield break;
            }

            BeginEnemyTurn();
        }

        private IEnumerator PlayCharacterAnimAndWait(BattleCharacterView view, BattleVisualAnimId animId)
        {
            if (view == null)
                yield break;

            bool finished = false;
            // Non-combat utility actions (inventory/open/escape-fail flow) should react instantly
            // and must not wait for idle loop boundary.
            view.PlayImmediate(animId, onFinished: () => finished = true);

            float timeout = 5f;
            while (!finished && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator PlayCharacterAnimImmediateAndWait(
            BattleCharacterView view,
            BattleVisualAnimId animId,
            System.Action onImpact = null,
            int impactFrameIndexOverride = -1)
        {
            if (view == null)
                yield break;

            bool finished = false;
            view.PlayImmediate(animId, onFinished: () => finished = true, onImpact: onImpact, impactFrameIndexOverride: impactFrameIndexOverride);

            float timeout = 5f;
            while (!finished && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        private bool TryGetEscapeRunMotion(out int startAtFrame, out float speedLeftUnitsPerSecond)
        {
            startAtFrame = -1;
            speedLeftUnitsPerSecond = 0f;

            var outfit = playerView != null ? playerView.ResolveOutfitVisuals() : null;
            if (outfit == null)
                return false;

            if (!outfit.TryGetEscapeRunMotion(out var cfg))
                return false;

            startAtFrame = cfg.startAtFrame;
            speedLeftUnitsPerSecond = cfg.speedLeftUnitsPerSecond;
            return true;
        }

        private static IEnumerator MoveTransformLeftUntilStopped(Transform target, float speedLeftUnitsPerSecond)
        {
            if (target == null || speedLeftUnitsPerSecond <= 0f)
                yield break;

            while (target != null)
            {
                target.position += Vector3.left * speedLeftUnitsPerSecond * Time.deltaTime;
                yield return null;
            }
        }

        public void OnSurrenderPressed()
        {
            if (!battleStarted)
                return;

            if (turnPhase != TurnPhase.PlayerTurn)
                return;

            if (surrenderRoutine != null || escapeSuccessRoutine != null || escapeFailedRoutine != null)
                return;

            Logger.LogInfo("BattleController: Surrender pressed");

            turnPhase = TurnPhase.EnemyTurn;
            hudController?.SetInputEnabled(false);
            surrenderRoutine = StartCoroutine(SurrenderSequenceRoutine());
        }

        private IEnumerator SurrenderSequenceRoutine()
        {
            yield return PlayCharacterAnimImmediateAndWait(playerView, BattleVisualAnimId.ActionAct3);

            surrenderRoutine = null;
            FinishBattle(playerWon: false, reason: BattleFinishReason.Surrender, winningActionId: null);
        }

        private bool TryFinishByLpThreshold(bool actionByPlayer, CombatActionId? sourceActionId)
        {
            if (!battleStarted || context == null)
                return false;

            int playerMaxLp = context.Player != null ? Mathf.Max(0, context.Player.MaxLP) : 0;
            int enemyMaxLp = context.Enemy != null ? Mathf.Max(0, context.Enemy.maxLp) : 0;

            bool playerReachedMaxLp = playerMaxLp > 0 && combatState.PlayerLp >= playerMaxLp;
            bool enemyReachedMaxLp = enemyMaxLp > 0 && combatState.EnemyLp >= enemyMaxLp;

            if (!playerReachedMaxLp && !enemyReachedMaxLp)
                return false;

            bool playerWon;
            if (playerReachedMaxLp && enemyReachedMaxLp)
            {
                // Rule requested: if the player filled enemy LP to max, enemy loses; otherwise player loses.
                playerWon = actionByPlayer;
            }
            else
            {
                playerWon = enemyReachedMaxLp;
            }

            var reason = playerWon ? BattleFinishReason.VictoryByLp : BattleFinishReason.DefeatByLp;
            FinishBattle(playerWon, reason, sourceActionId);
            return true;
        }

        private float CalculateEscapeChance01()
        {
            if (context == null || combatState == null)
                return minEscapeChance;

            static float Safe01(int value, int max)
            {
                if (max <= 0)
                    return 0f;
                return Mathf.Clamp01((float)value / max);
            }

            float playerStamina01 = Safe01(combatState.PlayerSp, context.Player != null ? context.Player.MaxSP : 0);
            float enemyStamina01 = Safe01(combatState.EnemySp, context.Enemy != null ? context.Enemy.maxSp : 0);

            float playerLust01 = Safe01(combatState.PlayerLp, context.Player != null ? context.Player.MaxLP : 0);
            float enemyLust01 = Safe01(combatState.EnemyLp, context.Enemy != null ? context.Enemy.maxLp : 0);

            // Requirement:
            // - Player: less lust + more stamina => higher chance
            // - Enemy: less stamina + less lust => higher chance
            float staminaScore01 = Mathf.Clamp01(0.5f + 0.5f * (playerStamina01 - enemyStamina01));
            float lustScore01 = Mathf.Clamp01(1f - 0.5f * (playerLust01 + enemyLust01));

            float wSum = Mathf.Max(0.0001f, escapeStaminaWeight + escapeLustWeight);
            float combined01 = (escapeStaminaWeight * staminaScore01 + escapeLustWeight * lustScore01) / wSum;

            float lo = Mathf.Clamp01(minEscapeChance);
            float hi = Mathf.Clamp01(Mathf.Max(minEscapeChance, maxEscapeChance));
            return Mathf.Clamp01(Mathf.Lerp(lo, hi, combined01));
        }

        public void OnSkipTurnPressed()
        {
            if (!battleStarted)
                return;

            if (turnPhase != TurnPhase.PlayerTurn)
                return;

            if (playerActionRoutine != null)
                return;

            turnPhase = TurnPhase.EnemyTurn;
            hudController?.SetInputEnabled(false);
            playerActionRoutine = StartCoroutine(SkipTurnSequenceRoutine());
        }

        private IEnumerator SkipTurnSequenceRoutine()
        {
            if (TryGetVisualAnimId(CombatActionId.ActionAct4, out var animId))
                yield return PlayCharacterAnimImmediateAndWait(playerView, animId);

            // CounterAttack window should last only until the end of the next player turn.
            // If the player skips the turn, they lose the opportunity.
            combatState = combatState
                .WithPlayerBlockedLastTurn(false)
                .WithPlayerBlockArmorAbsorbedLastEnemyAction(0);

            PushHudState();

            playerActionRoutine = null;

            BeginEnemyTurn();
        }

        private static bool TryGetVisualAnimId(CombatActionId actionId, out BattleVisualAnimId animId)
        {
            switch (actionId)
            {
                case CombatActionId.FastAttack: animId = BattleVisualAnimId.FastAttack; return true;
                case CombatActionId.NormalAttack: animId = BattleVisualAnimId.NormalAttack; return true;
                case CombatActionId.HeavyAttack: animId = BattleVisualAnimId.HeavyAttack; return true;
                case CombatActionId.CounterAttack: animId = BattleVisualAnimId.CounterAttack; return true;
                case CombatActionId.Block: animId = BattleVisualAnimId.Block; return true;

                case CombatActionId.FireSpell: animId = BattleVisualAnimId.FireSpell; return true;
                case CombatActionId.IceSpell: animId = BattleVisualAnimId.IceSpell; return true;
                case CombatActionId.HolySpell: animId = BattleVisualAnimId.HolySpell; return true;
                case CombatActionId.DarkSpell: animId = BattleVisualAnimId.DarkSpell; return true;

                case CombatActionId.SeductionAct1: animId = BattleVisualAnimId.SeductionAct1; return true;
                case CombatActionId.SeductionAct2: animId = BattleVisualAnimId.SeductionAct2; return true;
                case CombatActionId.SeductionAct3: animId = BattleVisualAnimId.SeductionAct3; return true;
                case CombatActionId.SeductionAct4: animId = BattleVisualAnimId.SeductionAct4; return true;

                case CombatActionId.ActionAct1: animId = BattleVisualAnimId.ActionAct1; return true;
                case CombatActionId.ActionAct2: animId = BattleVisualAnimId.ActionAct2; return true;
                case CombatActionId.ActionAct3: animId = BattleVisualAnimId.ActionAct3; return true;
                case CombatActionId.ActionAct4: animId = BattleVisualAnimId.ActionAct4; return true;
            }

            animId = BattleVisualAnimId.Idle;
            return false;
        }

        private IEnumerator PlayActionVisualAndWait(CombatActionId actionId, bool actorIsPlayer)
        {
            var view = actorIsPlayer ? playerView : enemyView;
            if (view == null)
                yield break;

            if (!TryGetVisualAnimId(actionId, out var animId))
                yield break;

            bool finished = false;
            view.RequestPlayAfterCurrent(animId, onFinished: () => finished = true);

            // Safety timeout to avoid soft-lock if something is miswired.
            float timeout = 5f;
            while (!finished && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator PlayActionWithTargetHitAndWait(
            CombatActionId actionId,
            bool actorIsPlayer,
            CombatState before,
            CombatState after)
        {
            var attackerView = actorIsPlayer ? playerView : enemyView;
            var targetView = actorIsPlayer ? enemyView : playerView;

            if (attackerView == null)
                yield break;

            if (!TryGetVisualAnimId(actionId, out var attackerAnimId))
                yield break;

            bool attackerFinished = false;
            bool targetFinished = true; // updated after we decide willPlayTargetHit

            // Optional: spell projectile config (comes from attacker's outfit visuals)
            OutfitVisuals.SpellProjectileConfig projectileConfig = default;
            bool hasProjectile = false;
            var attackerOutfit = attackerView != null ? attackerView.ResolveOutfitVisuals() : null;
            if (attackerOutfit != null)
                hasProjectile = attackerOutfit.TryGetProjectileConfig(attackerAnimId, out projectileConfig);

            // Optional: hit timing config (comes from attacker's outfit visuals)
            int hitAtFrame = 1;
            bool useLustHit = false;
            if (attackerOutfit != null && attackerOutfit.TryGetHitTiming(attackerAnimId, out var hitTiming))
            {
                hitAtFrame = hitTiming.hitAtFrame;
                useLustHit = hitTiming.useLustHit;
            }

            bool projectileSpawned = false;
            bool targetHitTriggered = false;
            BattleVisualAnimId targetHitAnimId = BattleVisualAnimId.Hit;
            bool willPlayTargetHit = false;

            void SpawnProjectileNow()
            {
                if (!hasProjectile)
                    return;
                if (projectileSpawned)
                    return;
                if (!projectileConfig.IsEnabled)
                    return;
                if (attackerView == null)
                    return;

                projectileSpawned = true;

                int dir = actorIsPlayer ? 1 : -1;
                var spawnOffsetUnits = new Vector3(
                    projectileConfig.ToUnits(projectileConfig.spawnOffsetPixels.x) * dir,
                    projectileConfig.ToUnits(projectileConfig.spawnOffsetPixels.y),
                    0f);

                var start = attackerView.transform.position + spawnOffsetUnits;
                var end = start + Vector3.right * (projectileConfig.ToUnits(projectileConfig.travelDistancePixels) * dir);

                var parent = projectilesRoot != null ? projectilesRoot : null;
                var go = Instantiate(projectileConfig.projectilePrefab, start, Quaternion.identity, parent);
                var proj = go.GetComponent<Game.Battle.Visual.BattleSpellProjectile>();
                if (proj == null)
                    proj = go.AddComponent<Game.Battle.Visual.BattleSpellProjectile>();

                bool flipX = dir < 0;

                // Impact moment for magic should be driven by the projectile animation.
                // If impactAtFrame is not set (<= 0), projectile will trigger impact at the end of its animation.
                proj.Initialize(
                    start,
                    end,
                    projectileConfig.travelTimeSeconds,
                    projectileConfig.projectileAnimation,
                    flipX,
                    impactAtFrame: projectileConfig.impactAtFrame,
                    onImpact: () => TriggerTargetHitNow());
            }

            void HandleOneShotStarted(BattleVisualAnimId id, IdleAnimation anim)
            {
                if (id != attackerAnimId)
                    return;

                // If the attacker animation has no impact marker configured,
                // spawn projectile immediately when the animation starts.
                if (hasProjectile && !projectileSpawned)
                {
                    bool hasImpactMarker = anim != null && anim.HasImpact;
                    if (!hasImpactMarker)
                        SpawnProjectileNow();
                }
            }

            attackerView.OnOneShotStarted += HandleOneShotStarted;

            bool targetTookHpDamage = false;
            if (before != null && after != null)
            {
                targetTookHpDamage = actorIsPlayer
                    ? after.EnemyHp < before.EnemyHp
                    : after.PlayerHp < before.PlayerHp;
            }

            bool targetTookLpDamage = false;
            if (before != null && after != null)
            {
                // Lust "damage" is represented as LP INCREASE on the target.
                targetTookLpDamage = actorIsPlayer
                    ? after.EnemyLp > before.EnemyLp
                    : after.PlayerLp > before.PlayerLp;
            }

            // Decide which target hit anim to play (physical vs emotional) and whether it should play at all.
            if (useLustHit)
            {
                targetHitAnimId = BattleVisualAnimId.LustHit;
                willPlayTargetHit = targetTookLpDamage;
            }
            else
            {
                targetHitAnimId = BattleVisualAnimId.Hit;
                willPlayTargetHit = targetTookHpDamage;
            }

            // hitAtFrame == -1 means: ignore hit animation even if damage happened.
            if (hitAtFrame == -1)
                willPlayTargetHit = false;

            // If we expect a hit, keep coroutine waiting until it actually happens and finishes.
            targetFinished = !willPlayTargetHit;

            void TriggerTargetHitNow()
            {
                if (!willPlayTargetHit)
                    return;
                if (targetHitTriggered)
                    return;
                if (targetView == null)
                    return;

                targetHitTriggered = true;
                targetFinished = false;

                // Immediate hit to avoid waiting for idle loop boundaries.
                targetView.PlayImmediate(targetHitAnimId, onFinished: () => targetFinished = true);
            }

            // Start attacker animation immediately (do not wait for idle loop/ambient to finish).
            // - For magic projectile attacks: onImpact is used to spawn the projectile at a specific caster frame.
            // - For non-projectile attacks: onImpact triggers the target hit.
            if (hasProjectile)
            {
                int spawnFrame = projectileConfig.spawnAtFrame;
                if (spawnFrame <= 1)
                    spawnFrame = 1;

                attackerView.PlayImmediate(
                    attackerAnimId,
                    onFinished: () => attackerFinished = true,
                    onImpact: () => SpawnProjectileNow(),
                    impactFrameIndexOverride: spawnFrame);
            }
            else
            {
                attackerView.PlayImmediate(
                    attackerAnimId,
                    onFinished: () => attackerFinished = true,
                    onImpact: () => TriggerTargetHitNow(),
                    impactFrameIndexOverride: -1);
            }

            // Safety timeout to avoid soft-lock if something is miswired.
            float timeout = 5f;
            while ((!attackerFinished || !targetFinished) && timeout > 0f)
            {
                // Fallbacks for misconfiguration.
                if (attackerFinished)
                {
                    if (hasProjectile && !projectileSpawned)
                        SpawnProjectileNow();

                    // If this is not a projectile action, and impact never fired, trigger hit at the end.
                    if (!hasProjectile && !targetHitTriggered)
                        TriggerTargetHitNow();
                }

                timeout -= Time.deltaTime;
                yield return null;
            }

            attackerView.OnOneShotStarted -= HandleOneShotStarted;
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

            if (playerActionRoutine != null)
                return;

            // Lock input immediately to prevent spamming while enemy is about to act.
            turnPhase = TurnPhase.EnemyTurn;
            hudController?.SetInputEnabled(false);

            playerActionRoutine = StartCoroutine(ExecutePlayerActionRoutine(actionId));
        }

        private IEnumerator ExecutePlayerActionRoutine(CombatActionId actionId)
        {
            if (actionRegistry == null)
            {
                Logger.LogError("BattleController: actionRegistry is not initialized");

                turnPhase = TurnPhase.PlayerTurn;
                hudController?.SetInputEnabled(true);
                playerActionRoutine = null;
                yield break;
            }

            var action = actionRegistry.Get(actionId);
            if (action == null)
            {
                Logger.LogError($"Action not found: {actionId}");

                turnPhase = TurnPhase.PlayerTurn;
                hudController?.SetInputEnabled(true);
                playerActionRoutine = null;
                yield break;
            }

            var resolution = combatEngine.ResolvePlayerAction(combatState, action);

            if (resolution.Result != CombatActionResult.Executed)
            {
                Logger.LogInfo($"Action rejected: {resolution.Result}");

                turnPhase = TurnPhase.PlayerTurn;
                hudController?.SetInputEnabled(true);
                playerActionRoutine = null;
                yield break;
            }

            // 1) Play attacker animation and (if HP damage happened) target Hit starting together.
            yield return PlayActionWithTargetHitAndWait(actionId, actorIsPlayer: true, combatState, resolution.State);

            // 2) Apply results after animation.
            combatState = ClampPlayerResourcesToMax(resolution.State);
            ApplyPostActionEffects(actionId, actorIsPlayer: true);

            if (combatState.EnemyBlockArmor <= 0)
                RemoveStatus(enemyStatuses, StatusEffectId.Block);

            PushHudState();

            playerActionRoutine = null;

            if (TryFinishByLpThreshold(actionByPlayer: true, sourceActionId: actionId))
                yield break;

            if (combatState.IsEnemyDead)
            {
                FinishBattle(playerWon: true, reason: BattleFinishReason.Victory, winningActionId: actionId);
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

            turnPhase = TurnPhase.EnemyTurn;
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
                yield return PlayActionWithTargetHitAndWait(enemyActionId, actorIsPlayer: false, combatState, enemyResolution.State);

                combatState = ClampEnemyResourcesToMax(enemyResolution.State);
                ApplyPostActionEffects(enemyActionId, actorIsPlayer: false);

                // If block armor got fully consumed, remove the status icon immediately.
                if (combatState.PlayerBlockArmor <= 0)
                    RemoveStatus(playerStatuses, StatusEffectId.Block);

                if (combatState.EnemyBlockArmor <= 0)
                    RemoveStatus(enemyStatuses, StatusEffectId.Block);

                PushHudState();

                if (TryFinishByLpThreshold(actionByPlayer: false, sourceActionId: enemyActionId))
                {
                    enemyTurnRoutine = null;
                    yield break;
                }

                if (combatState.IsPlayerDead)
                {
                    FinishBattle(playerWon: false, reason: BattleFinishReason.Defeat, winningActionId: enemyActionId);
                    enemyTurnRoutine = null;
                    yield break;
                }
            }

            enemyTurnRoutine = null;

            if (!battleStarted)
                yield break;

            if (combatState.IsPlayerDead)
            {
                FinishBattle(playerWon: false, reason: BattleFinishReason.Defeat, winningActionId: null);
                yield break;
            }

            // Give time for the enemy's damage popup to play before applying end-of-round effects.
            if (endOfRoundDelaySeconds > 0f)
                yield return new WaitForSeconds(endOfRoundDelaySeconds);

            ApplyEndOfRoundEffects();
            PushHudState();

            if (TryFinishByLpThreshold(actionByPlayer: false, sourceActionId: null))
                yield break;

            BeginPlayerTurn();
        }

        private bool TryResolveEnemyAction(
            out CombatActionId actionId,
            out CombatActionData action,
            out CombatResolution resolution)
        {
            actionId = default;
            action = null;
            resolution = null;

            if (!battleStarted)
                return false;

            if (combatEngine == null || actionRegistry == null)
            {
                Logger.LogError("BattleController: combatEngine/actionRegistry not initialized");
                return false;
            }

            if (combatState.IsEnemyDead || combatState.IsPlayerDead)
                return false;

            var picked = EnemyActionSelector.SelectEnemyAction(
                context.EnemyDifficulty,
                context.Enemy,
                actionRegistry,
                combatState,
                rng);

            if (!picked.HasValue)
            {
                Logger.LogInfo("[BattleController] Enemy skips turn (no affordable/allowed actions)");
                return false;
            }

            actionId = picked.Value;
            action = actionRegistry.Get(actionId);
            if (action == null)
            {
                Logger.LogError($"[BattleController] Enemy action not found in registry: {actionId}");
                return false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Battle] Enemy used: {actionId}");
#endif

            resolution = combatEngine.ResolveEnemyAction(combatState, action);
            if (resolution.Result != CombatActionResult.Executed)
            {
                Logger.LogInfo($"[BattleController] Enemy action rejected: {action.Id} -> {resolution.Result}");
                return false;
            }

            return true;
        }

        // Enemy actions are handled inside EnemyTurnRoutine() to ensure visuals play before state updates.
    }
}
