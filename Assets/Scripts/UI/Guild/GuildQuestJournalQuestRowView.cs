using System;
using TMPro;
using UDA2.SaveSystem.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Guild
{
    [DisallowMultipleComponent]
    public sealed class GuildQuestJournalQuestRowView : MonoBehaviour
    {
        [SerializeField] private Button rowButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private LocalizedGlobalComponent titleLocalized;

        private Action clickHandler;

        private void Awake()
        {
            if (rowButton == null)
                rowButton = GetComponent<Button>();

            if (rowButton != null)
                rowButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (rowButton != null)
                rowButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(GuildQuestDefinitionAsset quest, Action onClick)
        {
            clickHandler = onClick;
            ApplyTitle(quest != null ? quest.titleLocalizationKey : string.Empty);
            if (rowButton != null)
                rowButton.interactable = quest != null;
        }

        private void HandleClick()
        {
            clickHandler?.Invoke();
        }

        private void ApplyTitle(string key)
        {
            if (titleLocalized == null && titleText != null)
                titleLocalized = titleText.GetComponent<LocalizedGlobalComponent>();

            if (titleLocalized != null)
            {
                titleLocalized.Key = key;
                titleLocalized.ClearArgs();
                return;
            }

            if (titleText == null)
                return;

            if (string.IsNullOrWhiteSpace(key))
            {
                titleText.text = string.Empty;
                return;
            }

            var lang = UDA2.Core.SettingsContext.Current?.language;
            if (string.IsNullOrWhiteSpace(lang))
                lang = "en";

            var provider = UIStringsProvider.Instance;
            if (provider != null)
                titleText.text = provider.Get(key, lang);
            else
                titleText.text = key;
        }
    }
}
