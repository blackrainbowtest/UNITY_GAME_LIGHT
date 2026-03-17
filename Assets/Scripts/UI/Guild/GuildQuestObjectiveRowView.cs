using TMPro;
using UDA2.SaveSystem.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Guild
{
    [DisallowMultipleComponent]
    public sealed class GuildQuestObjectiveRowView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private LocalizedGlobalComponent titleLocalized;
        [SerializeField] private TMP_Text currentText;
        [SerializeField] private TMP_Text requiredText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Color metColor = new Color(0.25f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color unmetColor = new Color(0.95f, 0.3f, 0.3f, 1f);

        private void Awake()
        {
            if (titleLocalized == null && titleText != null)
                titleLocalized = titleText.GetComponent<LocalizedGlobalComponent>();
        }

        public void Render(GuildQuestTurnInObjectiveProgress objective, Sprite icon)
        {
            if (objective == null)
                return;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            ApplyTitle(objective.displayName ?? objective.objectiveId ?? string.Empty);

            var color = objective.IsMet ? metColor : unmetColor;

            if (currentText != null)
            {
                currentText.text = objective.current.ToString();
                currentText.color = color;
            }

            if (requiredText != null)
                requiredText.text = objective.required.ToString();

            if (progressText != null)
            {
                progressText.text = objective.current.ToString() + " / " + objective.required.ToString();
                progressText.color = color;
            }
        }

        private void ApplyTitle(string value)
        {
            if (titleLocalized != null)
            {
                var key = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    titleLocalized.Key = key;
                    titleLocalized.ClearArgs();
                }

                var lang = UDA2.Core.SettingsContext.Current?.language;
                if (string.IsNullOrWhiteSpace(lang))
                    lang = "en";

                var provider = UIStringsProvider.Instance;
                var hasLocalizedValue = provider != null
                    && !string.IsNullOrWhiteSpace(key)
                    && !string.Equals(provider.Get(key, lang), key, System.StringComparison.Ordinal);

                if (hasLocalizedValue)
                    return;
            }

            if (titleText != null)
                titleText.text = value ?? string.Empty;
        }
    }
}
