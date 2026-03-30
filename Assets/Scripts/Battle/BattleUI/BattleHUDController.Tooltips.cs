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

    public void SetTooltipCombatContext(int playerPhysicalDamage, int playerMagicDamage)
    {
        tooltipPlayerPhysicalDamage = CombatDamageModel.NormalizeBaseAttack(playerPhysicalDamage);
        tooltipPlayerMagicDamage = CombatDamageModel.NormalizeBaseAttack(playerMagicDamage);
        SetupLongPressTooltips();
    }
}
