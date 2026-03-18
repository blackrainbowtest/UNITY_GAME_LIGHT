using System;

namespace UDA2.SaveSystem.Guild
{
    public static class GuildNotificationAPI
    {
        public const string QuestBoardRefreshedLocalizationKey = "ui_guild_quest_board_refreshed";
        public const string QuestSelectedLocalizationKey = "ui_guild_quest_selected";
        public const string QuestCancelledLocalizationKey = "ui_guild_quest_cancelled";
        public const string QuestCompletedLocalizationKey = "ui_guild_quest_completed";
        public const string RankUpLocalizationKey = "ui_guild_rank_up";

        public static event Action<string> NotificationRequested;

        public static void RequestByLocalizationKey(string localizationKey)
        {
            if (string.IsNullOrWhiteSpace(localizationKey))
                return;

            NotificationRequested?.Invoke(localizationKey);
        }

        internal static void NotifyQuestBoardRefreshed()
        {
            RequestByLocalizationKey(QuestBoardRefreshedLocalizationKey);
        }

        internal static void NotifyQuestSelected()
        {
            RequestByLocalizationKey(QuestSelectedLocalizationKey);
        }

        internal static void NotifyQuestCancelled()
        {
            RequestByLocalizationKey(QuestCancelledLocalizationKey);
        }

        internal static void NotifyQuestCompleted()
        {
            RequestByLocalizationKey(QuestCompletedLocalizationKey);
        }

        internal static void NotifyRankUp()
        {
            RequestByLocalizationKey(RankUpLocalizationKey);
        }
    }
}
