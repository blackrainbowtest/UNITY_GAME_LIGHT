using Game.Progression;
using TMPro;
using UnityEngine;

namespace UDA2.UI.Game
{
    public sealed class ProfileExperienceView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text expText;
        [SerializeField] private TMP_Text expToNextText;
        [SerializeField] private StatBarView expBar;

        [Header("Formatting")]
        [SerializeField] private string levelPrefix = "Lv ";
        [SerializeField] private string expPrefix = "EXP ";
        [SerializeField] private string expToNextPrefix = "Next ";
        [SerializeField] private bool showCapAsMaxedBar = true;

        [Header("Behavior")]
        [SerializeField] private bool refreshOnEnable = true;

        private void OnEnable()
        {
            if (refreshOnEnable)
                RefreshFromCurrentSave();
        }

        public void RefreshFromCurrentSave()
        {
            var save = global::GameState.Instance != null ? global::GameState.Instance.CurrentSave : null;
            Refresh(save);
        }

        public void Refresh(SaveData save)
        {
            var player = save != null ? save.player : null;

            if (player == null)
            {
                SetNoData();
                return;
            }

            int level = player.level;
            int exp = player.exp;
            PlayerExperience.Normalize(ref level, ref exp);

            int expToNext = PlayerExperience.GetExpToNextLevel(level);

            if (levelText != null)
                levelText.text = $"{levelPrefix}{level}";

            if (expText != null)
            {
                if (expToNext > 0)
                    expText.text = $"{expPrefix}{exp}/{expToNext}";
                else
                    expText.text = $"{expPrefix}{exp}";
            }

            if (expToNextText != null)
                expToNextText.text = expToNext > 0 ? $"{expToNextPrefix}{expToNext}" : $"{expToNextPrefix}—";

            if (expBar != null)
            {
                if (expToNext <= 0)
                {
                    expBar.SetNormalized(showCapAsMaxedBar ? 1f : 0f);
                    expBar.SetValue(0, 0);
                }
                else
                {
                    expBar.SetNormalized(expToNext <= 0 ? 0f : Mathf.Clamp01(exp / (float)expToNext));
                    expBar.SetValue(exp, expToNext);
                }
            }
        }

        private void SetNoData()
        {
            if (levelText != null) levelText.text = $"{levelPrefix}—";
            if (expText != null) expText.text = $"{expPrefix}—";
            if (expToNextText != null) expToNextText.text = $"{expToNextPrefix}—";
            if (expBar != null)
            {
                expBar.SetNormalized(0f);
                expBar.SetValue(0, 0);
            }
        }
    }
}
