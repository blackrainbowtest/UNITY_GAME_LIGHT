using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Battle.UI
{
    public sealed class BattleResultAchievementItemView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private LocalizedGlobalComponent titleLocalized;
        [SerializeField] private LocalizedGlobalComponent descriptionLocalized;

        private void Awake()
        {
            AutoWireIfMissing();
        }

        private void OnValidate()
        {
            AutoWireIfMissing();
        }

        public void Bind(BattleAchievementCatalogAsset.AchievementDefinition definition)
        {
            AutoWireIfMissing();

            if (definition == null)
            {
                BindFallback(string.Empty, string.Empty, string.Empty, null);
                return;
            }

            var title = ResolveText(definition.titleLocalizationKey, definition.id);
            var description = ResolveText(definition.descriptionLocalizationKey, string.Empty);

            // First apply plain fallback text/icon, then let localization override text if keys exist.
            BindFallback(definition.id, title, description, definition.icon);

            ApplyLocalized(titleLocalized, definition.titleLocalizationKey, title);
            ApplyLocalized(descriptionLocalized, definition.descriptionLocalizationKey, description);
        }

        public void BindFallback(string id, string title, string description, Sprite icon)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = iconImage.sprite != null;
            }

            if (titleText != null && titleLocalized == null)
                titleText.text = !string.IsNullOrWhiteSpace(title) ? title : (id ?? string.Empty);

            if (descriptionText != null && descriptionLocalized == null)
                descriptionText.text = description ?? string.Empty;
        }

        private void AutoWireIfMissing()
        {
            if (iconImage == null)
                iconImage = FindImageByName("icon", "image", "sprite");

            if (titleText == null)
                titleText = FindTextByName("title", "name");

            if (descriptionText == null)
                descriptionText = FindTextByName("description", "desc", "body");

            if (titleLocalized == null && titleText != null)
                titleLocalized = titleText.GetComponent<LocalizedGlobalComponent>();

            if (descriptionLocalized == null && descriptionText != null)
                descriptionLocalized = descriptionText.GetComponent<LocalizedGlobalComponent>();
        }

        private Image FindImageByName(params string[] tokens)
        {
            var images = GetComponentsInChildren<Image>(includeInactive: true);
            for (int i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null || image == GetComponent<Image>())
                    continue;

                var name = image.name;
                for (int j = 0; j < tokens.Length; j++)
                {
                    if (!string.IsNullOrWhiteSpace(tokens[j])
                        && name.IndexOf(tokens[j], StringComparison.OrdinalIgnoreCase) >= 0)
                        return image;
                }
            }

            return null;
        }

        private TMP_Text FindTextByName(params string[] tokens)
        {
            var texts = GetComponentsInChildren<TMP_Text>(includeInactive: true);
            for (int i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null)
                    continue;

                var name = text.name;
                for (int j = 0; j < tokens.Length; j++)
                {
                    if (!string.IsNullOrWhiteSpace(tokens[j])
                        && name.IndexOf(tokens[j], StringComparison.OrdinalIgnoreCase) >= 0)
                        return text;
                }
            }

            return null;
        }

        private static void ApplyLocalized(LocalizedGlobalComponent localized, string key, string fallback)
        {
            if (localized == null || string.IsNullOrWhiteSpace(key))
                return;

            localized.Key = key.Trim();
            localized.ClearArgs();
            localized.UpdateText();

            var target = localized.GetComponent<TMP_Text>();
            if (target == null)
                return;

            if (string.IsNullOrWhiteSpace(target.text) || string.Equals(target.text, localized.Key, StringComparison.OrdinalIgnoreCase))
                target.text = fallback ?? string.Empty;
        }

        private static string ResolveText(string localizationKey, string fallback)
        {
            if (string.IsNullOrWhiteSpace(localizationKey))
                return fallback ?? string.Empty;

            var key = localizationKey.Trim();
            var localized = UDA2.Core.LocalizationManager.Get(key);
            if (string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.OrdinalIgnoreCase))
                return fallback ?? key;

            return localized;
        }
    }
}
