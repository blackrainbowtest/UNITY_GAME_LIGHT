using Game.Progression;
using TMPro;
using UnityEngine;
using UDA2.Core;

namespace UDA2.UI.Game
{
    public sealed class ProfileExperienceView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text expText;
        [SerializeField] private TMP_Text expToNextText;
        [SerializeField] private StatBarView expBar;

        [Header("Localization")]
        [SerializeField] private bool useLocalizationKeys = true;
        [SerializeField] private string levelKey = "lvl";
        [SerializeField] private string expKey = "exp";
        [SerializeField] private string expToNextKey = "next";
        [SerializeField] private bool refreshOnLanguageChange = true;

        [Header("Formatting")]
        [SerializeField] private string levelPrefix = "Lv ";
        [SerializeField] private string expPrefix = "EXP ";
        [SerializeField] private string expToNextPrefix = "Next ";
        [SerializeField] private bool showCapAsMaxedBar = true;

        [Header("Behavior")]
        [SerializeField] private bool refreshOnEnable = true;

        private void OnEnable()
        {
            if (refreshOnLanguageChange)
                SettingsContext.OnLanguageChanged += HandleLanguageChanged;

            if (refreshOnEnable)
                RefreshFromCurrentSave();
        }

        private void OnDisable()
        {
            if (refreshOnLanguageChange)
                SettingsContext.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(string _)
        {
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
            int remainingToNext = expToNext > 0 ? Mathf.Max(0, expToNext - exp) : 0;

            if (levelText != null)
            {
                if (useLocalizationKeys && !string.IsNullOrWhiteSpace(levelKey))
                    SetLocalizedFormatted(levelText, levelKey, level);
                else
                    levelText.text = $"{levelPrefix}{level}";
            }

            if (expText != null)
            {
                if (useLocalizationKeys && !string.IsNullOrWhiteSpace(expKey))
                {
                    if (expToNext > 0)
                        SetLocalizedFormatted(expText, expKey, exp, expToNext);
                    else
                        SetLocalizedFormatted(expText, expKey, exp, "—");
                }
                else
                {
                    if (expToNext > 0)
                        expText.text = $"{expPrefix}{exp}/{expToNext}";
                    else
                        expText.text = $"{expPrefix}{exp}";
                }
            }

            if (expToNextText != null)
            {
                if (useLocalizationKeys && !string.IsNullOrWhiteSpace(expToNextKey))
                {
                    if (expToNext > 0)
                        SetLocalizedFormatted(expToNextText, expToNextKey, remainingToNext);
                    else
                        SetLocalizedFormatted(expToNextText, expToNextKey, "—");
                }
                else
                {
                    expToNextText.text = expToNext > 0 ? $"{expToNextPrefix}{expToNext}" : $"{expToNextPrefix}—";
                }
            }

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
            if (levelText != null)
            {
                if (useLocalizationKeys && !string.IsNullOrWhiteSpace(levelKey))
                    SetLocalizedFormatted(levelText, levelKey, "—");
                else
                    levelText.text = $"{levelPrefix}—";
            }

            if (expText != null)
            {
                if (useLocalizationKeys && !string.IsNullOrWhiteSpace(expKey))
                    SetLocalizedFormatted(expText, expKey, "—", "—");
                else
                    expText.text = $"{expPrefix}—";
            }

            if (expToNextText != null)
            {
                if (useLocalizationKeys && !string.IsNullOrWhiteSpace(expToNextKey))
                    SetLocalizedFormatted(expToNextText, expToNextKey, "—");
                else
                    expToNextText.text = $"{expToNextPrefix}—";
            }

            if (expBar != null)
            {
                expBar.SetNormalized(0f);
                expBar.SetValue(0, 0);
            }
        }

        private static void SetLocalizedFormatted(TMP_Text target, string key, params object[] args)
        {
            if (target == null || string.IsNullOrWhiteSpace(key))
                return;

            var localized = target.GetComponent<LocalizedGlobalComponent>();
            if (localized != null)
            {
                localized.Key = key;
                localized.SetFormatArgs(args);
                localized.UpdateText();
                return;
            }

            var template = LocalizationManager.Get(key);
            try
            {
                target.text = (args != null && args.Length > 0) ? string.Format(template, args) : template;
            }
            catch
            {
                target.text = template;
            }
        }
    }
}
