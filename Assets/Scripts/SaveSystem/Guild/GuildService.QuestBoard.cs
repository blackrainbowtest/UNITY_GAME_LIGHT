using System;
using System.Collections.Generic;
using System.Linq;
using Game.Progression;

namespace UDA2.SaveSystem.Guild
{
    public sealed partial class GuildService
    {
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

            var random = new Random(CombineSeed(save.time.day, save.time.minuteOfDay, guild.completedQuestsTotal));
            var spawnPlan = BuildRankSpawnPlanForCurrentRank();
            var picks = spawnPlan.Count > 0 ? spawnPlan.Count : Math.Max(1, boardConfig.questsPerDay);
            LogDebug($"Rebuild picks: picks={picks}, rankPlan={spawnPlan.Count}, seedDay={save.time.day}, seedMinute={save.time.minuteOfDay}, completedTotal={guild.completedQuestsTotal}");

            for (var i = 0; i < picks; i++)
            {
                var desiredRank = spawnPlan.Count > 0 ? (AdventurerRank?)spawnPlan[i] : null;
                var questId = TakeNextQuestFromPool(eligibleIds, random, desiredRank);
                if (string.IsNullOrWhiteSpace(questId) && desiredRank.HasValue)
                    questId = TakeNextQuestFromPool(eligibleIds, random, null);

                if (string.IsNullOrWhiteSpace(questId))
                    break;

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

        private List<AdventurerRank> BuildRankSpawnPlanForCurrentRank()
        {
            var plan = new List<AdventurerRank>();
            if (boardConfig == null)
                return plan;

            if (!boardConfig.TryGetRuleForPlayerRank(CurrentRank, out var rule) || rule == null || rule.rankCounts == null)
                return plan;

            for (var i = 0; i < rule.rankCounts.Count; i++)
            {
                var item = rule.rankCounts[i];
                if (item == null || item.count <= 0)
                    continue;

                for (var j = 0; j < item.count; j++)
                    plan.Add(item.questRequiredRank);
            }

            return plan;
        }

        private string TakeNextQuestFromPool(List<string> eligibleIds, Random random, AdventurerRank? requiredRank)
        {
            var guild = save.progress.guild;
            if (eligibleIds == null || eligibleIds.Count == 0)
                return null;

            if (guild.remainingQuestPoolIds.Count == 0)
                guild.remainingQuestPoolIds.AddRange(eligibleIds);

            if (guild.remainingQuestPoolIds.Count == 0)
                return null;

            var candidateIndexes = new List<int>();
            for (var i = 0; i < guild.remainingQuestPoolIds.Count; i++)
            {
                var questId = guild.remainingQuestPoolIds[i];
                if (string.IsNullOrWhiteSpace(questId))
                    continue;

                if (!requiredRank.HasValue)
                {
                    candidateIndexes.Add(i);
                    continue;
                }

                var quest = FindQuestById(questId);
                if (quest != null && quest.requiredRank == requiredRank.Value)
                    candidateIndexes.Add(i);
            }

            if (candidateIndexes.Count == 0)
                return null;

            var selectedCandidate = candidateIndexes[random.Next(0, candidateIndexes.Count)];
            var selectedQuestId = guild.remainingQuestPoolIds[selectedCandidate];
            guild.remainingQuestPoolIds.RemoveAt(selectedCandidate);
            return selectedQuestId;
        }
    }
}
