using UnityEngine;
using UnityEngine.UI;
using Game.Battle.UI;
using Game.Battle.Combat.Actions;

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

    private IBattleUIActions actions;

    public void SetActions(IBattleUIActions actions)
    {
        this.actions = actions;

        // Root menu buttons should switch UI, not execute actions directly.
        // Actual combat actions are sent by submenu buttons.
        if (attackButton != null) attackButton.onClick.AddListener(OnAttackMenuPressed);
        if (defenseButton != null) defenseButton.onClick.AddListener(OnDefensePressed);
        if (magicButton != null) magicButton.onClick.AddListener(OnMagicMenuPressed);
        if (actionsButton != null) actionsButton.onClick.AddListener(OnActionsMenuPressed);
        if (seductionButton != null) seductionButton.onClick.AddListener(OnSeductionMenuPressed);
        if (itemButton != null) itemButton.onClick.AddListener(() => actions.OnItemPressed());
        if (exitButton != null) exitButton.onClick.AddListener(() => actions.OnExitPressed());

        if (backButtons != null)
        {
            foreach (var btn in backButtons)
            {
                if (btn == null) continue;
                btn.onClick.AddListener(OnBackPressed);
            }
        }

        ShowRootMenu();
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
        if (state == null) return;

        Debug.Log($"[HUD] Player HP: {state.PlayerHp}/{state.PlayerHpMax}, Enemy HP: {state.EnemyHp}/{state.EnemyHpMax}");

        if (playerHpBar != null && state.PlayerHpMax > 0)
            playerHpBar.SetNormalized((float)state.PlayerHp / state.PlayerHpMax);
        if (playerMpBar != null && state.PlayerMpMax > 0)
            playerMpBar.SetNormalized((float)state.PlayerMp / state.PlayerMpMax);
        if (playerSpBar != null && state.PlayerSpMax > 0)
            playerSpBar.SetNormalized((float)state.PlayerSp / state.PlayerSpMax);
        if (playerLpBar != null && state.PlayerLpMax > 0)
            playerLpBar.SetNormalized((float)state.PlayerLp / state.PlayerLpMax);

        if (enemyHpBar != null && state.EnemyHpMax > 0)
            enemyHpBar.SetNormalized((float)state.EnemyHp / state.EnemyHpMax);
        if (enemyMpBar != null && state.EnemyMpMax > 0)
            enemyMpBar.SetNormalized((float)state.EnemyMp / state.EnemyMpMax);
        if (enemySpBar != null && state.EnemySpMax > 0)
            enemySpBar.SetNormalized((float)state.EnemySp / state.EnemySpMax);
        if (enemyLpBar != null && state.EnemyLpMax > 0)
            enemyLpBar.SetNormalized((float)state.EnemyLp / state.EnemyLpMax);
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
