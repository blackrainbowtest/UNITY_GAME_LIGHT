using System;
using TMPro;
using UDA2.SaveSystem.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Guild
{
    [DisallowMultipleComponent]
    public sealed class GuildQuestDetailsView : MonoBehaviour
    {
        [Header("Quest")]
        [SerializeField] private Image questImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private LocalizedGlobalComponent titleLocalized;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private LocalizedGlobalComponent descriptionLocalized;

        [Header("Employer")]
        [SerializeField] private Image employerImage;
        [SerializeField] private TMP_Text employerNameText;
        [SerializeField] private LocalizedGlobalComponent employerNameLocalized;

        [Header("Actions")]
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button closeButton;

        private string questId;
        private Func<string, bool> acceptHandler;

        private void Awake()
        {
            if (acceptButton != null)
                acceptButton.onClick.AddListener(HandleAcceptClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (acceptButton != null)
                acceptButton.onClick.RemoveListener(HandleAcceptClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Bind(GuildQuestDefinitionAsset quest, Func<string, bool> onAccept, bool isTaken = false)
        {
            questId = quest != null ? quest.questId : string.Empty;
            acceptHandler = onAccept;

            if (questImage != null)
            {
                if (quest != null)
                    questImage.sprite = isTaken && quest.questTakenImage != null ? quest.questTakenImage : quest.questImage;
                else
                    questImage.sprite = null;

                questImage.enabled = questImage.sprite != null;
            }

            if (employerImage != null)
            {
                employerImage.sprite = quest != null ? quest.questGiverImage : null;
                employerImage.enabled = employerImage.sprite != null;
            }

            ApplyLocalized(titleLocalized, titleText, quest != null ? quest.titleLocalizationKey : string.Empty);
            ApplyLocalized(descriptionLocalized, descriptionText, quest != null ? quest.descriptionLocalizationKey : string.Empty);
            ApplyLocalized(employerNameLocalized, employerNameText, quest != null ? quest.questGiverNameLocalizationKey : string.Empty);

            if (acceptButton != null)
                acceptButton.gameObject.SetActive(!isTaken);
        }

        private void HandleAcceptClicked()
        {
            if (string.IsNullOrWhiteSpace(questId))
                return;

            var accepted = acceptHandler == null || acceptHandler.Invoke(questId);
            if (accepted)
                Close();
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        private static void ApplyLocalized(LocalizedGlobalComponent localized, TMP_Text text, string key)
        {
            if (localized != null)
            {
                localized.Key = key;
                localized.ClearArgs();
                return;
            }

            if (text == null)
                return;

            if (string.IsNullOrWhiteSpace(key))
            {
                text.text = string.Empty;
                return;
            }

            text.text = UDA2.Core.LocalizationManager.Get(key);
        }
    }
}
