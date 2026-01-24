using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Battle.UI;
using Game.Battle.Combat.Actions;
using Game.Battle.Statuses;

public class BattleHUDController : MonoBehaviour, IBattleHUDView
{
    [Header("Action Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button defenseButton;
    [SerializeField] private Button magicButton;
    [SerializeField] private Button actionsButton;
    [SerializeField] private Button seductionButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button exitButton;

    [Header("Optional Buttons")]
    [Tooltip("Any back buttons across submenus. If set, this controller will wire them to OnBackPressed().")]
    [SerializeField] private Button[] backButtons;

    [Header("Menus")]
    [SerializeField] private GameObject rootMenu;
    [SerializeField] private GameObject attackMenu;
    [SerializeField] private GameObject actionsMenu;
    [SerializeField] private GameObject magicMenu;
    [SerializeField] private GameObject seductionMenu;

    [Header("HP Bars")]
    [SerializeField] private StatBarView playerHpBar;
    [SerializeField] private StatBarView playerMpBar;
    [SerializeField] private StatBarView playerSpBar;
    [SerializeField] private StatBarView playerLpBar;
    [SerializeField] private StatBarView enemyHpBar;
    [SerializeField] private StatBarView enemyMpBar;
    [SerializeField] private StatBarView enemySpBar;
    [SerializeField] private StatBarView enemyLpBar;

    [Header("Statuses (Optional)")]
    [SerializeField] private BattleStatusPanelView statusPanel;

    private IBattleUIActions actions;
    private CanvasGroup canvasGroup;

    private BattleHUDState lastState;
	private bool isWired;

    private void Awake()
    {
        // CanvasGroup lets us disable all HUD interaction at once (including submenu buttons)
        // without having to serialize every single Button reference.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetActions(IBattleUIActions actions)
    {
        this.actions = actions;

        if (!isWired)
        {
            WireButtons();
            isWired = true;
        }

        ShowRootMenu();

        // Default: enabled.
        SetInputEnabled(true);
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    private void WireButtons()
    {
        // Root menu buttons should switch UI, not execute actions directly.
        // Actual combat actions are sent by submenu buttons.
        if (attackButton != null) attackButton.onClick.AddListener(OnAttackMenuPressed);
        if (defenseButton != null) defenseButton.onClick.AddListener(OnDefensePressed);
        if (magicButton != null) magicButton.onClick.AddListener(OnMagicMenuPressed);
        if (actionsButton != null) actionsButton.onClick.AddListener(OnActionsMenuPressed);
        if (seductionButton != null) seductionButton.onClick.AddListener(OnSeductionMenuPressed);
        if (itemButton != null) itemButton.onClick.AddListener(HandleItemPressed);
        if (exitButton != null) exitButton.onClick.AddListener(HandleExitPressed);

        if (backButtons != null)
        {
            foreach (var btn in backButtons)
            {
                if (btn == null) continue;
                btn.onClick.AddListener(OnBackPressed);
            }
        }
    }

    private void UnwireButtons()
    {
        if (!isWired)
            return;

        if (attackButton != null) attackButton.onClick.RemoveListener(OnAttackMenuPressed);
        if (defenseButton != null) defenseButton.onClick.RemoveListener(OnDefensePressed);
        if (magicButton != null) magicButton.onClick.RemoveListener(OnMagicMenuPressed);
        if (actionsButton != null) actionsButton.onClick.RemoveListener(OnActionsMenuPressed);
        if (seductionButton != null) seductionButton.onClick.RemoveListener(OnSeductionMenuPressed);
        if (itemButton != null) itemButton.onClick.RemoveListener(HandleItemPressed);
        if (exitButton != null) exitButton.onClick.RemoveListener(HandleExitPressed);

        if (backButtons != null)
        {
            foreach (var btn in backButtons)
            {
                if (btn == null) continue;
                btn.onClick.RemoveListener(OnBackPressed);
            }
        }

        isWired = false;
    }

    private void HandleItemPressed()
    {
        if (actions == null)
        {
            Debug.LogError("BattleHUDController: actions is not set");
            return;
        }
        actions.OnItemPressed();
    }

    private void HandleExitPressed()
    {
        if (actions == null)
        {
            Debug.LogError("BattleHUDController: actions is not set");
            return;
        }
        actions.OnExitPressed();
    }

    public void SetInputEnabled(bool enabled)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;

        // Optional: slight visual feedback.
        canvasGroup.alpha = enabled ? 1f : 0.85f;
    }

    public void ShowRootMenu()
    {
        if (rootMenu != null) rootMenu.SetActive(true);
        if (attackMenu != null) attackMenu.SetActive(false);
        if (actionsMenu != null) actionsMenu.SetActive(false);
        if (magicMenu != null) magicMenu.SetActive(false);
        if (seductionMenu != null) seductionMenu.SetActive(false);
    }

    public void ShowAttackMenu()
    {
        if (rootMenu != null) rootMenu.SetActive(false);
        if (attackMenu != null) attackMenu.SetActive(true);
        if (actionsMenu != null) actionsMenu.SetActive(false);
        if (magicMenu != null) magicMenu.SetActive(false);
        if (seductionMenu != null) seductionMenu.SetActive(false);
    }

    public void ShowActionsMenu()
    {
        if (rootMenu != null) rootMenu.SetActive(false);
        if (attackMenu != null) attackMenu.SetActive(false);
        if (actionsMenu != null) actionsMenu.SetActive(true);
        if (magicMenu != null) magicMenu.SetActive(false);
        if (seductionMenu != null) seductionMenu.SetActive(false);
    }

    public void ShowMagicMenu()
    {
        if (rootMenu != null) rootMenu.SetActive(false);
        if (attackMenu != null) attackMenu.SetActive(false);
        if (actionsMenu != null) actionsMenu.SetActive(false);
        if (magicMenu != null) magicMenu.SetActive(true);
        if (seductionMenu != null) seductionMenu.SetActive(false);
    }

    public void ShowSeductionMenu()
    {
        if (rootMenu != null) rootMenu.SetActive(false);
        if (attackMenu != null) attackMenu.SetActive(false);
        if (actionsMenu != null) actionsMenu.SetActive(false);
        if (magicMenu != null) magicMenu.SetActive(false);
        if (seductionMenu != null) seductionMenu.SetActive(true);
    }

    // ===== Root menu button handlers =====

    public void OnAttackMenuPressed() => ShowAttackMenu();
    public void OnDefensePressed() => SelectAction(CombatActionId.Block);
    public void OnActionsMenuPressed() => ShowActionsMenu();
    public void OnMagicMenuPressed() => ShowMagicMenu();
    public void OnSeductionMenuPressed() => ShowSeductionMenu();
    public void OnBackPressed() => ShowRootMenu();

    // ===== Submenu action button handlers =====

    public void OnFastAttackPressed() => SelectAction(CombatActionId.FastAttack);
    public void OnNormalAttackPressed() => SelectAction(CombatActionId.NormalAttack);
    public void OnHeavyAttackPressed() => SelectAction(CombatActionId.HeavyAttack);
    public void OnCounterAttackPressed() => SelectAction(CombatActionId.CounterAttack);

    public void OnFireSpellPressed() => SelectAction(CombatActionId.FireSpell);
    public void OnIceSpellPressed() => SelectAction(CombatActionId.IceSpell);
    public void OnHolySpellPressed() => SelectAction(CombatActionId.HolySpell);
    public void OnDarkSpellPressed() => SelectAction(CombatActionId.DarkSpell);

    public void OnSeductionAct1Pressed() => SelectAction(CombatActionId.SeductionAct1);
    public void OnSeductionAct2Pressed() => SelectAction(CombatActionId.SeductionAct2);
    public void OnSeductionAct3Pressed() => SelectAction(CombatActionId.SeductionAct3);
    public void OnSeductionAct4Pressed() => SelectAction(CombatActionId.SeductionAct4);

    // ===== Actions menu (non-combat intents) =====

    public void OnInventoryPressed()
    {
        if (actions == null)
        {
            Debug.LogError("BattleHUDController: actions is not set");
            return;
        }

        actions.OnItemPressed();
        ShowRootMenu();
    }

    public void OnRunPressed()
    {
        if (actions == null)
        {
            Debug.LogError("BattleHUDController: actions is not set");
            return;
        }

        actions.OnRunPressed();
        ShowRootMenu();
    }

    public void OnSurrenderPressed()
    {
        if (actions == null)
        {
            Debug.LogError("BattleHUDController: actions is not set");
            return;
        }

        actions.OnSurrenderPressed();
        ShowRootMenu();
    }

    public void OnSkipTurnPressed()
    {
        if (actions == null)
        {
            Debug.LogError("BattleHUDController: actions is not set");
            return;
        }

        actions.OnSkipTurnPressed();
        ShowRootMenu();
    }

    private void SelectAction(CombatActionId actionId)
    {
        if (actions == null)
        {
            Debug.LogError("BattleHUDController: actions is not set");
            return;
        }

        actions.OnCombatActionSelected(actionId);
        ShowRootMenu();
    }

    public void UpdateState(BattleHUDState state)
    {
        UpdateState(state, showDeltas: true);
    }

    public void UpdateStatuses(IReadOnlyList<StatusInstance> player, IReadOnlyList<StatusInstance> enemy)
    {
        if (statusPanel == null)
            return;

        statusPanel.Render(player, enemy);
    }

    public void UpdateState(BattleHUDState state, bool showDeltas)
    {
        if (state == null) return;

        var hasLast = lastState != null;
        var canShowDeltas = showDeltas && hasLast;

        UpdateBar(playerHpBar, state.PlayerHp, state.PlayerHpMax, hasLast ? state.PlayerHp - lastState.PlayerHp : 0, canShowDeltas);
        UpdateBar(playerMpBar, state.PlayerMp, state.PlayerMpMax, hasLast ? state.PlayerMp - lastState.PlayerMp : 0, canShowDeltas);
        UpdateBar(playerSpBar, state.PlayerSp, state.PlayerSpMax, hasLast ? state.PlayerSp - lastState.PlayerSp : 0, canShowDeltas);
        UpdateBar(playerLpBar, state.PlayerLp, state.PlayerLpMax, hasLast ? state.PlayerLp - lastState.PlayerLp : 0, canShowDeltas);

        UpdateBar(enemyHpBar, state.EnemyHp, state.EnemyHpMax, hasLast ? state.EnemyHp - lastState.EnemyHp : 0, canShowDeltas);
        UpdateBar(enemyMpBar, state.EnemyMp, state.EnemyMpMax, hasLast ? state.EnemyMp - lastState.EnemyMp : 0, canShowDeltas);
        UpdateBar(enemySpBar, state.EnemySp, state.EnemySpMax, hasLast ? state.EnemySp - lastState.EnemySp : 0, canShowDeltas);
        UpdateBar(enemyLpBar, state.EnemyLp, state.EnemyLpMax, hasLast ? state.EnemyLp - lastState.EnemyLp : 0, canShowDeltas);

        // Store a copy so later deltas work even if caller reuses the same object.
        lastState = Clone(state);
    }

    private static void UpdateBar(StatBarView bar, int current, int max, int delta, bool showDelta)
    {
        if (bar == null)
            return;

        // Important: if max == 0, we must still update the UI.
        // Otherwise the bar keeps previous fill/value and looks like a random/default number.
        if (max <= 0)
        {
            bar.SetNormalized(0f);
            bar.SetValue(0, 0);
            return;
        }

        bar.SetNormalized((float)current / max);
        bar.SetValue(current, max);

        if (showDelta)
            bar.ShowDelta(delta);
    }

    private static BattleHUDState Clone(BattleHUDState s)
    {
        return new BattleHUDState
        {
            PlayerHp = s.PlayerHp,
            PlayerHpMax = s.PlayerHpMax,
            PlayerMp = s.PlayerMp,
            PlayerMpMax = s.PlayerMpMax,
            PlayerSp = s.PlayerSp,
            PlayerSpMax = s.PlayerSpMax,
            PlayerLp = s.PlayerLp,
            PlayerLpMax = s.PlayerLpMax,

            EnemyHp = s.EnemyHp,
            EnemyHpMax = s.EnemyHpMax,
            EnemyMp = s.EnemyMp,
            EnemyMpMax = s.EnemyMpMax,
            EnemySp = s.EnemySp,
            EnemySpMax = s.EnemySpMax,
            EnemyLp = s.EnemyLp,
            EnemyLpMax = s.EnemyLpMax
        };
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
