using System;
using System.Collections.Generic;
using System.Linq;
using Game.Battle.Combat;
using Game.Battle.Combat.Actions;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleHUDController
{
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
}
