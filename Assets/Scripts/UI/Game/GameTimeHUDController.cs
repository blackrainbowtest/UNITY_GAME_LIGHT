using TMPro;
using UnityEngine;
using UDA2.GameTime;

namespace UDA2.UI.Game
{
    public sealed class GameTimeHUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text timeText;

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

        private void HandleTimeChanged(int day, int minuteOfDay)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (dayText != null)
                dayText.text = $"{GameTimeAPI.Day}";

            if (timeText != null)
                timeText.text = GameTimeAPI.Time24h;
        }
    }
}
