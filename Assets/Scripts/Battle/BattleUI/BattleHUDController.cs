using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Battle.UI;
using Game.Battle.Combat;
using Game.Battle.Combat.Actions;
using Game.Battle.Statuses;
using System;
using System.Linq;

public enum TooltipButtonBindingMode
{
    ButtonToken = 0,
    CombatAction = 1
}

[Serializable]
public class BattleTooltipButtonBinding
{
    public Button button;
    public TooltipButtonBindingMode mode = TooltipButtonBindingMode.ButtonToken;
    [Tooltip("Used when Mode = ButtonToken (e.g. 'run', 'inventory', 'magic_menu').")]
    public string buttonToken;
    [Tooltip("Used when Mode = CombatAction.")]
    public CombatActionId combatActionId = CombatActionId.NormalAttack;
}

public partial class BattleHUDController : MonoBehaviour, IBattleHUDView
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

    [Header("Input Visuals")]
    [Tooltip("Optional: assign a CanvasGroup to dim ONLY the button area. If null, the HUD root CanvasGroup is used.")]
    [SerializeField] private CanvasGroup inputVisualGroup;
    [Tooltip("Optional: overlay GameObject (e.g. a semi-transparent panel with 'Enemy turn'). Enabled when input is disabled.")]
    [SerializeField] private GameObject inputDisabledOverlay;
    [SerializeField] private bool dimWhenInputDisabled = true;
    [SerializeField, Range(0.05f, 1f)] private float disabledAlpha = 0.35f;

    [Header("Long Press Tooltip")]
    [SerializeField] private BattleActionTooltipModalController actionTooltipModal;
    [SerializeField, Min(0.1f)] private float tooltipHoldDuration = 0.35f;
    [SerializeField] private LongPressProgressView longPressProgressViewPrefab;
    [Tooltip("Optional: where to parent the long-press progress circle (recommended: a top-level Canvas/Overlay root). If null, root Canvas is used.")]
    [SerializeField] private Transform longPressProgressViewParent;
    [SerializeField, Min(0f)] private float tooltipProgressShowDelay = 0.15f;
    [SerializeField] private bool debugLongPress;
    [SerializeField] private string actionNameKeyFormat = "battle_action_{0}_name";
    [SerializeField] private string actionDescriptionKeyFormat = "battle_action_{0}_desc";
    [SerializeField] private string buttonNameKeyFormat = "battle_button_{0}_name";
    [SerializeField] private string buttonDescriptionKeyFormat = "battle_button_{0}_desc";
    [SerializeField] private string damageFormatKey = "battle_tooltip_damage_fmt";
    [SerializeField] private string healFormatKey = "battle_tooltip_heal_fmt";
    [SerializeField] private string mpCostFormatKey = "battle_tooltip_mp_cost_fmt";
    [SerializeField] private string spCostFormatKey = "battle_tooltip_sp_cost_fmt";
    [SerializeField] private string lpCostFormatKey = "battle_tooltip_lp_cost_fmt";
    [Tooltip("Optional explicit mapping: assign a Button and manually choose what tooltip it represents.")]
    [SerializeField] private BattleTooltipButtonBinding[] manualTooltipButtons;

    private IBattleUIActions actions;
    private CanvasGroup canvasGroup;
    private GraphicRaycaster graphicRaycaster;
    private Button[] allButtons;
    private bool inputEnabled;

    private BattleHUDState lastState;
	private bool isWired;
    private CombatActionRegistry tooltipActionRegistry;
    private LongPressProgressView longPressProgressViewInstance;
    private bool warnedMissingTooltipModal;
    private int tooltipPlayerPhysicalDamage = CombatDamageModel.DefaultBaseAttack;
    private int tooltipPlayerMagicDamage = CombatDamageModel.DefaultBaseAttack;

    private void Awake()
    {
        // CanvasGroup lets us disable all HUD interaction at once (including submenu buttons)
        // without having to serialize every single Button reference.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (inputVisualGroup == null)
            inputVisualGroup = canvasGroup;

        graphicRaycaster = GetComponentInParent<GraphicRaycaster>();

        RefreshButtonsCache();

        tooltipActionRegistry = new CombatActionRegistry();
        SetupLongPressTooltips();
    }

    private void Start()
    {
        RefreshButtonsCache();
        SetupLongPressTooltips();
    }

    private void OnEnable()
    {
        RefreshButtonsCache();
        SetupLongPressTooltips();
    }

}
