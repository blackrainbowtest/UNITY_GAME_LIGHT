using UnityEngine;

namespace UDA2.GameTime
{
    /// <summary>
    /// Lightweight static API for gameplay code.
    /// Backed by GameTimeService + SaveData.time.
    /// </summary>
    public static class GameTimeAPI
    {
        public static int Day => GameTimeService.Instance != null ? GameTimeService.Instance.GetDay() : 1;
        public static int Hour24 => GameTimeService.Instance != null ? GameTimeService.Instance.GetHour() : 0;
        public static int Minute => GameTimeService.Instance != null ? GameTimeService.Instance.GetMinute() : 0;

        public static int MinuteOfDay => GameTimeService.Instance != null ? GameTimeService.Instance.GetMinuteOfDay() : 0;

        public static string Time24h => GameTimeService.Instance != null ? GameTimeService.Instance.GetFormattedTime24h() : "00:00";

        public static TimeOfDayPhase TimeOfDayPhase => GameTimePhaseResolver.GetTimeOfDayPhase(MinuteOfDay);

        public static NightSpecialPhase NightSpecialPhase => GameTimePhaseResolver.GetNightSpecialPhase(Day, MinuteOfDay);

        public static bool IsNightRaidWindow => GameTimePhaseResolver.IsInNightRaidWindow(MinuteOfDay);

        public static void AddMinutes(int minutes)
        {
            GameTimeService.Instance?.AddMinutes(minutes);
        }

        public static void AddHours(int hours)
        {
            AddMinutes(hours * 60);
        }

        /// <summary>
        /// Adds time step-by-step so UI updates look smooth.
        /// </summary>
        public static void AddMinutesAnimated(int minutes)
        {
            GameTimeService.Instance?.AddMinutesAnimated(minutes);
        }

        public static void SetTime(int day, int hour24, int minute)
        {
            GameTimeService.Instance?.SetTime(day, hour24, minute);
        }

        public static void ConfigureAnimation(float stepSeconds, int minutesPerStep)
        {
            GameTimeService.Instance?.ConfigureAnimation(stepSeconds, minutesPerStep);
        }
    }
}
