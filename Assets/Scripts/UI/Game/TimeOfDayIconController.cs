using UnityEngine;
using UnityEngine.UI;
using UDA2.GameTime;

namespace UDA2.UI.Game
{
    public sealed class TimeOfDayIconController : MonoBehaviour
    {
        [SerializeField] private Image targetImage;

        [Header("Normal Phases")]
        [SerializeField] private Sprite dawn;
        [SerializeField] private Sprite morning;
        [SerializeField] private Sprite noon;
        [SerializeField] private Sprite afternoon;
        [SerializeField] private Sprite evening;
        [SerializeField] private Sprite dusk;
        [SerializeField] private Sprite night;

        [Header("Special Nights (00:00..04:59)")]
        [SerializeField] private Sprite crystalNight;
        [SerializeField] private Sprite lustNight;
        [SerializeField] private Sprite fullMoon;

        private void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (GameTimeService.Instance != null)
                GameTimeService.Instance.TimeChanged += HandleTimeChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (GameTimeService.Instance != null)
                GameTimeService.Instance.TimeChanged -= HandleTimeChanged;
        }

        private void HandleTimeChanged(int dayValue, int minuteOfDay)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (targetImage == null)
                return;

            int dayValue = GameTimeAPI.Day;
            int minuteOfDay = GameTimeAPI.MinuteOfDay;

            var special = GameTimePhaseResolver.GetNightSpecialPhase(dayValue, minuteOfDay);
            if (special != NightSpecialPhase.None)
            {
                var s = ResolveSpecialSprite(special);
                if (s != null)
                {
                    targetImage.sprite = s;
                    return;
                }
            }

            var phase = GameTimePhaseResolver.GetTimeOfDayPhase(minuteOfDay);
            var sprite = ResolvePhaseSprite(phase);
            if (sprite != null)
                targetImage.sprite = sprite;
        }

        private Sprite ResolveSpecialSprite(NightSpecialPhase phase)
        {
            switch (phase)
            {
                case NightSpecialPhase.CrystalNight: return crystalNight;
                case NightSpecialPhase.LustNight: return lustNight;
                case NightSpecialPhase.FullMoon: return fullMoon;
                default: return null;
            }
        }

        private Sprite ResolvePhaseSprite(TimeOfDayPhase phase)
        {
            switch (phase)
            {
                case TimeOfDayPhase.Dawn: return dawn;
                case TimeOfDayPhase.Morning: return morning;
                case TimeOfDayPhase.Noon: return noon;
                case TimeOfDayPhase.Afternoon: return afternoon;
                case TimeOfDayPhase.Evening: return evening;
                case TimeOfDayPhase.Dusk: return dusk;
                default: return night;
            }
        }
    }
}
