using UnityEngine;

namespace UDA2.GameTime
{
    public static class GameTimePhaseResolver
    {
        public const int SpecialNightFrequencyDays = 7;

        // Special night window: 00:00..04:59 (so Dawn from 05:00 is not affected)
        public const int NightRaidStartMinute = 0;
        public const int NightRaidEndMinute = (5 * 60) - 1;

        public static bool IsInNightRaidWindow(int minuteOfDay)
        {
            return minuteOfDay >= NightRaidStartMinute && minuteOfDay <= NightRaidEndMinute;
        }

        /// <summary>
        /// Basic VN-like phases for an icon.
        /// </summary>
        public static TimeOfDayPhase GetTimeOfDayPhase(int minuteOfDay)
        {
            minuteOfDay = Mathf.Clamp(minuteOfDay, 0, 1439);

            // You can tweak these borders later.
            // 05:00-07:59 Dawn
            // 08:00-10:59 Morning
            // 11:00-13:59 Noon
            // 14:00-16:59 Afternoon
            // 17:00-19:59 Evening
            // 20:00-21:59 Dusk
            // 22:00-04:59 Night

            int hour = minuteOfDay / 60;

            if (hour >= 5 && hour <= 7) return TimeOfDayPhase.Dawn;
            if (hour >= 8 && hour <= 10) return TimeOfDayPhase.Morning;
            if (hour >= 11 && hour <= 13) return TimeOfDayPhase.Noon;
            if (hour >= 14 && hour <= 16) return TimeOfDayPhase.Afternoon;
            if (hour >= 17 && hour <= 19) return TimeOfDayPhase.Evening;
            if (hour >= 20 && hour <= 21) return TimeOfDayPhase.Dusk;
            return TimeOfDayPhase.Night;
        }

        /// <summary>
        /// Returns special night type if:
        /// - day is a multiple of 7
        /// - and time is inside night raid window (00:00..04:59)
        /// </summary>
        public static NightSpecialPhase GetNightSpecialPhase(int day, int minuteOfDay)
        {
            day = Mathf.Max(1, day);

            if (!IsInNightRaidWindow(minuteOfDay))
                return NightSpecialPhase.None;

            return GetScheduledSpecialNightForDay(day);
        }

        /// <summary>
        /// Returns which special night is scheduled for the given day.
        /// Note: does NOT check the time window (00:00..04:59).
        /// </summary>
        public static NightSpecialPhase GetScheduledSpecialNightForDay(int day)
        {
            day = Mathf.Max(1, day);

            // Every 7 days (e.g. day 7, 14, 21...) => special night.
            if (day % SpecialNightFrequencyDays != 0)
                return NightSpecialPhase.None;

            // Deterministic cycle between types.
            int cycle = (day / SpecialNightFrequencyDays) % 3;
            switch (cycle)
            {
                case 0: return NightSpecialPhase.CrystalNight;
                case 1: return NightSpecialPhase.LustNight;
                default: return NightSpecialPhase.FullMoon;
            }
        }
    }
}
