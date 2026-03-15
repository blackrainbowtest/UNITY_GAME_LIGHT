using System;
using TMPro;
using UDA2.SaveSystem.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Guild
{
    [DisallowMultipleComponent]
    public sealed class GuildQuestBoardCardView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button clickButton;
        [SerializeField] private Image questImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private LocalizedGlobalComponent titleLocalized;

        private GuildQuestDefinitionAsset quest;
        private Action<GuildQuestDefinitionAsset> onClick;

        private void Awake()
        {
            if (clickButton == null)
                clickButton = GetComponent<Button>();

            if (clickButton != null)
                clickButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (clickButton != null)
                clickButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(GuildQuestDefinitionAsset questDefinition, Action<GuildQuestDefinitionAsset> clickHandler, bool isTaken = false)
        {
            quest = questDefinition;
            onClick = clickHandler;

            if (questImage != null)
            {
                if (quest != null)
                    questImage.sprite = isTaken && quest.questTakenImage != null ? quest.questTakenImage : quest.questImage;
                else
                    questImage.sprite = null;

                questImage.enabled = questImage.sprite != null;
            }

            ApplyTitle(quest != null ? quest.titleLocalizationKey : string.Empty);
        }

        private void HandleClick()
        {
            if (quest == null)
                return;

            onClick?.Invoke(quest);
        }

        private void ApplyTitle(string key)
        {
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

            titleText.text = UDA2.Core.LocalizationManager.Get(key);
        }
    }
}
