using System;
using System.Collections.Generic;
using System.Linq;
using Game.Progression;
using UnityEngine;

namespace UDA2.SaveSystem.Guild
{
    public sealed class GuildService
    {
        private const int FirstRegistrationGoldCost = 10;
        private static readonly bool EnableDebugLogs = false;

        private readonly SaveData save;
        private readonly GuildRankProgressionConfigAsset rankConfig;
        private readonly GuildQuestBoardConfigAsset boardConfig;

        public GuildService(
            SaveData save,
            GuildRankProgressionConfigAsset rankConfig,
            GuildQuestBoardConfigAsset boardConfig)
        {
            this.save = save ?? throw new ArgumentNullException(nameof(save));
            this.rankConfig = rankConfig;
            this.boardConfig = boardConfig;
            EnsureGuildStateInitialized();
        }

        public AdventurerRank CurrentRank => save.progress?.adventurerRank ?? AdventurerRank.None;

        public bool TryBuildRankUpViewData(out GuildRankUpViewData data)
        {
            data = null;
            if (!TryGetNextRankRequirement(out var requirement) || requirement == null)
                return false;

            var currentGold = Math.Max(0, save.inventory?.gold ?? 0);
            var currentHeroLevel = Math.Max(1, save.player?.level ?? 1);
            var currentCompleted = Math.Max(0, save.progress?.guild?.completedQuestsSinceLastRank ?? 0);

            data = new GuildRankUpViewData
            {
                currentRank = CurrentRank,
                targetRank = requirement.targetRank,
                requiredGold = Math.Max(0, requirement.requiredGold),
                currentGold = currentGold,
                requiredHeroLevel = Math.Max(1, requirement.requiredHeroLevel),
                currentHeroLevel = currentHeroLevel,
                requiredCompletedQuests = Math.Max(0, requirement.requiredCompletedQuests),
                currentCompletedQuests = currentCompleted,
                canRankUpNow = CanRankUp(out _)
            };

            if (requirement.requiredItems == null)
                return true;

            for (var i = 0; i < requirement.requiredItems.Count; i++)
            {
                var req = requirement.requiredItems[i];
                if (req == null || string.IsNullOrWhiteSpace(req.itemId))
                    continue;

                var needed = Math.Max(0, req.amount);
                var inv = GetInventoryItemCount(req.itemId);
                var storage = GetStorageItemCount(req.itemId);
                var total = inv + storage;

                data.requiredItems.Add(new GuildItemRequirementProgress
                {
                    itemId = req.itemId,
                    required = needed,
                    inventoryOwned = inv,
                    storageOwned = storage,
                    totalOwned = total,
                    isMet = total >= needed
                });
            }

            return true;
        }

        public IReadOnlyList<string> GetActiveQuestIds()
        {
            EnsureGuildStateInitialized();
            return save.progress.guild.activeQuestIds;
        }

        public IReadOnlyList<string> GetSelectedQuestIds()
        {
            EnsureGuildStateInitialized();
            return save.progress.guild.selectedQuestIds;
        }

        public IReadOnlyList<string> GetCompletedQuestIds()
        {
            EnsureGuildStateInitialized();
            return save.progress.guild.completedQuestIds;
        }

        public IReadOnlyList<string> GetFailedQuestIds()
        {
            EnsureGuildStateInitialized();
            return save.progress.guild.failedQuestIds;
        }

        public bool TryGetNextRankRequirement(out GuildRankRequirement requirement)
        {
            requirement = null;

            if (CurrentRank == AdventurerRank.None)
            {
                requirement = new GuildRankRequirement
                {
                    targetRank = AdventurerRank.G,
                    requiredGold = FirstRegistrationGoldCost,
                    requiredHeroLevel = 1,
                    requiredCompletedQuests = 0,
                    requiredItems = new List<GuildItemAmount>()
                };

                return true;
            }

            if (rankConfig == null)
                return false;

            return rankConfig.TryGetNextRequirement(CurrentRank, out requirement);
        }

        public bool CanRankUp(out GuildRankRequirement requirement)
        {
            if (!TryGetNextRankRequirement(out requirement))
                return false;

            return HasRequirements(requirement);
        }

        public bool TryRankUp(out AdventurerRank newRank)
        {
            newRank = CurrentRank;
            if (!CanRankUp(out var requirement))
                return false;

            ConsumeRankRequirements(requirement);
            save.progress.adventurerRank = requirement.targetRank;
            save.progress.guild.completedQuestsSinceLastRank = 0;
            newRank = requirement.targetRank;
            return true;
        }

        public bool TrySubmitQuest(string questId, out GuildQuestDefinitionAsset quest)
        {
            quest = null;
            if (string.IsNullOrWhiteSpace(questId))
                return false;

            RefreshQuestBoardIfNeeded();

            if (!save.progress.guild.selectedQuestIds.Contains(questId))
                return false;

            quest = FindQuestById(questId);
            if (quest == null)
                return false;

            if (!CanTakeQuest(quest) || !HasQuestTurnInRequirements(quest))
                return false;

            ConsumeQuestTurnInRequirements(quest);
            GrantQuestRewards(quest);

            save.progress.guild.activeQuestIds.Remove(questId);
            save.progress.guild.selectedQuestIds.Remove(questId);
            AddUnique(save.progress.guild.completedQuestIds, questId);
            save.progress.guild.completedQuestsSinceLastRank++;
            save.progress.guild.completedQuestsTotal++;

            return true;
        }

        public bool TrySelectQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return false;

            RefreshQuestBoardIfNeeded();

            if (!save.progress.guild.activeQuestIds.Contains(questId))
                return false;

            var quest = FindQuestById(questId);
            if (!CanTakeQuest(quest))
                return false;

            if (save.progress.guild.selectedQuestIds.Contains(questId))
                return true;

            save.progress.guild.activeQuestIds.Remove(questId);
            AddUnique(save.progress.guild.selectedQuestIds, questId);
            return true;
        }

        public bool TryCancelSelectedQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return false;

            if (!save.progress.guild.selectedQuestIds.Remove(questId))
                return false;

            AddUnique(save.progress.guild.failedQuestIds, questId);
            return true;
        }

        public bool RefreshQuestBoardIfNeeded()
        {
            EnsureGuildStateInitialized();
            if (boardConfig == null)
            {
                LogDebug("Refresh skipped: boardConfig is null");
                return false;
            }

            var nowDay = Math.Max(0, save.time.day);
            var nowMinute = ClampMinuteOfDay(save.time.minuteOfDay);

            // Initial bootstrap: if board has never been generated in this save,
            // create first set immediately instead of waiting for refresh minute.
            var isInitialBootstrap =
                save.progress.guild.lastQuestRefreshDay <= 0 &&
                save.progress.guild.activeQuestIds.Count == 0 &&
                save.progress.guild.selectedQuestIds.Count == 0;

            var hasNoCurrentBoard = HasNoCurrentBoard();

            var hasGuildHistory =
                save.progress.guild.lastQuestRefreshDay > 0 ||
                save.progress.guild.completedQuestIds.Count > 0 ||
                save.progress.guild.failedQuestIds.Count > 0 ||
                save.progress.guild.remainingQuestPoolIds.Count > 0;

            // Compatibility recovery for old saves: if board state is empty in migrated saves,
            // regenerate once for the current day.
            var isCompatibilityRecovery =
                hasNoCurrentBoard &&
                hasGuildHistory &&
                save.progress.guild.lastQuestRefreshDay < nowDay;

            // Legacy bug recovery: some saves were marked as already refreshed for this day,
            // but board remained empty. Force one rebuild attempt.
            var isStaleMarkedDayRecovery = IsLikelyLegacyEmptyBoard(nowDay);

            var shouldRefresh =
                isInitialBootstrap ||
                isCompatibilityRecovery ||
                isStaleMarkedDayRecovery ||
                (nowMinute >= boardConfig.refreshMinuteOfDay &&
                 save.progress.guild.lastQuestRefreshDay < nowDay);

            LogDebug(
                $"Refresh check: day={nowDay}, minute={nowMinute}, refreshAt={boardConfig.refreshMinuteOfDay}, " +
                $"lastDay={save.progress.guild.lastQuestRefreshDay}, active={save.progress.guild.activeQuestIds.Count}, " +
                $"selected={save.progress.guild.selectedQuestIds.Count}, completed={save.progress.guild.completedQuestIds.Count}, " +
                $"failed={save.progress.guild.failedQuestIds.Count}, pool={save.progress.guild.remainingQuestPoolIds.Count}, " +
                $"initial={isInitialBootstrap}, " +
                $"compat={isCompatibilityRecovery}, staleRecovery={isStaleMarkedDayRecovery}, shouldRefresh={shouldRefresh}");

            if (!shouldRefresh)
                return false;

            var built = RebuildActiveQuestBoard();
            if (!built)
            {
                LogDebug("Refresh aborted: no quests were generated (day marker not updated, will retry later)");
                return false;
            }

            save.progress.guild.lastQuestRefreshDay = nowDay;
            LogDebug($"Refresh applied: new active={save.progress.guild.activeQuestIds.Count}, lastDay={save.progress.guild.lastQuestRefreshDay}");
            return true;
        }

        private bool RebuildActiveQuestBoard()
        {
            var guild = save.progress.guild;
            guild.activeQuestIds.Clear();

            var eligibleIds = GetEligibleQuestIds();
            LogDebug($"Rebuild start: eligible={eligibleIds.Count}, poolBefore={guild.remainingQuestPoolIds.Count}");
            if (eligibleIds.Count == 0)
            {
                LogDebug("Rebuild aborted: eligibleIds is empty");
                return false;
            }

            if (guild.remainingQuestPoolIds.Count == 0)
                guild.remainingQuestPoolIds.AddRange(eligibleIds);
            else
                guild.remainingQuestPoolIds = guild.remainingQuestPoolIds
                    .Where(id => eligibleIds.Contains(id))
                    .Distinct()
                    .ToList();

            if (guild.remainingQuestPoolIds.Count == 0)
                guild.remainingQuestPoolIds.AddRange(eligibleIds);

            var random = new System.Random(CombineSeed(save.time.day, save.time.minuteOfDay, guild.completedQuestsTotal));
            var picks = Math.Max(1, boardConfig.questsPerDay);
            LogDebug($"Rebuild picks: picks={picks}, seedDay={save.time.day}, seedMinute={save.time.minuteOfDay}, completedTotal={guild.completedQuestsTotal}");

            for (var i = 0; i < picks; i++)
            {
                if (guild.remainingQuestPoolIds.Count == 0)
                    guild.remainingQuestPoolIds.AddRange(eligibleIds);

                if (guild.remainingQuestPoolIds.Count == 0)
                    break;

                var index = random.Next(0, guild.remainingQuestPoolIds.Count);
                var questId = guild.remainingQuestPoolIds[index];
                guild.remainingQuestPoolIds.RemoveAt(index);
                if (!guild.activeQuestIds.Contains(questId))
                    guild.activeQuestIds.Add(questId);
            }

            LogDebug($"Rebuild done: active={guild.activeQuestIds.Count}, poolAfter={guild.remainingQuestPoolIds.Count}");
            return guild.activeQuestIds.Count > 0;
        }

        private List<string> GetEligibleQuestIds()
        {
            if (boardConfig == null || boardConfig.questPool == null)
                return new List<string>();

            var eligible = new List<string>();
            var heroLevel = Math.Max(1, save.player?.level ?? 1);

            // Daily quests are repeatable: completed/failed history must not block future offers.
            // We only exclude quests already in progress (selected).
            for (var i = 0; i < boardConfig.questPool.Count; i++)
            {
                var q = boardConfig.questPool[i];
                if (q == null)
                {
                    LogDebug($"Eligible skip [#{i}]: null quest reference");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(q.questId))
                {
                    LogDebug($"Eligible skip [#{i}]: empty questId (titleKey='{q.titleLocalizationKey ?? string.Empty}')");
                    continue;
                }

                if (save.progress.guild.selectedQuestIds.Contains(q.questId))
                {
                    LogDebug($"Eligible skip [{q.questId}]: already selected");
                    continue;
                }

                if (CurrentRank < q.requiredRank)
                {
                    LogDebug($"Eligible skip [{q.questId}]: rank too low ({CurrentRank} < {q.requiredRank})");
                    continue;
                }

                var requiredLevel = Math.Max(1, q.requiredHeroLevel);
                if (heroLevel < requiredLevel)
                {
                    LogDebug($"Eligible skip [{q.questId}]: hero level too low ({heroLevel} < {requiredLevel})");
                    continue;
                }

                if (!eligible.Contains(q.questId))
                    eligible.Add(q.questId);
            }

            LogDebug($"Eligible quests: pool={boardConfig.questPool.Count}, eligible={eligible.Count}, rank={CurrentRank}, heroLevel={heroLevel}");
            return eligible;
        }

        private static void LogDebug(string message)
        {
            if (!EnableDebugLogs)
                return;

            Debug.Log($"[GuildService] {message}");
        }

        private GuildQuestDefinitionAsset FindQuestById(string questId)
        {
            if (boardConfig == null || boardConfig.questPool == null)
                return null;

            return boardConfig.questPool.FirstOrDefault(q => q != null && string.Equals(q.questId, questId, StringComparison.Ordinal));
        }

        private bool CanTakeQuest(GuildQuestDefinitionAsset quest)
        {
            if (quest == null)
                return false;

            var heroLevel = Math.Max(1, save.player?.level ?? 1);
            return CurrentRank >= quest.requiredRank && heroLevel >= Math.Max(1, quest.requiredHeroLevel);
        }

        private bool HasRequirements(GuildRankRequirement requirement)
        {
            if (requirement == null)
                return false;

            var heroLevel = Math.Max(1, save.player?.level ?? 1);
            if (heroLevel < Math.Max(1, requirement.requiredHeroLevel))
                return false;

            if (save.inventory.gold < Math.Max(0, requirement.requiredGold))
                return false;

            if (save.progress.guild.completedQuestsSinceLastRank < Math.Max(0, requirement.requiredCompletedQuests))
                return false;

            return HasItems(requirement.requiredItems, includeStorage: true);
        }

        private bool HasQuestTurnInRequirements(GuildQuestDefinitionAsset quest)
        {
            if (save.inventory.gold < Math.Max(0, quest.requiredGold))
                return false;

            return HasItems(quest.requiredItems, includeStorage: false);
        }

        private bool HasItems(List<GuildItemAmount> requirements, bool includeStorage)
        {
            if (requirements == null || requirements.Count == 0)
                return true;

            foreach (var requirement in requirements)
            {
                if (requirement == null || string.IsNullOrWhiteSpace(requirement.itemId))
                    continue;

                var needed = Math.Max(0, requirement.amount);
                if (needed == 0)
                    continue;

                var current = GetInventoryItemCount(requirement.itemId);
                if (includeStorage)
                    current += GetStorageItemCount(requirement.itemId);

                if (current < needed)
                    return false;
            }

            return true;
        }

        private void ConsumeRankRequirements(GuildRankRequirement requirement)
        {
            save.inventory.gold -= Math.Max(0, requirement.requiredGold);
            ConsumeItems(requirement.requiredItems, includeStorage: true);
        }

        private void ConsumeQuestTurnInRequirements(GuildQuestDefinitionAsset quest)
        {
            save.inventory.gold -= Math.Max(0, quest.requiredGold);
            ConsumeItems(quest.requiredItems, includeStorage: false);
        }

        private void GrantQuestRewards(GuildQuestDefinitionAsset quest)
        {
            save.inventory.gold += Math.Max(0, quest.rewardGold);
            save.player.exp = Math.Max(0, save.player.exp + Math.Max(0, quest.rewardExp));
            AddItems(quest.rewardItems);
        }

        private void ConsumeItems(List<GuildItemAmount> requiredItems, bool includeStorage)
        {
            if (requiredItems == null || requiredItems.Count == 0)
                return;

            foreach (var requirement in requiredItems)
            {
                if (requirement == null || string.IsNullOrWhiteSpace(requirement.itemId))
                    continue;

                var needed = Math.Max(0, requirement.amount);
                if (needed == 0)
                    continue;

                var leftToConsume = needed;

                var item = save.inventory.items.FirstOrDefault(i => i.itemId == requirement.itemId);
                if (item != null && leftToConsume > 0)
                {
                    var fromInventory = Math.Min(item.count, leftToConsume);
                    item.count = Math.Max(0, item.count - fromInventory);
                    leftToConsume -= fromInventory;
                }

                if (includeStorage && leftToConsume > 0)
                {
                    var storageItem = save.storage.items.FirstOrDefault(i => i.itemId == requirement.itemId);
                    if (storageItem != null)
                    {
                        var fromStorage = Math.Min(storageItem.count, leftToConsume);
                        storageItem.count = Math.Max(0, storageItem.count - fromStorage);
                    }
                }
            }

            save.inventory.items.RemoveAll(i => i == null || i.count <= 0 || string.IsNullOrWhiteSpace(i.itemId));
            if (save.storage?.items != null)
                save.storage.items.RemoveAll(i => i == null || i.count <= 0 || string.IsNullOrWhiteSpace(i.itemId));
        }

        private void AddItems(List<GuildItemAmount> rewardItems)
        {
            if (rewardItems == null || rewardItems.Count == 0)
                return;

            foreach (var reward in rewardItems)
            {
                if (reward == null || string.IsNullOrWhiteSpace(reward.itemId))
                    continue;

                var amount = Math.Max(0, reward.amount);
                if (amount == 0)
                    continue;

                var item = save.inventory.items.FirstOrDefault(i => i.itemId == reward.itemId);
                if (item == null)
                {
                    item = new SaveData.Item { itemId = reward.itemId, count = 0 };
                    save.inventory.items.Add(item);
                }

                item.count += amount;
            }
        }

        private void EnsureGuildStateInitialized()
        {
            save.progress ??= new SaveData.Progress();
            save.progress.guild ??= new SaveData.GuildState();
            save.progress.guild.activeQuestIds ??= new List<string>();
            save.progress.guild.selectedQuestIds ??= new List<string>();
            save.progress.guild.completedQuestIds ??= new List<string>();
            save.progress.guild.failedQuestIds ??= new List<string>();
            save.progress.guild.remainingQuestPoolIds ??= new List<string>();
            save.inventory ??= new SaveData.Inventory();
            save.inventory.items ??= new List<SaveData.Item>();
            save.storage ??= new SaveData.Storage();
            save.storage.items ??= new List<SaveData.Item>();
            save.player ??= new SaveData.Player();
            save.time ??= new SaveData.TimeState();

            SanitizeQuestState();
            save.time.minuteOfDay = ClampMinuteOfDay(save.time.minuteOfDay);
        }

        private void SanitizeQuestState()
        {
            var guild = save.progress.guild;
            if (guild == null)
                return;

            SanitizeQuestIdList(guild.activeQuestIds);
            SanitizeQuestIdList(guild.selectedQuestIds);
            SanitizeQuestIdList(guild.completedQuestIds);
            SanitizeQuestIdList(guild.failedQuestIds);
            SanitizeQuestIdList(guild.remainingQuestPoolIds);

            // Same quest cannot exist as both offer and accepted at the same time.
            guild.activeQuestIds.RemoveAll(guild.selectedQuestIds.Contains);
        }

        private static void SanitizeQuestIdList(List<string> ids)
        {
            if (ids == null)
                return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = ids.Count - 1; i >= 0; i--)
            {
                var id = ids[i]?.Trim();
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                {
                    ids.RemoveAt(i);
                    continue;
                }

                ids[i] = id;
            }
        }

        private int GetInventoryItemCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || save.inventory?.items == null)
                return 0;

            return Math.Max(0, save.inventory.items.FirstOrDefault(i => i.itemId == itemId)?.count ?? 0);
        }

        private int GetStorageItemCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || save.storage?.items == null)
                return 0;

            return Math.Max(0, save.storage.items.FirstOrDefault(i => i.itemId == itemId)?.count ?? 0);
        }

        private static void AddUnique(List<string> list, string questId)
        {
            if (list == null || string.IsNullOrWhiteSpace(questId))
                return;

            if (!list.Contains(questId))
                list.Add(questId);
        }

        private static int ClampMinuteOfDay(int value) => Math.Max(0, Math.Min(1439, value));

        private static int CombineSeed(int day, int minuteOfDay, int completed)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + day;
                hash = hash * 31 + minuteOfDay;
                hash = hash * 31 + completed;
                return hash;
            }
        }

        private bool HasNoCurrentBoard()
        {
            var guild = save.progress.guild;
            return guild.activeQuestIds.Count == 0 && guild.selectedQuestIds.Count == 0;
        }

        private bool IsLikelyLegacyEmptyBoard(int nowDay)
        {
            var guild = save.progress.guild;
            return
                HasNoCurrentBoard() &&
                guild.lastQuestRefreshDay >= nowDay &&
                guild.completedQuestIds.Count == 0 &&
                guild.failedQuestIds.Count == 0;
        }
    }
}
