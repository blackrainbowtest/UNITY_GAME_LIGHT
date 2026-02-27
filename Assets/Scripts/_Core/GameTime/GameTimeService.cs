using System;
using System.Collections;
using UnityEngine;

namespace UDA2.GameTime
{
    public sealed class GameTimeService : MonoBehaviour
    {
        public static GameTimeService Instance { get; private set; }

        public event Action<int, int> TimeChanged; // (day, minuteOfDay)

        [Header("Animation")]
        [SerializeField, Min(0.001f)] private float animatedStepSeconds = 0.02f;
        [SerializeField, Min(1)] private int animatedMinutesPerStep = 1;

        private bool initializedFromSave;
        private Coroutine animateRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureCreated()
        {
            if (Instance != null)
                return;

            var existing = FindFirstObjectByType<GameTimeService>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var go = new GameObject("[GameTimeService]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameTimeService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (initializedFromSave)
                return;

            var save = global::GameState.Instance?.CurrentSave;
            if (save == null)
                return;

            EnsureSaveHasTime(save);
            initializedFromSave = true;
            RaiseChanged(save);
        }

        public int GetDay()
        {
            var save = global::GameState.Instance?.CurrentSave;
            if (save?.time == null)
                return 1;
            return Mathf.Max(1, save.time.day);
        }

        public int GetMinuteOfDay()
        {
            var save = global::GameState.Instance?.CurrentSave;
            if (save?.time == null)
                return 0;
            return Mathf.Clamp(save.time.minuteOfDay, 0, 1439);
        }

        public int GetHour() => GetMinuteOfDay() / 60;
        public int GetMinute() => GetMinuteOfDay() % 60;

        public string GetFormattedTime24h()
        {
            return $"{GetHour():00}:{GetMinute():00}";
        }

        public void AddMinutes(int minutes)
        {
            var save = global::GameState.Instance?.CurrentSave;
            if (save == null)
                return;

            EnsureSaveHasTime(save);
            ApplyMinutes(save, minutes);
            RaiseChanged(save);
        }

        public void AddMinutesAnimated(int minutes)
        {
            if (minutes == 0)
                return;

            if (animateRoutine != null)
            {
                StopCoroutine(animateRoutine);
                animateRoutine = null;
            }

            animateRoutine = StartCoroutine(AddMinutesAnimatedRoutine(minutes));
        }

        public void SetTime(int day, int hour24, int minute)
        {
            var save = global::GameState.Instance?.CurrentSave;
            if (save == null)
                return;

            EnsureSaveHasTime(save);

            day = Mathf.Max(1, day);
            hour24 = Mathf.Clamp(hour24, 0, 23);
            minute = Mathf.Clamp(minute, 0, 59);

            save.time.day = day;
            save.time.minuteOfDay = hour24 * 60 + minute;

            RaiseChanged(save);
        }

        public void ConfigureAnimation(float stepSeconds, int minutesPerStep)
        {
            animatedStepSeconds = Mathf.Max(0.001f, stepSeconds);
            animatedMinutesPerStep = Mathf.Max(1, minutesPerStep);
        }

        public void SyncFromCurrentSave()
        {
            if (animateRoutine != null)
            {
                StopCoroutine(animateRoutine);
                animateRoutine = null;
            }

            initializedFromSave = false;

            var save = global::GameState.Instance?.CurrentSave;
            if (save == null)
                return;

            EnsureSaveHasTime(save);
            initializedFromSave = true;
            RaiseChanged(save);
        }

        private IEnumerator AddMinutesAnimatedRoutine(int totalMinutes)
        {
            var save = global::GameState.Instance?.CurrentSave;
            if (save == null)
                yield break;

            EnsureSaveHasTime(save);

            int remaining = totalMinutes;
            int stepMinutes = Mathf.Max(1, animatedMinutesPerStep);
            float stepSeconds = Mathf.Max(0.001f, animatedStepSeconds);

            while (remaining != 0)
            {
                int step;
                if (remaining > 0)
                {
                    step = Mathf.Min(remaining, stepMinutes);
                    remaining -= step;
                }
                else
                {
                    step = -Mathf.Min(-remaining, stepMinutes);
                    remaining -= step;
                }

                ApplyMinutes(save, step);
                RaiseChanged(save);

                yield return new WaitForSeconds(stepSeconds);
            }

            animateRoutine = null;
        }

        private static void EnsureSaveHasTime(SaveData save)
        {
            if (save.time == null)
                save.time = new SaveData.TimeState();

            if (save.time.day <= 0)
                save.time.day = 1;

            save.time.minuteOfDay = Mathf.Clamp(save.time.minuteOfDay, 0, 1439);
        }

        private static void ApplyMinutes(SaveData save, int deltaMinutes)
        {
            int day = Mathf.Max(1, save.time.day);
            int minuteOfDay = Mathf.Clamp(save.time.minuteOfDay, 0, 1439);

            int total = minuteOfDay + deltaMinutes;
            while (total >= 1440)
            {
                total -= 1440;
                day++;
            }

            while (total < 0)
            {
                total += 1440;
                day = Mathf.Max(1, day - 1);
            }

            save.time.day = day;
            save.time.minuteOfDay = Mathf.Clamp(total, 0, 1439);
        }

        private void RaiseChanged(SaveData save)
        {
            var day = Mathf.Max(1, save.time.day);
            var minuteOfDay = Mathf.Clamp(save.time.minuteOfDay, 0, 1439);
            TimeChanged?.Invoke(day, minuteOfDay);
        }
    }
}
