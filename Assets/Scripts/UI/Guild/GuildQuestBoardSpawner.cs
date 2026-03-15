using System;
using System.Collections.Generic;
using System.Linq;
using UDA2.SaveSystem.Guild;
using UnityEngine;

namespace UDA2.UI.Guild
{
    [DisallowMultipleComponent]
    public sealed class GuildQuestBoardSpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class QuestSlot
        {
            public Transform slotRoot;
            public GameObject emptyState;
        }

        [Header("Config")]
        [SerializeField] private GuildQuestBoardConfigAsset boardConfig;
        [SerializeField] private GuildRankProgressionConfigAsset rankConfig;
        [SerializeField] private bool configureRuntimeApiOnEnable = true;

        [Header("Slots")]
        [SerializeField] private List<QuestSlot> slots = new List<QuestSlot>();
        [Tooltip("Card prefab GameObject. It must contain GuildQuestBoardCardView component.")]
        [SerializeField] private GameObject questCardPrefab;
        [Tooltip("If enabled, slotRoot GameObject is hidden when there is no quest in the slot.")]
        [SerializeField] private bool hideSlotRootWhenEmpty = true;

        [Header("Details")]
        [Tooltip("Details prefab GameObject. It must contain GuildQuestDetailsView component.")]
        [SerializeField] private GameObject questDetailsPrefab;
        [Tooltip("Optional explicit parent for details window. If empty, root Canvas is auto-detected.")]
        [SerializeField] private Transform detailsParent;
        [Tooltip("If true, details prefab RectTransform is stretched to fill parent.")]
        [SerializeField] private bool forceFullscreenDetails = true;

        [Header("Lifecycle")]
        [SerializeField] private bool refreshOnEnable = true;
        [SerializeField] private bool debugLogs = false;

        private readonly struct DisplayQuest
        {
            public readonly string questId;
            public readonly bool isTaken;

            public DisplayQuest(string questId, bool isTaken)
            {
                this.questId = questId;
                this.isTaken = isTaken;
            }
        }

        private readonly List<GameObject> spawnedCards = new List<GameObject>();
        private bool runtimeConfigured;

        private void OnEnable()
        {
            EnsureRuntimeConfigured();
            if (refreshOnEnable)
                RefreshBoard();
        }

        public void RefreshBoard()
        {
            LogDebug($"RefreshBoard start: slots={slots.Count}, hasCardPrefab={questCardPrefab != null}, hasDetailsPrefab={questDetailsPrefab != null}");
            ClearSpawnedCards();
            SetAllSlotsHasQuest(hasQuest: false);

            var service = GuildRuntimeAPI.GetService();
            if (service == null)
            {
                LogDebug("RefreshBoard aborted: GuildRuntimeAPI.GetService() returned null");
                return;
            }

            var refreshed = service.RefreshQuestBoardIfNeeded();
            if (refreshed)
            {
                var save = GameState.Instance?.CurrentSave;
                if (save != null)
                    SaveSlotsManager.SaveToSlot(SaveSlotsManager.GetRuntimeSaveSlotOrAutosave(), save, rememberAsCurrentRuntimeSlot: false);

                LogDebug($"Service refreshed board and state was saved to slot {SaveSlotsManager.GetRuntimeSaveSlotOrAutosave()}");
            }

            var displayQuests = BuildDisplayQuestList(service);
            if (displayQuests.Count == 0)
            {
                LogDebug("RefreshBoard: display quest list is empty after refresh attempt");
                return;
            }

            LogDebug($"RefreshBoard: display quests count = {displayQuests.Count}");

            var questMap = BuildQuestMap();
            var slotCount = Math.Min(slots.Count, displayQuests.Count);
            LogDebug($"RefreshBoard: questMap={questMap.Count}, slotCount={slotCount}");

            for (var i = 0; i < slotCount; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.slotRoot == null)
                    continue;

                var displayQuest = displayQuests[i];
                var questId = displayQuest.questId;
                if (string.IsNullOrWhiteSpace(questId) || !questMap.TryGetValue(questId, out var quest) || quest == null)
                {
                    LogDebug($"Slot {i}: skipped (questId='{questId ?? "null"}')");
                    continue;
                }

                SpawnCard(slot, quest, displayQuest.isTaken);
            }

            LogDebug($"RefreshBoard done: rendered={spawnedCards.Count}");
        }

        private void SpawnCard(QuestSlot slot, GuildQuestDefinitionAsset quest, bool isTaken)
        {
            if (slot == null || slot.slotRoot == null)
                return;

            SetSlotHasQuest(slot, hasQuest: true);

            if (questCardPrefab != null)
            {
                var cardGo = Instantiate(questCardPrefab, slot.slotRoot, worldPositionStays: false);
                if (cardGo == null)
                    return;

                cardGo.SetActive(true);
                var card = ResolveCardView(cardGo);
                if (card == null)
                {
                    Debug.LogWarning("[GuildQuestBoardSpawner] questCardPrefab has no GuildQuestBoardCardView component.", this);
                    return;
                }

                card.Bind(quest, q => HandleQuestCardClicked(q, isTaken), isTaken);
                spawnedCards.Add(cardGo);
                LogDebug($"SpawnCard: instantiated prefab card for questId={quest.questId}, taken={isTaken}");
                return;
            }

            // Fallback mode: use component on slot object itself when no prefab is assigned.
            var slotCard = ResolveCardView(slot.slotRoot.gameObject);
            if (slotCard != null)
            {
                slotCard.Bind(quest, q => HandleQuestCardClicked(q, isTaken), isTaken);
                LogDebug($"SpawnCard: bound existing slot card for questId={quest.questId}, taken={isTaken}");
            }
            else
            {
                LogDebug($"SpawnCard: no GuildQuestBoardCardView on slotRoot '{slot.slotRoot.name}'");
            }
        }

        private void HandleQuestCardClicked(GuildQuestDefinitionAsset quest, bool isTaken)
        {
            if (quest == null || questDetailsPrefab == null)
                return;

            var parent = ResolveDetailsParent();
            var detailsGo = Instantiate(questDetailsPrefab, parent, worldPositionStays: false);
            BringDetailsToFront(detailsGo, parent);
            var details = ResolveDetailsView(detailsGo);
            if (details == null)
            {
                Debug.LogWarning("[GuildQuestBoardSpawner] questDetailsPrefab has no GuildQuestDetailsView component.", this);
                return;
            }

            details.Bind(quest, TryAcceptQuest, isTaken);
        }

        private static List<DisplayQuest> BuildDisplayQuestList(GuildService service)
        {
            var result = new List<DisplayQuest>();
            if (service == null)
                return result;

            var active = service.GetActiveQuestIds();
            var selected = service.GetSelectedQuestIds();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            if (active != null)
            {
                for (var i = 0; i < active.Count; i++)
                {
                    var id = active[i];
                    if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                        continue;

                    result.Add(new DisplayQuest(id, isTaken: false));
                }
            }

            if (selected != null)
            {
                for (var i = 0; i < selected.Count; i++)
                {
                    var id = selected[i];
                    if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                        continue;

                    result.Add(new DisplayQuest(id, isTaken: true));
                }
            }

            return result;
        }

        private bool TryAcceptQuest(string questId)
        {
            if (!GuildRuntimeAPI.TrySelectQuest(questId))
                return false;

            var save = GameState.Instance?.CurrentSave;
            if (save != null)
                SaveSlotsManager.SaveToSlot(SaveSlotsManager.GetRuntimeSaveSlotOrAutosave(), save, rememberAsCurrentRuntimeSlot: false);

            RefreshBoard();
            return true;
        }

        private Dictionary<string, GuildQuestDefinitionAsset> BuildQuestMap()
        {
            if (boardConfig == null || boardConfig.questPool == null)
            {
                LogDebug("BuildQuestMap: boardConfig or questPool is null");
                return new Dictionary<string, GuildQuestDefinitionAsset>(StringComparer.Ordinal);
            }

            var map = boardConfig.questPool
                .Where(q => q != null && !string.IsNullOrWhiteSpace(q.questId))
                .GroupBy(q => q.questId.Trim(), StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            LogDebug($"BuildQuestMap: pool={boardConfig.questPool.Count}, map={map.Count}");
            return map;
        }

        private void EnsureRuntimeConfigured()
        {
            if (!configureRuntimeApiOnEnable || runtimeConfigured)
                return;

            GuildRuntimeAPI.Configure(rankConfig, boardConfig);
            runtimeConfigured = true;
            LogDebug("Runtime API configured");
        }

        private Transform ResolveDetailsParent()
        {
            if (detailsParent != null)
                return detailsParent;

            var ownCanvas = GetComponentInParent<Canvas>();
            if (ownCanvas != null && ownCanvas.isActiveAndEnabled)
                return ownCanvas.transform;

#if UNITY_2023_1_OR_NEWER || UNITY_2022_2_OR_NEWER
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            var canvases = FindObjectsOfType<Canvas>(true);
#pragma warning restore CS0618
#endif
            if (canvases == null || canvases.Length == 0)
                return transform;

            Canvas best = null;
            var bestScore = int.MinValue;
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null || !c.isActiveAndEnabled)
                    continue;

                var score = c.sortingOrder;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    score += 10000;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = c;
            }

            return best != null ? best.transform : transform;
        }

        private void BringDetailsToFront(GameObject detailsGo, Transform parent)
        {
            if (detailsGo == null)
                return;

            detailsGo.transform.SetAsLastSibling();

            if (!forceFullscreenDetails)
                return;

            var rt = detailsGo.GetComponent<RectTransform>();
            if (rt == null)
                return;

            if (parent is RectTransform)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
        }

        private void ClearSpawnedCards()
        {
            for (var i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    Destroy(spawnedCards[i]);
            }

            spawnedCards.Clear();
        }

        private void SetAllSlotsHasQuest(bool hasQuest)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null)
                    continue;

                SetSlotHasQuest(slot, hasQuest);
            }
        }

        private void SetSlotHasQuest(QuestSlot slot, bool hasQuest)
        {
            if (slot == null)
                return;

            if (slot.slotRoot != null && hideSlotRootWhenEmpty)
                slot.slotRoot.gameObject.SetActive(hasQuest);

            if (slot.emptyState != null)
                slot.emptyState.SetActive(!hasQuest && !hideSlotRootWhenEmpty);
        }

        private static GuildQuestBoardCardView ResolveCardView(GameObject root)
        {
            if (root == null)
                return null;

            var view = root.GetComponent<GuildQuestBoardCardView>();
            if (view != null)
                return view;

            return root.GetComponentInChildren<GuildQuestBoardCardView>(includeInactive: true);
        }

        private static GuildQuestDetailsView ResolveDetailsView(GameObject root)
        {
            if (root == null)
                return null;

            var view = root.GetComponent<GuildQuestDetailsView>();
            if (view != null)
                return view;

            return root.GetComponentInChildren<GuildQuestDetailsView>(includeInactive: true);
        }

        private void LogDebug(string message)
        {
            if (!debugLogs)
                return;

            Debug.Log($"[GuildQuestBoardSpawner] {message}", this);
        }
    }
}
