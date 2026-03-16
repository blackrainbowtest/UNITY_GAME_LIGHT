using System;
using TMPro;
using UDA2.SaveSystem.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Guild
{
    [DisallowMultipleComponent]
    public sealed class GuildQuestBoardSlotView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button clickButton;
        [SerializeField] private Image questImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private LocalizedGlobalComponent titleLocalized;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private LocalizedGlobalComponent descriptionLocalized;
        [SerializeField] private TMP_Text employerNameText;
        [SerializeField] private LocalizedGlobalComponent employerNameLocalized;
        [SerializeField] private TMP_Text questIdText;

        private GuildQuestDefinitionAsset quest;
        private Action<GuildQuestDefinitionAsset> clickHandler;

        private void Awake()
        {
            AutoWireIfNeeded();

            if (clickButton == null)
                clickButton = GetComponent<Button>();

            if (clickButton == null)
                clickButton = GetComponentInChildren<Button>(includeInactive: true);

            if (clickButton != null)
                clickButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (clickButton != null)
                clickButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(GuildQuestDefinitionAsset questDefinition, bool isTaken, Action<GuildQuestDefinitionAsset> onClick)
        {
            quest = questDefinition;
            clickHandler = onClick;

            if (questImage != null)
            {
                if (quest != null)
                    questImage.sprite = isTaken && quest.questTakenImage != null ? quest.questTakenImage : quest.questImage;
                else
                    questImage.sprite = null;

                questImage.enabled = questImage.sprite != null;
            }

            ApplyLocalized(titleLocalized, titleText, quest != null ? quest.titleLocalizationKey : string.Empty);
            ApplyLocalized(descriptionLocalized, descriptionText, quest != null ? quest.descriptionLocalizationKey : string.Empty);
            ApplyLocalized(employerNameLocalized, employerNameText, quest != null ? quest.questGiverNameLocalizationKey : string.Empty);

            if (questIdText != null)
                questIdText.text = quest != null ? (quest.questId ?? string.Empty) : string.Empty;
        }

        private void HandleClick()
        {
            if (quest == null)
                return;

            clickHandler?.Invoke(quest);
        }

        private void AutoWireIfNeeded()
        {
            titleText ??= FindTextByNameHint("title");
            descriptionText ??= FindTextByNameHint("description", "desc");
            employerNameText ??= FindTextByNameHint("employer", "giver", "name");
            questIdText ??= FindTextByNameHint("questid", "id");

            titleLocalized ??= FindLocalizedByNameHint("title");
            descriptionLocalized ??= FindLocalizedByNameHint("description", "desc");
            employerNameLocalized ??= FindLocalizedByNameHint("employer", "giver", "name");

            if (questImage == null)
                questImage = FindImageByNameHint("quest", "icon", "image");
        }

        private TMP_Text FindTextByNameHint(params string[] hints)
        {
            var all = GetComponentsInChildren<TMP_Text>(includeInactive: true);
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null)
                    continue;

                var n = t.name;
                for (var h = 0; h < hints.Length; h++)
                {
                    if (n.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) >= 0)
                        return t;
                }
            }

            return null;
        }

        private LocalizedGlobalComponent FindLocalizedByNameHint(params string[] hints)
        {
            var all = GetComponentsInChildren<LocalizedGlobalComponent>(includeInactive: true);
            for (var i = 0; i < all.Length; i++)
            {
                var l = all[i];
                if (l == null)
                    continue;

                var n = l.name;
                for (var h = 0; h < hints.Length; h++)
                {
                    if (n.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) >= 0)
                        return l;
                }
            }

            return null;
        }

        private Image FindImageByNameHint(params string[] hints)
        {
            var all = GetComponentsInChildren<Image>(includeInactive: true);
            for (var i = 0; i < all.Length; i++)
            {
                var img = all[i];
                if (img == null)
                    continue;

                var n = img.name;
                for (var h = 0; h < hints.Length; h++)
                {
                    if (n.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) >= 0)
                        return img;
                }
            }

            return null;
        }

        private static void ApplyLocalized(LocalizedGlobalComponent localized, TMP_Text text, string key)
        {
            if (localized != null)
            {
                localized.Key = key;
                localized.ClearArgs();

                // LocalizedGlobalComponent updates TMP text by itself.
                // Do not force overwrite here, otherwise missing-provider fallback can display raw keys.
                return;
            }

            if (text == null)
                return;

            if (string.IsNullOrWhiteSpace(key))
            {
                text.text = string.Empty;
                return;
            }

            var lang = UDA2.Core.SettingsContext.Current?.language;
            if (string.IsNullOrWhiteSpace(lang))
                lang = "en";

            var provider = UIStringsProvider.Instance;
            if (provider != null)
                text.text = provider.Get(key, lang);
            else
                text.text = UDA2.Core.LocalizationManager.Get(key);
        }
    }
}
