using UDA2.SaveSystem.Guild;
using UnityEngine;

namespace UDA2.GameTime
{
    public sealed class GuildTimeSyncBridge : MonoBehaviour
    {
        private static bool created;
        private bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCreated()
        {
            if (created)
                return;

            created = true;
            var go = new GameObject("[GuildTimeSyncBridge]");
            DontDestroyOnLoad(go);
            go.AddComponent<GuildTimeSyncBridge>();
        }

        private void Update()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            TryUnsubscribe();
        }

        private void OnDestroy()
        {
            TryUnsubscribe();
        }

        private void TrySubscribe()
        {
            if (subscribed)
                return;

            var timeService = GameTimeService.Instance;
            if (timeService == null)
                return;

            timeService.TimeChanged += HandleTimeChanged;
            subscribed = true;

            HandleTimeChanged(timeService.GetDay(), timeService.GetMinuteOfDay());
        }

        private void TryUnsubscribe()
        {
            if (!subscribed)
                return;

            var timeService = GameTimeService.Instance;
            if (timeService != null)
                timeService.TimeChanged -= HandleTimeChanged;

            subscribed = false;
        }

        private void HandleTimeChanged(int day, int minuteOfDay)
        {
            GuildRuntimeAPI.HandleTimeChanged(day, minuteOfDay);
        }
    }
}
