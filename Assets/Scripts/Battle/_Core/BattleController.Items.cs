using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Game.Battle.UI;
using Game.Battle.Statuses;
using Game.Battle.Visual;
using UDA2.UI.Game;
using Logger = UDA2.Logging.Logger;

namespace Game.Battle
{
    public partial class BattleController
    {
        [Header("Inventory / Items")]
        [Tooltip("Optional. If null, BattleController will try to load Resources/Prefabs/UI/Profile/PlayerCharacterWindow.")]
        [SerializeField] private GameObject playerCharacterWindowPrefab;

        [Tooltip("Optional. If null, will attach to the best Canvas found (same heuristic as PlayerCharacterWindowOpener).")]
        [SerializeField] private Transform inventoryParentOverride;

        [Tooltip("Optional but required to APPLY item effects. Assign your ItemDatabase asset here.")]
        [SerializeField] private UnityEngine.Object itemDatabase;

        [Header("Battle Item Rules")]
        [Tooltip("If true: pressing Item after using an item this turn does nothing.")]
        [SerializeField] private bool blockOpeningInventoryAfterUse = true;

        [Header("Inventory Animations")]
        [SerializeField] private BattleVisualAnimId inventoryOpenAnim = BattleVisualAnimId.InventoryOpen;
        [SerializeField] private BattleVisualAnimId inventorySearchAnim = BattleVisualAnimId.InventorySearch;
        [SerializeField] private BattleVisualAnimId inventoryCloseAnim = BattleVisualAnimId.InventoryClose;

        private bool itemUsedThisPlayerTurn;
        private GameObject inventoryWindowInstance;
        private PlayerCharacterWindowController inventoryWindowController;
        private BattleInventoryItemClickHook inventoryClickHook;
        private Coroutine inventoryOpenRoutine;
        private Coroutine inventorySearchLoopRoutine;
        private Coroutine inventoryCloseRoutine;
        private bool isInventoryWindowOpen;
        private bool inventorySearchLoopActive;

        private void ResetPerTurnNonCombatActions()
        {
            itemUsedThisPlayerTurn = false;
        }

        private void OpenInventoryForBattleItemUse()
        {
            if (blockOpeningInventoryAfterUse && itemUsedThisPlayerTurn)
            {
                Logger.LogInfo("[BattleController] Item already used this turn.");
                return;
            }

            if (isInventoryWindowOpen || inventoryOpenRoutine != null)
                return;

            if (inventoryCloseRoutine != null)
                return;

            playerView?.SetAutoIdleFallbackEnabled(false);
            hudController?.SetInputEnabled(false);
            inventoryOpenRoutine = StartCoroutine(InventoryOpenSequenceRoutine());
        }

        private IEnumerator InventoryOpenSequenceRoutine()
        {
            yield return PlayCharacterAnimAndWait(playerView, inventoryOpenAnim);

            if (!ShowInventoryWindowInternal())
            {
                playerView?.SetAutoIdleFallbackEnabled(true);
                inventoryOpenRoutine = null;
                if (battleStarted && turnSystem != null && turnSystem.IsPlayerTurn)
                    hudController?.SetInputEnabled(true);
                yield break;
            }

            isInventoryWindowOpen = true;
            StartInventorySearchLoop();
            inventoryOpenRoutine = null;
        }

        private bool ShowInventoryWindowInternal()
        {

            if (inventoryWindowInstance != null)
            {
                inventoryWindowInstance.SetActive(true);
                inventoryWindowInstance.transform.SetAsLastSibling();
                return true;
            }

            var prefab = playerCharacterWindowPrefab;
            if (prefab == null)
                prefab = Resources.Load<GameObject>("Prefabs/UI/Profile/PlayerCharacterWindow");

            if (prefab == null)
            {
                Logger.LogError("[BattleController] PlayerCharacterWindow prefab is not assigned and could not be loaded from Resources/Prefabs/UI/Profile/PlayerCharacterWindow.");
                return false;
            }

            Transform parent = inventoryParentOverride != null ? inventoryParentOverride : FindBestCanvasTransform();
            inventoryWindowInstance = Instantiate(prefab, parent, worldPositionStays: false);
            if (inventoryWindowInstance == null)
                return false;

            inventoryWindowInstance.SetActive(true);
            inventoryWindowInstance.transform.SetAsLastSibling();

            inventoryWindowController = inventoryWindowInstance.GetComponentInChildren<PlayerCharacterWindowController>(true);
            if (inventoryWindowController != null)
            {
                inventoryWindowController.SetOwnerRoot(inventoryWindowInstance);
                inventoryWindowController.SelectTab(PlayerCharacterTabId.Inventory);
            }

            // Wire click hook so clicking an inventory slot uses the item (battle-only behavior).
            inventoryClickHook = inventoryWindowInstance.AddComponent<BattleInventoryItemClickHook>();
            inventoryClickHook.Init(
                onItemClicked: HandleBattleInventoryItemClicked,
                resolveIsAllowed: () => battleStarted && turnSystem != null && turnSystem.IsPlayerTurn && (!itemUsedThisPlayerTurn),
                requireInsideInventoryTabView: true
            );

            // Push item database to InventoryTabView if possible (so icons/types render correctly).
            if (itemDatabase != null && inventoryWindowInstance != null)
            {
                var invTabs = inventoryWindowInstance.GetComponentsInChildren<InventoryTabView>(true);
                for (int i = 0; i < invTabs.Length; i++)
                {
                    if (invTabs[i] == null) continue;
                    invTabs[i].SetItemDatabase(itemDatabase);
                    invTabs[i].Refresh();
                }
            }

            // IMPORTANT: close handler is implemented by PlayerCharacterWindowController (child),
            // not necessarily on the prefab root.
            var closeHandler = inventoryWindowController as global::IMenuCloseHandler
                ?? inventoryWindowInstance.GetComponentInChildren<global::IMenuCloseHandler>(true);
            if (closeHandler != null)
                closeHandler.OnMenuClosed += HandleInventoryClosed;
            else
                Logger.LogWarning("[BattleController] Inventory window has no IMenuCloseHandler; HUD input may remain locked after closing.");

            return true;
        }

        private void StartInventorySearchLoop()
        {
            inventorySearchLoopActive = true;

            if (inventorySearchLoopRoutine != null)
            {
                StopCoroutine(inventorySearchLoopRoutine);
                inventorySearchLoopRoutine = null;
            }

            inventorySearchLoopRoutine = StartCoroutine(InventorySearchLoopRoutine());
        }

        private IEnumerator InventorySearchLoopRoutine()
        {
            while (inventorySearchLoopActive && isInventoryWindowOpen && battleStarted && turnSystem != null && turnSystem.IsPlayerTurn)
            {
                if (playerView == null)
                    yield break;

                bool finished = false;
                playerView.PlayImmediate(inventorySearchAnim, onFinished: () => finished = true);

                while (!finished)
                {
                    if (!inventorySearchLoopActive || !isInventoryWindowOpen || !battleStarted || turnSystem == null || !turnSystem.IsPlayerTurn)
                        yield break;

                    yield return null;
                }

                yield return null;
            }

            inventorySearchLoopRoutine = null;
        }

        private void HandleInventoryClosed()
        {
            isInventoryWindowOpen = false;
            inventorySearchLoopActive = false;

            if (inventorySearchLoopRoutine != null)
            {
                StopCoroutine(inventorySearchLoopRoutine);
                inventorySearchLoopRoutine = null;
            }

            if (inventoryWindowController != null)
            {
                var closeHandler = inventoryWindowController as global::IMenuCloseHandler;
                if (closeHandler != null)
                    closeHandler.OnMenuClosed -= HandleInventoryClosed;
            }
            else if (inventoryWindowInstance != null)
            {
                // Fallback cleanup.
                var closeHandler = inventoryWindowInstance.GetComponentInChildren<global::IMenuCloseHandler>(true);
                if (closeHandler != null)
                    closeHandler.OnMenuClosed -= HandleInventoryClosed;
            }

            inventoryClickHook = null;
            inventoryWindowController = null;
            inventoryWindowInstance = null;

            if (inventoryCloseRoutine != null)
                StopCoroutine(inventoryCloseRoutine);

            inventoryCloseRoutine = StartCoroutine(InventoryCloseSequenceRoutine());
        }

        private IEnumerator InventoryCloseSequenceRoutine()
        {
            // Interrupt any currently running search animation immediately.
            yield return PlayCharacterAnimImmediateAndWait(playerView, inventoryCloseAnim);

            playerView?.SetAutoIdleFallbackEnabled(true);
            playerView?.Play(BattleVisualAnimId.Idle);

            if (battleStarted && turnSystem != null && turnSystem.IsPlayerTurn && !isInventoryWindowOpen)
                hudController?.SetInputEnabled(true);

            inventoryCloseRoutine = null;
        }

        private void HandleBattleInventoryItemClicked(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            if (!battleStarted || turnSystem == null || !turnSystem.IsPlayerTurn)
                return;

            if (itemUsedThisPlayerTurn)
                return;

            if (!TryUseInventoryItemInBattle(itemId.Trim()))
                return;

            itemUsedThisPlayerTurn = true;

            // Close inventory after a successful use (still player's turn).
            if (inventoryWindowController != null)
                inventoryWindowController.Close();

            PushHudState();
        }

        private bool TryUseInventoryItemInBattle(string itemId)
        {
            var save = global::GameState.Instance != null ? global::GameState.Instance.CurrentSave : null;
            if (save?.inventory?.items == null)
                return false;

            if (!TryGetInventoryCount(save.inventory.items, itemId, out var count) || count <= 0)
            {
                Logger.LogInfo($"[BattleController] Cannot use item '{itemId}': not in inventory.");
                return false;
            }

            if (itemDatabase == null)
            {
                Logger.LogError("[BattleController] itemDatabase is not assigned on BattleController. Cannot apply item effects. Assign ItemDatabase in the Battle scene on the BattleController component.");
                return false;
            }

            if (!TryResolveItemDefinition(itemDatabase, itemId, out var def) || def == null)
            {
                Logger.LogWarning($"[BattleController] ItemDefinition not found for '{itemId}'.");
                return false;
            }

            if (!TryGetBoolProperty(def, "Consumable", fallback: true))
            {
                Logger.LogInfo($"[BattleController] Item '{itemId}' is not consumable.");
                return false;
            }

            if (!IsBattleUsableItem(def))
            {
                Logger.LogInfo($"[BattleController] Item '{itemId}' cannot be used in battle (UseType)." );
                return false;
            }

            if (!HasAnyBattleEffect(def))
            {
                Logger.LogInfo($"[BattleController] Item '{itemId}' has no battle effects." );
                return false;
            }

            // Consume item from inventory first (atomic-ish behavior).
            if (!TryConsumeInventoryItem(save.inventory.items, itemId, amount: 1))
                return false;

            ApplyItemEffectsToBattleState(def);
            return true;
        }

        private static bool IsBattleUsableItem(object itemDef)
        {
            // Current data (items.json) uses usage.useType: "None" or "Drink".
            // Rule: only items with a non-None useType are usable during battle.
            var useType = TryGetStringProperty(itemDef, "UseType");
            if (string.IsNullOrWhiteSpace(useType))
                return false;

            return !string.Equals(useType.Trim(), "None", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasAnyBattleEffect(object itemDef)
        {
            if (itemDef == null)
                return false;

            if (TryGetIntProperty(itemDef, "HP") != 0) return true;
            if (TryGetIntProperty(itemDef, "MP") != 0) return true;
            if (TryGetIntProperty(itemDef, "SP") != 0) return true;
            if (TryGetIntProperty(itemDef, "LP") != 0) return true;

            try
            {
                var t = itemDef.GetType();
                var prop = t.GetProperty("StatusEffects", BindingFlags.Public | BindingFlags.Instance);
                var raw = prop != null ? prop.GetValue(itemDef) : null;
                if (raw is Array arr && arr.Length > 0)
                    return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }

        private static string TryGetStringProperty(object obj, string propName, string fallback = "")
        {
            if (obj == null || string.IsNullOrWhiteSpace(propName))
                return fallback;

            try
            {
                var t = obj.GetType();
                var prop = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                    return fallback;

                var v = prop.GetValue(obj);
                return v != null ? v.ToString() : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private void ApplyItemEffectsToBattleState(object itemDef)
        {
            if (itemDef == null)
                return;

            int addHp = TryGetIntProperty(itemDef, "HP");
            int addMp = TryGetIntProperty(itemDef, "MP");
            int addSp = TryGetIntProperty(itemDef, "SP");
            int addLp = TryGetIntProperty(itemDef, "LP");

            if (addHp != 0) combatState = combatState.WithPlayerHp(combatState.PlayerHp + addHp);
            if (addMp != 0) combatState = combatState.WithPlayerMp(combatState.PlayerMp + addMp);
            if (addSp != 0) combatState = combatState.WithPlayerSp(combatState.PlayerSp + addSp);
            if (addLp != 0) combatState = combatState.WithPlayerLp(combatState.PlayerLp + addLp);

            combatState = ClampPlayerResourcesToMax(combatState);

            // Grant statuses (buff scrolls etc). This is non-combat usage, so we do NOT consume CounterAttack windows.
            TryApplyGrantedStatuses(itemDef);
        }

        private void TryApplyGrantedStatuses(object itemDef)
        {
            try
            {
                var t = itemDef.GetType();
                var prop = t.GetProperty("StatusEffects", BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                    return;

                var raw = prop.GetValue(itemDef);
                if (raw is not Array arr || arr.Length == 0)
                    return;

                for (int i = 0; i < arr.Length; i++)
                {
                    var entry = arr.GetValue(i);
                    if (entry == null) continue;

                    var entryType = entry.GetType();
                    var idField = entryType.GetField("id", BindingFlags.Public | BindingFlags.Instance);
                    var turnsField = entryType.GetField("turns", BindingFlags.Public | BindingFlags.Instance);

                    var idObj = idField != null ? idField.GetValue(entry) : null;
                    var turnsObj = turnsField != null ? turnsField.GetValue(entry) : null;

                    if (idObj == null)
                        continue;

                    if (!Enum.TryParse(idObj.ToString(), ignoreCase: true, out StatusEffectId id))
                        continue;

                    int turns = 1;
                    if (turnsObj != null && int.TryParse(turnsObj.ToString(), out var parsedTurns))
                        turns = Mathf.Max(0, parsedTurns);

                    if (turns <= 0)
                        continue;

                    AddOrRefreshPlayerStatusInternal(id, turns);
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"[BattleController] Failed to apply item status effects: {e.Message}");
            }
        }

        private static bool TryResolveItemDefinition(UnityEngine.Object itemDatabaseObj, string itemId, out object definition)
        {
            definition = null;
            if (itemDatabaseObj == null || string.IsNullOrWhiteSpace(itemId))
                return false;

            try
            {
                var dbType = itemDatabaseObj.GetType();
                var getById = dbType.GetMethod("GetById", BindingFlags.Public | BindingFlags.Instance);
                if (getById == null)
                    return false;

                definition = getById.Invoke(itemDatabaseObj, new object[] { itemId.Trim() });
                return definition != null;
            }
            catch
            {
                definition = null;
                return false;
            }
        }

        private static int TryGetIntProperty(object obj, string propName, int fallback = 0)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propName))
                return fallback;

            try
            {
                var t = obj.GetType();
                var prop = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                    return fallback;

                var v = prop.GetValue(obj);
                if (v == null)
                    return fallback;

                return v is int i ? i : (int.TryParse(v.ToString(), out var parsed) ? parsed : fallback);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool TryGetBoolProperty(object obj, string propName, bool fallback)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propName))
                return fallback;

            try
            {
                var t = obj.GetType();
                var prop = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                    return fallback;

                var v = prop.GetValue(obj);
                if (v == null)
                    return fallback;

                return v is bool b ? b : (bool.TryParse(v.ToString(), out var parsed) ? parsed : fallback);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool TryGetInventoryCount(System.Collections.Generic.List<SaveData.Item> items, string itemId, out int count)
        {
            count = 0;
            if (items == null || string.IsNullOrWhiteSpace(itemId))
                return false;

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null) continue;
                if (!string.Equals(it.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                    continue;

                count = it.count;
                return true;
            }

            return false;
        }

        private static bool TryConsumeInventoryItem(System.Collections.Generic.List<SaveData.Item> items, string itemId, int amount)
        {
            if (items == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null) continue;
                if (!string.Equals(it.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (it.count < amount)
                    return false;

                it.count -= amount;
                if (it.count <= 0)
                    items.RemoveAt(i);

                return true;
            }

            return false;
        }

        private static Transform FindBestCanvasTransform()
        {
            Canvas[] canvases;
#if UNITY_2023_1_OR_NEWER || UNITY_2022_2_OR_NEWER
            canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
#pragma warning restore CS0618
#endif
            if (canvases == null || canvases.Length == 0)
                return null;

            Canvas best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null || !c.isActiveAndEnabled)
                    continue;

                int score = 0;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) score += 1000;
                score += c.sortingOrder;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best != null ? best.transform : null;
        }
    }
}
