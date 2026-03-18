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

    private void RefreshButtonsCache()
    {
        var unique = new HashSet<Button>();

        void Add(Button button)
        {
            if (button != null)
                unique.Add(button);
        }

        Add(attackButton);
        Add(defenseButton);
        Add(magicButton);
        Add(actionsButton);
        Add(seductionButton);
        Add(itemButton);
        Add(exitButton);

        if (backButtons != null)
        {
            for (int i = 0; i < backButtons.Length; i++)
                Add(backButtons[i]);
        }

        if (manualTooltipButtons != null)
        {
            for (int i = 0; i < manualTooltipButtons.Length; i++)
                Add(manualTooltipButtons[i] != null ? manualTooltipButtons[i].button : null);
        }

        var childrenButtons = GetComponentsInChildren<Button>(includeInactive: true);
        if (childrenButtons != null)
        {
            for (int i = 0; i < childrenButtons.Length; i++)
                Add(childrenButtons[i]);
        }

        if (unique.Count == 0)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var canvasButtons = canvas.GetComponentsInChildren<Button>(includeInactive: true);
                if (canvasButtons != null)
                {
                    for (int i = 0; i < canvasButtons.Length; i++)
                        Add(canvasButtons[i]);
                }
            }
        }

        allButtons = unique.ToArray();
        LogLongPress($"RefreshButtonsCache: found {allButtons.Length} buttons");
    }

    private void SetupLongPressTooltips()
    {
        EnsureTooltipModal();

        EnsureLongPressProgressView();

        if (allButtons == null || allButtons.Length == 0)
            RefreshButtonsCache();

        if (allButtons == null)
            return;

        int configuredCount = 0;
        int addedCount = 0;

        for (int i = 0; i < allButtons.Length; i++)
        {
            var button = allButtons[i];
            if (button == null)
                continue;

            var trigger = button.GetComponent<BattleButtonLongPressTooltipTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<BattleButtonLongPressTooltipTrigger>();
                addedCount++;
            }

            if (debugLongPress)
                trigger.SetDebugLogging(true, button.name);

            var data = BuildTooltipData(button);
            trigger.Configure(actionTooltipModal, data, tooltipHoldDuration, longPressProgressViewInstance, tooltipProgressShowDelay);
            configuredCount++;
        }

        if (actionTooltipModal == null && !warnedMissingTooltipModal)
        {
            warnedMissingTooltipModal = true;
            Debug.LogWarning("BattleHUDController: Action Tooltip Modal is not assigned and was not found automatically. Long press will detect hold, but tooltip cannot be shown.", this);
        }

        LogLongPress($"SetupLongPressTooltips: buttons={allButtons.Length}, addedTriggers={addedCount}, configured={configuredCount}, progressView={(longPressProgressViewInstance != null ? longPressProgressViewInstance.name : "NULL")}");
    }

    private void EnsureTooltipModal()
    {
        if (actionTooltipModal != null)
            return;

        actionTooltipModal = GetComponentInChildren<BattleActionTooltipModalController>(includeInactive: true);
        if (actionTooltipModal != null)
        {
            warnedMissingTooltipModal = false;
            LogLongPress($"EnsureTooltipModal: found existing '{actionTooltipModal.name}'");
            return;
        }

        var parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            actionTooltipModal = parentCanvas.GetComponentInChildren<BattleActionTooltipModalController>(includeInactive: true);

        if (actionTooltipModal != null)
        {
            warnedMissingTooltipModal = false;
            LogLongPress($"EnsureTooltipModal: found under canvas '{actionTooltipModal.name}'");
        }
        else
        {
            LogLongPress("EnsureTooltipModal: modal not found automatically");
        }
    }

    private void EnsureLongPressProgressView()
    {
        if (longPressProgressViewInstance != null)
            return;

        var parent = ResolveLongPressProgressParent();

        longPressProgressViewInstance = GetComponentInChildren<LongPressProgressView>(includeInactive: true);
        if (longPressProgressViewInstance != null)
        {
            // Ensure it isn't parented under a layout-driven HUD subtree (can pin it to center).
            if (parent != null && longPressProgressViewInstance.transform.parent != parent)
                longPressProgressViewInstance.transform.SetParent(parent, worldPositionStays: false);

            longPressProgressViewInstance.Hide();
            LogLongPress($"EnsureLongPressProgressView: found existing '{longPressProgressViewInstance.name}'");
            return;
        }

        if (longPressProgressViewPrefab == null)
        {
            LogLongPress("EnsureLongPressProgressView: prefab is NULL (circle won't show)");
            return;
        }

        if (parent == null)
            parent = transform;

        longPressProgressViewInstance = Instantiate(longPressProgressViewPrefab, parent);
        longPressProgressViewInstance.gameObject.SetActive(false);
        LogLongPress($"EnsureLongPressProgressView: instantiated '{longPressProgressViewInstance.name}' from prefab under '{parent.name}'");
    }

    private Transform ResolveLongPressProgressParent()
    {
        if (longPressProgressViewParent != null)
            return longPressProgressViewParent;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            return root.transform;
        }

        return transform;
    }

    private void LogLongPress(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!debugLongPress)
            return;
        Debug.Log($"[BattleLongPressHUD] {name}: {message}", this);
#endif
    }

    private BattleButtonTooltipData BuildTooltipData(Button button)
    {
        if (TryBuildManualTooltipData(button, out var manualData))
            return manualData;

        if (TryResolveCombatAction(button, out var actionId))
        {
            return BuildCombatActionTooltipData(actionId);
        }

        string token = ResolveButtonIntentToken(button);
        return BuildButtonTokenTooltipData(token);
    }

    private bool TryBuildManualTooltipData(Button button, out BattleButtonTooltipData data)
    {
        data = default;
        if (button == null || manualTooltipButtons == null || manualTooltipButtons.Length == 0)
            return false;

        for (int i = 0; i < manualTooltipButtons.Length; i++)
        {
            var binding = manualTooltipButtons[i];
            if (binding == null || binding.button != button)
                continue;

            if (binding.mode == TooltipButtonBindingMode.CombatAction)
            {
                data = BuildCombatActionTooltipData(binding.combatActionId);
                return true;
            }

            string token = string.IsNullOrWhiteSpace(binding.buttonToken)
                ? ResolveButtonIntentToken(button)
                : ToSnakeCase(binding.buttonToken);

            data = BuildButtonTokenTooltipData(token);
            return true;
        }

        return false;
    }

    private BattleButtonTooltipData BuildCombatActionTooltipData(CombatActionId actionId)
    {
        var actionToken = ToSnakeCase(actionId.ToString());
        var actionData = tooltipActionRegistry != null ? tooltipActionRegistry.Get(actionId) : null;

        int hpDamage = actionData != null
            ? CombatDamageModel.ComputeHpDamage(tooltipPlayerPhysicalDamage, tooltipPlayerMagicDamage, actionData)
            : 0;
        int damage = actionData != null ? Mathf.Max(0, hpDamage + actionData.LpDamage) : 0;
        int heal = actionData != null
            ? CombatDamageModel.ComputeSelfHealPreviewFromHpDamage(actionId, hpDamage, actionData)
            : 0;

        string titleKey = FormatSafe(actionNameKeyFormat, actionToken);
        if (string.IsNullOrWhiteSpace(titleKey))
            titleKey = $"battle_action_{actionToken}_name";

        string descriptionKey = FormatSafe(actionDescriptionKeyFormat, actionToken);
        if (string.IsNullOrWhiteSpace(descriptionKey))
            descriptionKey = $"battle_action_{actionToken}_desc";

        return new BattleButtonTooltipData
        {
            TitleKey = titleKey,
            DescriptionKey = descriptionKey,
            DamageFormatKey = damageFormatKey,
            Damage = damage,
            HealFormatKey = healFormatKey,
            Heal = heal,
            MpCostFormatKey = mpCostFormatKey,
            MpCost = actionData != null ? Mathf.Max(0, actionData.MpCost) : 0,
            SpCostFormatKey = spCostFormatKey,
            SpCost = actionData != null ? Mathf.Max(0, actionData.SpCost) : 0,
            LpCostFormatKey = lpCostFormatKey,
            LpCost = actionData != null ? Mathf.Max(0, actionData.LpCost) : 0
        };
    }

    private BattleButtonTooltipData BuildButtonTokenTooltipData(string token)
    {
        token = ToSnakeCase(token);
        if (string.IsNullOrWhiteSpace(token))
            token = "button";

        string titleKey = FormatSafe(buttonNameKeyFormat, token);
        if (string.IsNullOrWhiteSpace(titleKey))
            titleKey = $"battle_button_{token}_name";

        string descriptionKey = FormatSafe(buttonDescriptionKeyFormat, token);
        if (string.IsNullOrWhiteSpace(descriptionKey))
            descriptionKey = $"battle_button_{token}_desc";

        return new BattleButtonTooltipData
        {
            TitleKey = titleKey,
            DescriptionKey = descriptionKey,
            DamageFormatKey = damageFormatKey,
            Damage = 0,
            HealFormatKey = healFormatKey,
            Heal = 0,
            MpCostFormatKey = mpCostFormatKey,
            MpCost = 0,
            SpCostFormatKey = spCostFormatKey,
            SpCost = 0,
            LpCostFormatKey = lpCostFormatKey,
            LpCost = 0
        };
    }

    private bool TryResolveCombatAction(Button button, out CombatActionId actionId)
    {
        actionId = default;

        if (button == null)
            return false;

        if (button == defenseButton)
        {
            actionId = CombatActionId.Block;
            return true;
        }

        if (!TryResolveMethodToken(button, out var methodName))
            return false;

        switch (methodName)
        {
            case nameof(OnFastAttackPressed): actionId = CombatActionId.FastAttack; return true;
            case nameof(OnNormalAttackPressed): actionId = CombatActionId.NormalAttack; return true;
            case nameof(OnHeavyAttackPressed): actionId = CombatActionId.HeavyAttack; return true;
            case nameof(OnCounterAttackPressed): actionId = CombatActionId.CounterAttack; return true;

            case nameof(OnFireSpellPressed): actionId = CombatActionId.FireSpell; return true;
            case nameof(OnIceSpellPressed): actionId = CombatActionId.IceSpell; return true;
            case nameof(OnHolySpellPressed): actionId = CombatActionId.HolySpell; return true;
            case nameof(OnDarkSpellPressed): actionId = CombatActionId.DarkSpell; return true;

            case nameof(OnSeductionAct1Pressed): actionId = CombatActionId.SeductionAct1; return true;
            case nameof(OnSeductionAct2Pressed): actionId = CombatActionId.SeductionAct2; return true;
            case nameof(OnSeductionAct3Pressed): actionId = CombatActionId.SeductionAct3; return true;
            case nameof(OnSeductionAct4Pressed): actionId = CombatActionId.SeductionAct4; return true;
        }

        return false;
    }

    private string ResolveButtonIntentToken(Button button)
    {
        if (button == attackButton) return "attack_menu";
        if (button == defenseButton) return "defense";
        if (button == magicButton) return "magic_menu";
        if (button == actionsButton) return "actions_menu";
        if (button == seductionButton) return "seduction_menu";
        if (button == itemButton) return "item";
        if (button == exitButton) return "exit";

        if (TryResolveMethodToken(button, out var methodName))
        {
            switch (methodName)
            {
                case nameof(OnRunPressed): return "run";
                case nameof(OnSurrenderPressed): return "surrender";
                case nameof(OnSkipTurnPressed): return "skip_turn";
                case nameof(OnInventoryPressed): return "inventory";
                case nameof(OnBackPressed): return "back";
            }

            if (methodName.StartsWith("On", StringComparison.Ordinal) && methodName.EndsWith("Pressed", StringComparison.Ordinal))
            {
                var core = methodName.Substring(2, methodName.Length - 2 - "Pressed".Length);
                if (!string.IsNullOrEmpty(core))
                    return ToSnakeCase(core);
            }
        }

        if (button != null && !string.IsNullOrWhiteSpace(button.name))
            return ToSnakeCase(button.name);

        return "button";
    }

    private static bool TryResolveMethodToken(Button button, out string methodName)
    {
        methodName = null;
        if (button == null)
            return false;

        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            var target = button.onClick.GetPersistentTarget(i);
            if (target == null)
                continue;

            var candidate = button.onClick.GetPersistentMethodName(i);
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            methodName = candidate.Trim();
            return true;
        }

        return false;
    }

    private static string FormatSafe(string format, string token)
    {
        if (string.IsNullOrWhiteSpace(format))
            return string.Empty;

        try
        {
            return string.Format(format, token);
        }
        catch (FormatException)
        {
            return format;
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = new System.Text.StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsWhiteSpace(c) || c == '-' || c == '.')
            {
                if (chars.Length > 0 && chars[chars.Length - 1] != '_')
                    chars.Append('_');
                continue;
            }

            if (char.IsUpper(c))
            {
                if (i > 0 && chars.Length > 0 && chars[chars.Length - 1] != '_')
                    chars.Append('_');
                chars.Append(char.ToLowerInvariant(c));
                continue;
            }

            chars.Append(char.ToLowerInvariant(c));
        }

        return chars.ToString().Trim('_');
    }

    public void SetTooltipCombatContext(int playerPhysicalDamage, int playerMagicDamage)
    {
        tooltipPlayerPhysicalDamage = CombatDamageModel.NormalizeBaseAttack(playerPhysicalDamage);
        tooltipPlayerMagicDamage = CombatDamageModel.NormalizeBaseAttack(playerMagicDamage);
        SetupLongPressTooltips();
    }

    public void SetActions(IBattleUIActions actions)
    {
        this.actions = actions;

        if (!isWired)
        {
            WireButtons();
            isWired = true;
        }

        RefreshButtonsCache();
        SetupLongPressTooltips();

        ShowRootMenu();

        // Do not change input state here.
        // BattleController is the single source of truth for when input is enabled (player turn).
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
        inputEnabled = enabled;

        // Keep raycasting enabled so long-press tooltips can still work even when input is disabled.
        // Actual gameplay actions are still blocked by Button.interactable + CanInteract() checks.
        if (graphicRaycaster != null)
            graphicRaycaster.enabled = true;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = true;
        }

        if (inputDisabledOverlay != null)
            inputDisabledOverlay.SetActive(!enabled);

        if (dimWhenInputDisabled && inputVisualGroup != null)
        {
            inputVisualGroup.alpha = enabled ? 1f : Mathf.Clamp(disabledAlpha, 0.05f, 1f);
        }

        if (allButtons == null || allButtons.Length == 0)
            RefreshButtonsCache();

        if (allButtons != null)
        {
            for (int i = 0; i < allButtons.Length; i++)
            {
                var btn = allButtons[i];
                if (btn == null)
                    continue;
                btn.interactable = enabled;
            }
        }
    }

    private bool CanInteract()
    {
        return inputEnabled;
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

    public void OnAttackMenuPressed() { if (!CanInteract()) return; ShowAttackMenu(); }
    public void OnDefensePressed() { if (!CanInteract()) return; SelectAction(CombatActionId.Block); }
    public void OnActionsMenuPressed() { if (!CanInteract()) return; ShowActionsMenu(); }
    public void OnMagicMenuPressed() { if (!CanInteract()) return; ShowMagicMenu(); }
    public void OnSeductionMenuPressed() { if (!CanInteract()) return; ShowSeductionMenu(); }
    public void OnBackPressed() { if (!CanInteract()) return; ShowRootMenu(); }

    // ===== Submenu action button handlers =====

    public void OnFastAttackPressed() { if (!CanInteract()) return; SelectAction(CombatActionId.FastAttack); }
    public void OnNormalAttackPressed() { if (!CanInteract()) return; SelectAction(CombatActionId.NormalAttack); }
    public void OnHeavyAttackPressed() { if (!CanInteract()) return; SelectAction(CombatActionId.HeavyAttack); }
    public void OnCounterAttackPressed() { if (!CanInteract()) return; SelectAction(CombatActionId.CounterAttack); }

    public void OnFireSpellPressed() { if (!CanInteract()) return; SelectAction(CombatActionId.FireSpell); }
    public void OnIceSpellPressed() { if (!CanInteract()) return; SelectAction(CombatActionId.IceSpell); }
    public void OnHolySpellPressed() { if (!CanInteract()) return; SelectAction(CombatActionId.HolySpell); }
    public void OnDarkSpellPressed() { if (!CanInteract()) return; SelectAction(CombatActionId.DarkSpell); }

    public void OnSeductionAct1Pressed() { if (!CanInteract()) return; SelectAction(CombatActionId.SeductionAct1); }
    public void OnSeductionAct2Pressed() { if (!CanInteract()) return; SelectAction(CombatActionId.SeductionAct2); }
    public void OnSeductionAct3Pressed() { if (!CanInteract()) return; SelectAction(CombatActionId.SeductionAct3); }
    public void OnSeductionAct4Pressed() { if (!CanInteract()) return; SelectAction(CombatActionId.SeductionAct4); }

    // ===== Actions menu (non-combat intents) =====

    public void OnInventoryPressed()
    {
        if (!CanInteract())
            return;

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
        if (!CanInteract())
            return;

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
        if (!CanInteract())
            return;

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
        if (!CanInteract())
            return;

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

    public BattleStatusCatalog GetStatusCatalog()
    {
        return statusPanel != null ? statusPanel.StatusCatalog : null;
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
