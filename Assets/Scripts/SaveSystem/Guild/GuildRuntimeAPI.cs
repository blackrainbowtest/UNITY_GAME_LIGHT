using UnityEngine;
using Game.Progression;

namespace UDA2.SaveSystem.Guild
{
    public static class GuildRuntimeAPI
    {
        private static SaveData cachedSave;
        private static GuildService cachedService;
        private static GuildRankProgressionConfigAsset rankConfig;
        private static GuildQuestBoardConfigAsset boardConfig;

        public static void Configure(
            GuildRankProgressionConfigAsset rankConfigAsset,
            GuildQuestBoardConfigAsset boardConfigAsset)
        {
            rankConfig = rankConfigAsset;
            boardConfig = boardConfigAsset;
            cachedSave = null;
            cachedService = null;
        }

        public static GuildService GetService()
        {
            var save = global::GameState.Instance?.CurrentSave;
            if (save == null)
                return null;

            // Runtime service must be configured by scene/UI components.
            if (rankConfig == null || boardConfig == null)
                return null;

            if (ReferenceEquals(cachedSave, save) && cachedService != null)
                return cachedService;

            cachedSave = save;
            cachedService = new GuildService(save, rankConfig, boardConfig);
            return cachedService;
        }

        public static bool HandleTimeChanged(int day, int minuteOfDay)
        {
            var service = GetService();
            if (service == null)
                return false;

            var save = global::GameState.Instance?.CurrentSave;
            if (save?.time != null)
            {
                save.time.day = Mathf.Max(1, day);
                save.time.minuteOfDay = Mathf.Clamp(minuteOfDay, 0, 1439);
            }

            var refreshed = service.RefreshQuestBoardIfNeeded();
            if (refreshed)
            {
                if (save != null)
                    global::SaveSlotsManager.SaveToSlot(global::SaveSlotsManager.GetRuntimeSaveSlotOrAutosave(), save, rememberAsCurrentRuntimeSlot: false);

                GuildNotificationAPI.NotifyQuestBoardRefreshed();
            }

            return refreshed;
        }

        public static bool TrySelectQuest(string questId)
        {
            var service = GetService();
            if (service == null)
                return false;

            var success = service.TrySelectQuest(questId);
            if (success)
                GuildNotificationAPI.NotifyQuestSelected();

            return success;
        }

        public static bool TryCancelQuest(string questId)
        {
            var service = GetService();
            if (service == null)
                return false;

            var success = service.TryCancelSelectedQuest(questId);
            if (success)
                GuildNotificationAPI.NotifyQuestCancelled();

            return success;
        }

        public static bool TrySubmitQuest(string questId, out GuildQuestDefinitionAsset quest)
        {
            quest = null;
            var service = GetService();
            if (service == null)
                return false;

            var success = service.TrySubmitQuest(questId, out quest);
            if (success)
                GuildNotificationAPI.NotifyQuestCompleted();

            return success;
        }

        public static bool TryGetQuestTurnInProgress(string questId, out GuildQuestTurnInProgressData data)
        {
            data = null;
            var service = GetService();
            if (service == null)
                return false;

            return service.TryBuildQuestTurnInProgress(questId, out data);
        }

        public static bool CanRankUp(out GuildRankRequirement requirement)
        {
            requirement = null;
            var service = GetService();
            return service != null && service.CanRankUp(out requirement);
        }

        public static bool TryRankUp(out AdventurerRank newRank)
        {
            newRank = AdventurerRank.None;
            var service = GetService();
            if (service == null)
                return false;

            var success = service.TryRankUp(out newRank);
            if (success)
            {
                var save = global::GameState.Instance?.CurrentSave;
                if (save != null)
                    global::SaveSlotsManager.SaveToSlot(global::SaveSlotsManager.GetRuntimeSaveSlotOrAutosave(), save, rememberAsCurrentRuntimeSlot: false);

                GuildNotificationAPI.NotifyRankUp();
            }

            return success;
        }

        public static AdventurerRank GetCurrentRank()
        {
            var service = GetService();
            return service != null ? service.CurrentRank : AdventurerRank.None;
        }

        public static bool TryGetRankUpViewData(out GuildRankUpViewData data)
        {
            data = null;
            var service = GetService();
            return service != null && service.TryBuildRankUpViewData(out data);
        }
    }
}
