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
        [SerializeField] private Button clickButton;
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

        private GuildQuestDefinitionAsset questDefinition;
        private string questId;
        private Func<string, bool> acceptHandler;
        private Action<GuildQuestDefinitionAsset> clickHandler;
        private GameObject owningRoot;

        public void SetOwningRoot(GameObject root)
        {
            owningRoot = root;
        }

        private void Awake()
        {
            AutoWireIfNeeded();

            if (clickButton == null)
                clickButton = GetComponent<Button>();

            if (clickButton == null)
                clickButton = FindClickableButtonInChildren();

            if (clickButton != null)
                clickButton.onClick.AddListener(HandleCardClicked);

            if (acceptButton != null)
                acceptButton.onClick.AddListener(HandleAcceptClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void AutoWireIfNeeded()
        {
            titleText ??= FindTextByNameHint("title");
            descriptionText ??= FindTextByNameHint("description", "desc");
            employerNameText ??= FindTextByNameHint("employer", "giver", "name");

            titleLocalized ??= FindLocalizedByNameHint("title");
            descriptionLocalized ??= FindLocalizedByNameHint("description", "desc");
            employerNameLocalized ??= FindLocalizedByNameHint("employer", "giver", "name");

            acceptButton ??= FindButtonByNameHint("accept", "take");
            closeButton ??= FindButtonByNameHint("close", "back");
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

        private Button FindButtonByNameHint(params string[] hints)
        {
            var all = GetComponentsInChildren<Button>(includeInactive: true);
            for (var i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (b == null)
                    continue;

                var n = b.name;
                for (var h = 0; h < hints.Length; h++)
                {
                    if (n.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) >= 0)
                        return b;
                }
            }

            return null;
        }

        private Button FindClickableButtonInChildren()
        {
            var all = GetComponentsInChildren<Button>(includeInactive: true);
            for (var i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (b == null)
                    continue;

                if (ReferenceEquals(b, acceptButton) || ReferenceEquals(b, closeButton))
                    continue;

                return b;
            }

            return null;
        }

        private void OnDestroy()
        {
            if (clickButton != null)
                clickButton.onClick.RemoveListener(HandleCardClicked);

            if (acceptButton != null)
                acceptButton.onClick.RemoveListener(HandleAcceptClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Bind(
            GuildQuestDefinitionAsset quest,
            Func<string, bool> onAccept,
            bool isTaken = false,
            Action<GuildQuestDefinitionAsset> onClick = null)
        {
            questDefinition = quest;
            questId = quest != null ? quest.questId : string.Empty;
            acceptHandler = onAccept;
            clickHandler = onClick;

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

        private void HandleCardClicked()
        {
            if (questDefinition == null)
                return;

            clickHandler?.Invoke(questDefinition);
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
            if (owningRoot != null)
                Destroy(owningRoot);
            else
                Destroy(gameObject);
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
