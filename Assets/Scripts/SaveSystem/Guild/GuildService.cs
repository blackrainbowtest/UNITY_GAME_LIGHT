using System;
using System.Collections.Generic;
using System.Linq;
using Game.Progression;

namespace UDA2.SaveSystem.Guild
{
    public sealed class GuildService
    {
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
                return false;

            var nowDay = Math.Max(0, save.time.day);
            var nowMinute = ClampMinuteOfDay(save.time.minuteOfDay);

            var shouldRefresh =
                nowMinute >= boardConfig.refreshMinuteOfDay &&
                save.progress.guild.lastQuestRefreshDay < nowDay;

            if (!shouldRefresh)
                return false;

            RebuildActiveQuestBoard();
            save.progress.guild.lastQuestRefreshDay = nowDay;
            return true;
        }

        private void RebuildActiveQuestBoard()
        {
            var guild = save.progress.guild;
            guild.activeQuestIds.Clear();

            var eligibleIds = GetEligibleQuestIds();
            if (eligibleIds.Count == 0)
                return;

            if (guild.remainingQuestPoolIds.Count == 0)
                guild.remainingQuestPoolIds.AddRange(eligibleIds);
            else
                guild.remainingQuestPoolIds = guild.remainingQuestPoolIds
                    .Where(id => eligibleIds.Contains(id))
                    .Distinct()
                    .ToList();

            if (guild.remainingQuestPoolIds.Count == 0)
                guild.remainingQuestPoolIds.AddRange(eligibleIds);

            var random = new Random(CombineSeed(save.time.day, save.time.minuteOfDay, guild.completedQuestsTotal));
            var picks = Math.Max(1, boardConfig.questsPerDay);

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
        }

        private List<string> GetEligibleQuestIds()
        {
            if (boardConfig == null || boardConfig.questPool == null)
                return new List<string>();

            return boardConfig.questPool
                .Where(q =>
                    q != null &&
                    !string.IsNullOrWhiteSpace(q.questId) &&
                    CanTakeQuest(q) &&
                    !save.progress.guild.selectedQuestIds.Contains(q.questId))
                .Select(q => q.questId)
                .Distinct()
                .ToList();
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

            return HasItems(requirement.requiredItems);
        }

        private bool HasQuestTurnInRequirements(GuildQuestDefinitionAsset quest)
        {
            if (save.inventory.gold < Math.Max(0, quest.requiredGold))
                return false;

            return HasItems(quest.requiredItems);
        }

        private bool HasItems(List<GuildItemAmount> requirements)
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

                var current = save.inventory?.items?.FirstOrDefault(i => i.itemId == requirement.itemId)?.count ?? 0;
                if (current < needed)
                    return false;
            }

            return true;
        }

        private void ConsumeRankRequirements(GuildRankRequirement requirement)
        {
            save.inventory.gold -= Math.Max(0, requirement.requiredGold);
            ConsumeItems(requirement.requiredItems);
        }

        private void ConsumeQuestTurnInRequirements(GuildQuestDefinitionAsset quest)
        {
            save.inventory.gold -= Math.Max(0, quest.requiredGold);
            ConsumeItems(quest.requiredItems);
        }

        private void GrantQuestRewards(GuildQuestDefinitionAsset quest)
        {
            save.inventory.gold += Math.Max(0, quest.rewardGold);
            save.player.exp = Math.Max(0, save.player.exp + Math.Max(0, quest.rewardExp));
            AddItems(quest.rewardItems);
        }

        private void ConsumeItems(List<GuildItemAmount> requiredItems)
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

                var item = save.inventory.items.FirstOrDefault(i => i.itemId == requirement.itemId);
                if (item == null)
                    continue;

                item.count = Math.Max(0, item.count - needed);
            }

            save.inventory.items.RemoveAll(i => i == null || i.count <= 0 || string.IsNullOrWhiteSpace(i.itemId));
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
            save.player ??= new SaveData.Player();
            save.time ??= new SaveData.TimeState();

            save.time.minuteOfDay = ClampMinuteOfDay(save.time.minuteOfDay);
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
    }
}
