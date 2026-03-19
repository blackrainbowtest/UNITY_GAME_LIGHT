using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UDA2.SaveSystem.Guild;
using UnityEngine;
using UnityEngine.Serialization;
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
        [SerializeField] private TMP_Text acceptButtonText;
        [SerializeField] private string acceptButtonLabel = "Accept";
        [SerializeField] private string submitButtonLabel = "Submit";

        [Header("Requirements (Scroll View)")]
        [Tooltip("Content transform from Scroll View where requirement rows will be spawned.")]
        [FormerlySerializedAs("objectivesContentRoot")]
        [SerializeField] private Transform requirementsContentRoot;
        [Tooltip("Disabled row template placed inside Scroll View Content.")]
        [FormerlySerializedAs("objectiveRowPrefab")]
        [FormerlySerializedAs("objectiveRowTemplate")]
        [SerializeField] private GuildQuestObjectiveRowView requirementRowTemplate;
        [Tooltip("Optional item database used to resolve item names/icons in objective rows.")]
        [SerializeField] private UnityEngine.Object itemDatabase;
        [SerializeField] private Sprite goldObjectiveIcon;

        private GuildQuestDefinitionAsset questDefinition;
        private string questId;
        private Func<string, bool> acceptHandler;
        private Func<string, bool> submitHandler;
        private Action<GuildQuestDefinitionAsset> clickHandler;
        private GameObject owningRoot;
        private bool isTakenQuest;
        private bool allowActions = true;
        private readonly List<GuildQuestObjectiveRowView> spawnedObjectiveRows = new List<GuildQuestObjectiveRowView>();

        public void SetOwningRoot(GameObject root)
        {
            owningRoot = root;
        }

        private void Awake()
        {
            AutoWireIfNeeded();
            ResolveObjectiveTemplateIfNeeded();

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

            ClearObjectiveRows();
        }

        public void Bind(
            GuildQuestDefinitionAsset quest,
            Func<string, bool> onAccept,
            Func<string, bool> onSubmit,
            bool isTaken = false,
            Action<GuildQuestDefinitionAsset> onClick = null,
            bool allowActions = true)
        {
            questDefinition = quest;
            questId = quest != null ? quest.questId : string.Empty;
            acceptHandler = onAccept;
            submitHandler = onSubmit;
            clickHandler = onClick;
            isTakenQuest = isTaken;
            this.allowActions = allowActions;

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

            RefreshObjectivesAndActions();
        }

        private void HandleCardClicked()
        {
            if (questDefinition == null)
                return;

            clickHandler?.Invoke(questDefinition);
        }

        private void HandleAcceptClicked()
        {
            if (!allowActions)
                return;

            if (string.IsNullOrWhiteSpace(questId))
                return;

            bool success;
            if (isTakenQuest)
                success = submitHandler != null && submitHandler.Invoke(questId);
            else
                success = acceptHandler == null || acceptHandler.Invoke(questId);

            if (success)
            {
                Close();
                return;
            }

            // Failed submit/accept may mean requirements changed; refresh objective state.
            RefreshObjectivesAndActions();
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

        private void RefreshObjectivesAndActions()
        {
            if (!GuildRuntimeAPI.TryGetQuestTurnInProgress(questId, out var progress) || progress == null)
            {
                ClearObjectiveRows();
                ApplyAcceptButtonState(canSubmit: !isTakenQuest);
                return;
            }

            RebuildObjectiveRows(progress.objectives);
            ApplyAcceptButtonState(canSubmit: progress.canSubmit || !isTakenQuest);
        }

        private void ApplyAcceptButtonState(bool canSubmit)
        {
            if (acceptButton != null)
            {
                acceptButton.gameObject.SetActive(questDefinition != null && allowActions);
                acceptButton.interactable = !isTakenQuest || canSubmit;
            }

            if (acceptButtonText != null)
                acceptButtonText.text = isTakenQuest ? submitButtonLabel : acceptButtonLabel;
        }

        private void RebuildObjectiveRows(IReadOnlyList<GuildQuestTurnInObjectiveProgress> objectives)
        {
            ClearObjectiveRows();
            ResolveObjectiveTemplateIfNeeded();

            if (requirementsContentRoot == null || objectives == null)
                return;

            var prototype = ResolveRequirementTemplate();
            if (prototype == null)
                return;

            for (var i = 0; i < objectives.Count; i++)
            {
                var objective = objectives[i];
                if (objective == null)
                    continue;

                if (objective.required <= 0)
                    continue;

                var row = Instantiate(prototype, requirementsContentRoot, false);
                row.gameObject.SetActive(true);

                if (objective.type == GuildQuestObjectiveType.Item)
                    objective.displayName = ResolveItemDisplayName(objective.objectiveId, objective.displayName);

                row.Render(objective, ResolveObjectiveIcon(objective));
                spawnedObjectiveRows.Add(row);
            }
        }

        private void ClearObjectiveRows()
        {
            for (var i = 0; i < spawnedObjectiveRows.Count; i++)
            {
                if (spawnedObjectiveRows[i] != null)
                    Destroy(spawnedObjectiveRows[i].gameObject);
            }

            spawnedObjectiveRows.Clear();
        }

        private GuildQuestObjectiveRowView ResolveRequirementTemplate()
        {
            return requirementRowTemplate;
        }

        private void ResolveObjectiveTemplateIfNeeded()
        {
            if (requirementRowTemplate != null || requirementsContentRoot == null)
                return;

            requirementRowTemplate = requirementsContentRoot.GetComponentInChildren<GuildQuestObjectiveRowView>(includeInactive: true);
            if (requirementRowTemplate != null)
                requirementRowTemplate.gameObject.SetActive(false);
        }

        private Sprite ResolveObjectiveIcon(GuildQuestTurnInObjectiveProgress objective)
        {
            if (objective == null)
                return null;

            if (objective.type == GuildQuestObjectiveType.Gold)
                return goldObjectiveIcon;

            if (objective.type == GuildQuestObjectiveType.MobKill)
                return ResolveSpriteByMember(objective.sourceObject, "icon", "Icon");

            return ResolveItemIcon(objective.objectiveId);
        }

        private string ResolveItemDisplayName(string itemId, string fallback)
        {
            var normalizedId = NormalizeItemId(itemId);
            var baseFallback = !string.IsNullOrWhiteSpace(fallback)
                ? fallback
                : (!string.IsNullOrWhiteSpace(normalizedId) ? normalizedId : itemId);

            var def = ResolveItemDefinition(normalizedId);

            // Preferred: localize by ItemDefinition.DisplayNameKey (e.g. item.stick.name).
            var displayNameKey = ResolveStringByMember(def, "DisplayNameKey", "displayNameKey");
            var localizedFromKey = TryGetLocalizedString(displayNameKey);
            if (!string.IsNullOrWhiteSpace(localizedFromKey))
                return localizedFromKey;

            // Secondary: localize by conventional key from id.
            var conventionalKey = BuildItemNameKeyFromId(normalizedId);
            var localizedFromId = TryGetLocalizedString(conventionalKey);
            if (!string.IsNullOrWhiteSpace(localizedFromId))
                return localizedFromId;

            // Fallback: plain display name from definition.
            var fromDef = ResolveStringByMember(def, "DisplayName", "Name", "displayName", "name", "id", "Id");
            if (!string.IsNullOrWhiteSpace(fromDef))
                return fromDef;

            return baseFallback ?? string.Empty;
        }

        private Sprite ResolveItemIcon(string itemId)
        {
            var def = ResolveItemDefinition(itemId);
            return ResolveSpriteByMember(def, "Icon", "icon");
        }

        private object ResolveItemDefinition(string itemId)
        {
            if (itemDatabase == null || string.IsNullOrWhiteSpace(itemId))
                return null;

            try
            {
                var dbType = itemDatabase.GetType();
                var getById = dbType.GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public);
                if (getById == null)
                    return null;

                var trimmed = itemId.Trim();
                var definition = getById.Invoke(itemDatabase, new object[] { trimmed });
                if (definition != null)
                    return definition;

                // Compatibility: some configs may still store ids like "item_stick".
                var normalized = NormalizeItemId(trimmed);
                if (!string.Equals(normalized, trimmed, StringComparison.OrdinalIgnoreCase))
                    return getById.Invoke(itemDatabase, new object[] { normalized });

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeItemId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            var id = raw.Trim();
            if (id.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
                id = id.Substring("item_".Length);

            return id;
        }

        private static string BuildItemNameKeyFromId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            return "item." + itemId.Trim().Replace('_', '.') + ".name";
        }

        private static string TryGetLocalizedString(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var lang = UDA2.Core.SettingsContext.Current?.language;
            if (string.IsNullOrWhiteSpace(lang))
                lang = "en";

            var provider = UIStringsProvider.Instance;
            if (provider == null)
                return null;

            var localized = provider.Get(key.Trim(), lang);
            return string.Equals(localized, key, StringComparison.Ordinal) ? null : localized;
        }

        private static Sprite ResolveSpriteByMember(object target, params string[] memberNames)
        {
            if (target == null || memberNames == null)
                return null;

            var type = target.GetType();
            for (var i = 0; i < memberNames.Length; i++)
            {
                var name = memberNames[i];
                var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (prop != null && prop.GetValue(target) is Sprite propSprite)
                    return propSprite;

                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
                if (field != null && field.GetValue(target) is Sprite fieldSprite)
                    return fieldSprite;
            }

            return null;
        }

        private static string ResolveStringByMember(object target, params string[] memberNames)
        {
            if (target == null || memberNames == null)
                return null;

            var type = target.GetType();
            for (var i = 0; i < memberNames.Length; i++)
            {
                var name = memberNames[i];
                var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (prop != null && prop.GetValue(target) is string propString && !string.IsNullOrWhiteSpace(propString))
                    return propString.Trim();

                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
                if (field != null && field.GetValue(target) is string fieldString && !string.IsNullOrWhiteSpace(fieldString))
                    return fieldString.Trim();
            }

            return null;
        }
    }
}
