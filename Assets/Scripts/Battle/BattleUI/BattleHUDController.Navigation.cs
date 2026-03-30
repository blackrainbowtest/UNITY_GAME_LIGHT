using Game.Battle.Combat.Actions;
using Game.Battle.UI;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleHUDController
{
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
}
