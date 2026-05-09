using System;
using System.Collections.Generic;
using TMPro;
using UDA2.SaveSystem.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Guild
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GuildQuestJournalWindowController : MonoBehaviour
    {
        [Serializable]
        public sealed class CategorySection
        {
            [Header("Category")]
            public string id;
            public string titleLocalizationKey;
            public string titleFallback;

            [Header("Header")]
            public Button headerButton;
            public TMP_Text titleText;
            public LocalizedGlobalComponent titleLocalized;
            public TMP_Text countText;
            public RectTransform arrow;
            public float expandedArrowZ = -90f;
            public float collapsedArrowZ = 0f;

            [Header("Content")]
            public GameObject dropdownBody;
            public Transform listContent;
            public GuildQuestJournalQuestRowView rowTemplate;

            [Header("Pagination")]
            public Button prevPageButton;
            public Button nextPageButton;
            public TMP_Text pageText;
            [Tooltip("If > 0, overrides global itemsPerPage for this section.")]
            public int itemsPerPageOverride;

            [NonSerialized] public bool expanded;
            [NonSerialized] public readonly List<GuildQuestJournalQuestRowView> spawnedRows = new List<GuildQuestJournalQuestRowView>();
            [NonSerialized] public readonly List<GuildQuestDefinitionAsset> cachedQuests = new List<GuildQuestDefinitionAsset>();
            [NonSerialized] public bool isTakenSection;
            [NonSerialized] public bool allowActions;
            [NonSerialized] public int currentPage;
        }

        [Header("Guild Runtime")]
        [SerializeField] private GuildRankProgressionConfigAsset rankConfig;
        [SerializeField] private GuildQuestBoardConfigAsset boardConfig;
        [SerializeField] private bool configureRuntimeApiOnEnable = true;

        [Header("Details")]
        [SerializeField] private GameObject questDetailsPrefab;
        [SerializeField] private Transform detailsParent;
        [SerializeField] private bool forceFullscreenDetails = true;

        [Header("Categories")]
        [SerializeField] private CategorySection activeSection;
        [SerializeField] private CategorySection completedSection;
        [SerializeField] private CategorySection failedSection;

        [Header("Behavior")]
        [SerializeField] private bool refreshOnEnable = true;
        [SerializeField] private bool openActiveByDefault = true;
        [SerializeField] private bool singleExpandedSection = true;
        [SerializeField] private int itemsPerPage = 10;

        [Header("Auto Pagination Style")]
        [SerializeField] private Color paginationButtonColor = new Color(0.2f, 0.24f, 0.38f, 1f);
        [SerializeField] private Color paginationTextColor = Color.white;
        [SerializeField] private float paginationBarHeight = 48f;
        [SerializeField] private float paginationButtonWidth = 72f;
        [SerializeField] private float paginationButtonHeight = 36f;
        [SerializeField] private float paginationPageTextWidth = 120f;
        [SerializeField] private float paginationPageTextHeight = 36f;
        [SerializeField] private float paginationButtonFontSize = 24f;
        [SerializeField] private float paginationPageFontSize = 22f;

        private bool runtimeConfigured;

        public void ConfigureGeneratedSections(CategorySection active, CategorySection completed, CategorySection failed)
        {
            activeSection = active;
            completedSection = completed;
            failedSection = failed;
        }

        private void Awake()
        {
            AutoWireMissingReferences();

            ConfigureSectionHeader(activeSection, HandleActiveHeaderClick);
            ConfigureSectionHeader(completedSection, HandleCompletedHeaderClick);
            ConfigureSectionHeader(failedSection, HandleFailedHeaderClick);
            ConfigurePaginationButtons(activeSection, HandleActivePrevPage, HandleActiveNextPage);
            ConfigurePaginationButtons(completedSection, HandleCompletedPrevPage, HandleCompletedNextPage);
            ConfigurePaginationButtons(failedSection, HandleFailedPrevPage, HandleFailedNextPage);

            EnsureTemplateHidden(activeSection);
            EnsureTemplateHidden(completedSection);
            EnsureTemplateHidden(failedSection);
        }

        private void OnEnable()
        {
            AutoWireMissingReferences();

            // Rebind after auto-wiring in case pagination controls were created in edit mode.
            ConfigureSectionHeader(activeSection, HandleActiveHeaderClick);
            ConfigureSectionHeader(completedSection, HandleCompletedHeaderClick);
            ConfigureSectionHeader(failedSection, HandleFailedHeaderClick);
            ConfigurePaginationButtons(activeSection, HandleActivePrevPage, HandleActiveNextPage);
            ConfigurePaginationButtons(completedSection, HandleCompletedPrevPage, HandleCompletedNextPage);
            ConfigurePaginationButtons(failedSection, HandleFailedPrevPage, HandleFailedNextPage);

            if (!Application.isPlaying)
                return;

            EnsureRuntimeConfigured();

            if (openActiveByDefault)
            {
                SetExpanded(activeSection, true);
                SetExpanded(completedSection, false);
                SetExpanded(failedSection, false);
            }

            if (refreshOnEnable)
                RefreshAll();
        }

        private void OnDestroy()
        {
            UnsubscribeHeader(activeSection, HandleActiveHeaderClick);
            UnsubscribeHeader(completedSection, HandleCompletedHeaderClick);
            UnsubscribeHeader(failedSection, HandleFailedHeaderClick);
            UnsubscribePaginationButtons(activeSection, HandleActivePrevPage, HandleActiveNextPage);
            UnsubscribePaginationButtons(completedSection, HandleCompletedPrevPage, HandleCompletedNextPage);
            UnsubscribePaginationButtons(failedSection, HandleFailedPrevPage, HandleFailedNextPage);

            ClearRows(activeSection);
            ClearRows(completedSection);
            ClearRows(failedSection);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoWireMissingReferences();
        }
#endif

        [ContextMenu("Create Missing Pagination Controls")]
        public void CreateMissingPaginationControls()
        {
            AutoWireMissingReferences();
        }

        public void RefreshAll()
        {
            var service = GuildRuntimeAPI.GetService();
            if (service == null)
            {
                PopulateSection(activeSection, null, true, allowActions: true);
                PopulateSection(completedSection, null, false, allowActions: false);
                PopulateSection(failedSection, null, false, allowActions: false);
                return;
            }

            service.RefreshQuestBoardIfNeeded();

            var map = BuildQuestMap();
            var activeIds = service.GetSelectedQuestIds();
            var completedIds = service.GetCompletedQuestIds();
            var failedIds = service.GetFailedQuestIds();

            PopulateSection(activeSection, ResolveQuestList(activeIds, map), true, allowActions: true);
            PopulateSection(completedSection, ResolveQuestList(completedIds, map), false, allowActions: false);
            PopulateSection(failedSection, ResolveQuestList(failedIds, map), false, allowActions: false);
        }

        private void PopulateSection(CategorySection section, List<GuildQuestDefinitionAsset> quests, bool isTaken, bool allowActions)
        {
            if (section == null)
                return;

            ApplySectionTitle(section);
            ClearRows(section);

            section.isTakenSection = isTaken;
            section.allowActions = allowActions;
            section.cachedQuests.Clear();
            if (quests != null)
            {
                for (int _qi = 0; _qi < quests.Count; _qi++)
                {
                    if (quests[_qi] != null)
                        section.cachedQuests.Add(quests[_qi]);
                }
            }

            var effectiveItemsPerPage = GetItemsPerPage(section);
            var totalCount = section.cachedQuests.Count;
            var totalPages = Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)effectiveItemsPerPage));
            section.currentPage = Mathf.Clamp(section.currentPage, 0, totalPages - 1);

            var count = totalCount;
            if (section.countText != null)
                section.countText.text = count.ToString();

            RenderCurrentPage(section);
        }

        private void OpenQuestDetails(GuildQuestDefinitionAsset quest, bool isTaken, bool allowActions)
        {
            if (quest == null || questDetailsPrefab == null)
                return;

            var parent = ResolveDetailsParent();
            var detailsGo = Instantiate(questDetailsPrefab, parent, worldPositionStays: false);
            if (detailsGo == null)
                return;

            BringDetailsToFront(detailsGo, parent);

            var details = detailsGo.GetComponent<GuildQuestDetailsView>();
            if (details == null)
                details = detailsGo.GetComponentInChildren<GuildQuestDetailsView>(includeInactive: true);

            if (details == null)
                return;

            details.SetOwningRoot(detailsGo);
            details.Bind(quest, TryAcceptQuest, TrySubmitQuest, isTaken, onClick: null, allowActions: allowActions);
        }

        private bool TryAcceptQuest(string questId)
        {
            if (!GuildRuntimeAPI.TrySelectQuest(questId))
                return false;

            var save = GameState.Instance?.CurrentSave;
            if (save != null)
                SaveSlotsManager.SaveToSlot(SaveSlotsManager.GetRuntimeSaveSlotOrAutosave(), save, rememberAsCurrentRuntimeSlot: false);

            RefreshAll();
            return true;
        }

        private bool TrySubmitQuest(string questId)
        {
            if (!GuildRuntimeAPI.TrySubmitQuest(questId, out _))
                return false;

            var save = GameState.Instance?.CurrentSave;
            if (save != null)
                SaveSlotsManager.SaveToSlot(SaveSlotsManager.GetRuntimeSaveSlotOrAutosave(), save, rememberAsCurrentRuntimeSlot: false);

            RefreshAll();
            return true;
        }

        private Dictionary<string, GuildQuestDefinitionAsset> BuildQuestMap()
        {
            var map = new Dictionary<string, GuildQuestDefinitionAsset>(StringComparer.Ordinal);

            if (boardConfig == null || boardConfig.questPool == null)
                return map;

            var pool = boardConfig.questPool;
            for (int i = 0; i < pool.Count; i++)
            {
                var q = pool[i];
                if (q == null || string.IsNullOrWhiteSpace(q.questId))
                    continue;

                var key = q.questId.Trim();
                if (!map.ContainsKey(key))
                    map[key] = q;
            }

            return map;
        }

        private static List<GuildQuestDefinitionAsset> ResolveQuestList(IReadOnlyList<string> ids, Dictionary<string, GuildQuestDefinitionAsset> map)
        {
            var result = new List<GuildQuestDefinitionAsset>();
            if (ids == null || map == null)
                return result;

            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (!map.TryGetValue(id, out var quest) || quest == null)
                    continue;

                result.Add(quest);
            }

            return result;
        }

        private void ConfigureSectionHeader(CategorySection section, Action onClick)
        {
            if (section?.headerButton == null)
                return;

            if (onClick == HandleActiveHeaderClick)
            {
                section.headerButton.onClick.RemoveListener(HandleActiveHeaderClick);
                section.headerButton.onClick.AddListener(HandleActiveHeaderClick);
            }
            else if (onClick == HandleCompletedHeaderClick)
            {
                section.headerButton.onClick.RemoveListener(HandleCompletedHeaderClick);
                section.headerButton.onClick.AddListener(HandleCompletedHeaderClick);
            }
            else if (onClick == HandleFailedHeaderClick)
            {
                section.headerButton.onClick.RemoveListener(HandleFailedHeaderClick);
                section.headerButton.onClick.AddListener(HandleFailedHeaderClick);
            }
        }

        private void UnsubscribeHeader(CategorySection section, Action onClick)
        {
            if (section?.headerButton == null)
                return;

            if (onClick == HandleActiveHeaderClick)
                section.headerButton.onClick.RemoveListener(HandleActiveHeaderClick);
            else if (onClick == HandleCompletedHeaderClick)
                section.headerButton.onClick.RemoveListener(HandleCompletedHeaderClick);
            else if (onClick == HandleFailedHeaderClick)
                section.headerButton.onClick.RemoveListener(HandleFailedHeaderClick);
        }

        private void HandleActiveHeaderClick()
        {
            ToggleExpanded(activeSection, completedSection, failedSection);
        }

        private void HandleCompletedHeaderClick()
        {
            ToggleExpanded(completedSection, activeSection, failedSection);
        }

        private void HandleFailedHeaderClick()
        {
            ToggleExpanded(failedSection, activeSection, completedSection);
        }

        private void HandleActivePrevPage() => ChangePage(activeSection, -1);
        private void HandleActiveNextPage() => ChangePage(activeSection, +1);
        private void HandleCompletedPrevPage() => ChangePage(completedSection, -1);
        private void HandleCompletedNextPage() => ChangePage(completedSection, +1);
        private void HandleFailedPrevPage() => ChangePage(failedSection, -1);
        private void HandleFailedNextPage() => ChangePage(failedSection, +1);

        private void ToggleExpanded(CategorySection target, params CategorySection[] others)
        {
            if (target == null)
                return;

            var newState = !target.expanded;
            SetExpanded(target, newState);

            if (singleExpandedSection && newState && others != null)
            {
                for (var i = 0; i < others.Length; i++)
                    SetExpanded(others[i], false);
            }
        }

        private static void SetExpanded(CategorySection section, bool expanded)
        {
            if (section == null)
                return;

            section.expanded = expanded;

            if (section.dropdownBody != null)
            {
                section.dropdownBody.SetActive(expanded);

                var le = section.dropdownBody.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.flexibleHeight = expanded ? 1f : 0f;
                    if (!expanded)
                        le.preferredHeight = 0f;
                }
            }

            if (section.arrow != null)
            {
                var e = section.arrow.localEulerAngles;
                e.z = expanded ? section.expandedArrowZ : section.collapsedArrowZ;
                section.arrow.localEulerAngles = e;
            }
        }

        private static GuildQuestJournalQuestRowView ResolveTemplate(CategorySection section)
        {
            if (section == null)
                return null;

            if (section.rowTemplate != null)
                return section.rowTemplate;

            if (section.listContent == null)
                return null;

            section.rowTemplate = section.listContent.GetComponentInChildren<GuildQuestJournalQuestRowView>(includeInactive: true);
            return section.rowTemplate;
        }

        private static void EnsureTemplateHidden(CategorySection section)
        {
            var template = ResolveTemplate(section);
            if (template != null)
                template.gameObject.SetActive(false);
        }

        private static void ClearRows(CategorySection section)
        {
            if (section == null)
                return;

            for (var i = 0; i < section.spawnedRows.Count; i++)
            {
                if (section.spawnedRows[i] != null)
                    Destroy(section.spawnedRows[i].gameObject);
            }

            section.spawnedRows.Clear();
        }

        private int GetItemsPerPage(CategorySection section)
        {
            if (section != null && section.itemsPerPageOverride > 0)
                return section.itemsPerPageOverride;

            return Mathf.Max(1, itemsPerPage);
        }

        private void ChangePage(CategorySection section, int delta)
        {
            if (section == null)
                return;

            var effectiveItemsPerPage = GetItemsPerPage(section);
            var totalPages = Mathf.Max(1, Mathf.CeilToInt(section.cachedQuests.Count / (float)effectiveItemsPerPage));
            section.currentPage = Mathf.Clamp(section.currentPage + delta, 0, totalPages - 1);
            RenderCurrentPage(section);
        }

        private void RenderCurrentPage(CategorySection section)
        {
            if (section == null)
                return;

            ClearRows(section);

            var template = ResolveTemplate(section);
            if (template == null || section.listContent == null)
            {
                UpdatePaginationUi(section, 1, 1);
                return;
            }

            var effectiveItemsPerPage = GetItemsPerPage(section);
            var totalCount = section.cachedQuests.Count;
            var totalPages = Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)effectiveItemsPerPage));
            section.currentPage = Mathf.Clamp(section.currentPage, 0, totalPages - 1);

            var start = section.currentPage * effectiveItemsPerPage;
            var end = Mathf.Min(totalCount, start + effectiveItemsPerPage);
            for (var i = start; i < end; i++)
            {
                var quest = section.cachedQuests[i];
                if (quest == null)
                    continue;

                var row = Instantiate(template, section.listContent, false);
                row.gameObject.SetActive(true);
                var capturedQuest = quest;
                row.Bind(capturedQuest, () => OpenQuestDetails(capturedQuest, section.isTakenSection, section.allowActions));
                section.spawnedRows.Add(row);
            }

            UpdatePaginationUi(section, section.currentPage + 1, totalPages);
        }

        private static void UpdatePaginationUi(CategorySection section, int page, int totalPages)
        {
            if (section == null)
                return;

            var shouldShowPagination = totalPages > 1;
            var paginationContainer = FindPaginationContainer(section);

            if (paginationContainer != null)
            {
                paginationContainer.SetActive(shouldShowPagination);
            }
            else
            {
                if (section.pageText != null)
                    section.pageText.gameObject.SetActive(shouldShowPagination);

                if (section.prevPageButton != null)
                    section.prevPageButton.gameObject.SetActive(shouldShowPagination);

                if (section.nextPageButton != null)
                    section.nextPageButton.gameObject.SetActive(shouldShowPagination);
            }

            if (section.pageText != null)
                section.pageText.text = page.ToString() + " / " + totalPages.ToString();

            if (section.prevPageButton != null)
                section.prevPageButton.interactable = page > 1;

            if (section.nextPageButton != null)
                section.nextPageButton.interactable = page < totalPages;
        }

        private static GameObject FindPaginationContainer(CategorySection section)
        {
            if (section == null)
                return null;

            if (section.pageText != null)
            {
                var parent = section.pageText.transform.parent;
                if (parent != null && parent.name.IndexOf("pagination", StringComparison.OrdinalIgnoreCase) >= 0)
                    return parent.gameObject;
            }

            if (section.prevPageButton != null && section.nextPageButton != null)
            {
                var prevParent = section.prevPageButton.transform.parent;
                var nextParent = section.nextPageButton.transform.parent;
                if (prevParent != null && prevParent == nextParent)
                    return prevParent.gameObject;
            }

            return null;
        }

        private static void ConfigurePaginationButtons(CategorySection section, UnityEngine.Events.UnityAction onPrev, UnityEngine.Events.UnityAction onNext)
        {
            if (section == null)
                return;

            if (section.prevPageButton != null)
            {
                section.prevPageButton.onClick.RemoveListener(onPrev);
                section.prevPageButton.onClick.AddListener(onPrev);
            }

            if (section.nextPageButton != null)
            {
                section.nextPageButton.onClick.RemoveListener(onNext);
                section.nextPageButton.onClick.AddListener(onNext);
            }
        }

        private static void UnsubscribePaginationButtons(CategorySection section, UnityEngine.Events.UnityAction onPrev, UnityEngine.Events.UnityAction onNext)
        {
            if (section == null)
                return;

            if (section.prevPageButton != null)
                section.prevPageButton.onClick.RemoveListener(onPrev);

            if (section.nextPageButton != null)
                section.nextPageButton.onClick.RemoveListener(onNext);
        }

        private void ApplySectionTitle(CategorySection section)
        {
            if (section == null)
                return;

            if (section.titleLocalized == null && section.titleText != null)
                section.titleLocalized = section.titleText.GetComponent<LocalizedGlobalComponent>();

            if (section.titleLocalized != null)
            {
                section.titleLocalized.Key = section.titleLocalizationKey;
                section.titleLocalized.ClearArgs();
                return;
            }

            if (section.titleText != null)
                section.titleText.text = section.titleFallback ?? string.Empty;
        }

        private void EnsureRuntimeConfigured()
        {
            if (!configureRuntimeApiOnEnable || runtimeConfigured)
                return;

            GuildRuntimeAPI.Configure(rankConfig, boardConfig);
            runtimeConfigured = true;
        }

        private Transform ResolveDetailsParent()
        {
            if (detailsParent != null)
                return detailsParent;

            var ownCanvas = GetComponentInParent<Canvas>();
            if (ownCanvas != null && ownCanvas.isActiveAndEnabled)
                return ownCanvas.transform;

#if UNITY_2023_1_OR_NEWER || UNITY_2022_2_OR_NEWER
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            var canvases = FindObjectsOfType<Canvas>(true);
#pragma warning restore CS0618
#endif
            if (canvases == null || canvases.Length == 0)
                return transform;

            Canvas best = null;
            var bestScore = int.MinValue;
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null || !c.isActiveAndEnabled)
                    continue;

                var score = c.sortingOrder;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    score += 10000;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = c;
            }

            return best != null ? best.transform : transform;
        }

        private void BringDetailsToFront(GameObject detailsGo, Transform parent)
        {
            if (detailsGo == null)
                return;

            detailsGo.transform.SetAsLastSibling();

            if (!forceFullscreenDetails)
                return;

            var rt = detailsGo.GetComponent<RectTransform>();
            if (rt == null)
                return;

            if (parent is RectTransform)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
        }

        private void AutoWireMissingReferences()
        {
            AutoWireSection(activeSection, "ActiveSection");
            AutoWireSection(completedSection, "CompletedSection");
            AutoWireSection(failedSection, "FailedSection");
        }

        private void AutoWireSection(CategorySection section, string fallbackSectionName)
        {
            if (section == null)
                return;

            var sectionName = !string.IsNullOrWhiteSpace(section.id) ? section.id : fallbackSectionName;
            var sectionRoot = !string.IsNullOrWhiteSpace(sectionName)
                ? FindDeepChild(transform, sectionName)
                : null;

            if (sectionRoot == null)
                sectionRoot = ResolveSectionRootFromKnownReferences(section);

            if (sectionRoot != null && section.headerButton == null)
            {
                var header = FindDeepChild(sectionRoot, "Header");
                if (header != null)
                    section.headerButton = header.GetComponent<Button>();
            }

            if (sectionRoot != null && section.dropdownBody == null)
            {
                var body = FindDeepChild(sectionRoot, "Body");
                if (body != null)
                    section.dropdownBody = body.gameObject;
            }

            if (sectionRoot != null && section.listContent == null)
            {
                var content = FindDeepChild(sectionRoot, "Content");
                if (content != null)
                    section.listContent = content;
            }

            if (section.rowTemplate == null && section.listContent != null)
                section.rowTemplate = section.listContent.GetComponentInChildren<GuildQuestJournalQuestRowView>(includeInactive: true);

            if (sectionRoot != null && section.titleText == null)
                section.titleText = FindTextInSectionByHint(sectionRoot, "title");

            if (section.titleLocalized == null && section.titleText != null)
                section.titleLocalized = section.titleText.GetComponent<LocalizedGlobalComponent>();

            if (sectionRoot != null && section.countText == null)
                section.countText = FindTextInSectionByHint(sectionRoot, "count", "counter");

            if (sectionRoot != null && section.arrow == null)
            {
                var arrow = FindDeepChild(sectionRoot, "Arrow");
                if (arrow != null)
                    section.arrow = arrow as RectTransform;
            }

            if (sectionRoot != null && section.prevPageButton == null)
                section.prevPageButton = FindButtonInSectionByHint(sectionRoot, "prev", "previous", "back");

            if (sectionRoot != null && section.nextPageButton == null)
                section.nextPageButton = FindButtonInSectionByHint(sectionRoot, "next", "forward");

            if (sectionRoot != null && section.pageText == null)
                section.pageText = FindTextInSectionByHint(sectionRoot, "page", "pagination", "pager");

            EnsurePaginationControls(section, sectionRoot);
        }

        private void EnsurePaginationControls(CategorySection section, Transform sectionRoot)
        {
            if (section == null)
                return;

            if (section.prevPageButton != null && section.nextPageButton != null && section.pageText != null)
                return;

            var bodyRoot = ResolveBodyRoot(section, sectionRoot);
            if (bodyRoot == null)
                return;

            var paginationRoot = FindDeepChild(bodyRoot, "Pagination");
            if (paginationRoot == null)
                paginationRoot = CreatePaginationRoot(bodyRoot);

            if (section.prevPageButton == null)
                section.prevPageButton = FindButtonInSectionByHint(paginationRoot, "prev", "previous", "back");

            if (section.nextPageButton == null)
                section.nextPageButton = FindButtonInSectionByHint(paginationRoot, "next", "forward");

            if (section.pageText == null)
                section.pageText = FindTextInSectionByHint(paginationRoot, "page", "pagination", "pager");
        }

        private static Transform ResolveSectionRootFromKnownReferences(CategorySection section)
        {
            if (section == null)
                return null;

            if (section.dropdownBody != null)
                return section.dropdownBody.transform.parent;

            if (section.listContent != null)
                return section.listContent.parent;

            if (section.headerButton != null)
                return section.headerButton.transform.parent;

            return null;
        }

        private static Transform ResolveBodyRoot(CategorySection section, Transform sectionRoot)
        {
            if (section?.dropdownBody != null)
                return section.dropdownBody.transform;

            if (sectionRoot != null)
            {
                var body = FindDeepChild(sectionRoot, "Body");
                if (body != null)
                    return body;
            }

            if (section?.listContent != null)
                return section.listContent.parent;

            return null;
        }

        private Transform CreatePaginationRoot(Transform bodyRoot)
        {
            var go = new GameObject("Pagination", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(bodyRoot, false);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 8);

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = paginationBarHeight;

            CreatePaginationButton(go.transform, "PrevButton", "<");
            CreatePaginationText(go.transform, "PageText", "1 / 1");
            CreatePaginationButton(go.transform, "NextButton", ">");

            return go.transform;
        }

        private void CreatePaginationButton(Transform parent, string name, string caption)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = paginationButtonColor;

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = paginationButtonWidth;
            le.preferredHeight = paginationButtonHeight;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.SetParent(rt, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = caption;
            text.fontSize = paginationButtonFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = paginationTextColor;
        }

        private void CreatePaginationText(Transform parent, string name, string value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = paginationPageTextWidth;
            le.preferredHeight = paginationPageTextHeight;

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = paginationPageFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = paginationTextColor;
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
                return null;

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child == null)
                    continue;

                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                    return child;

                var nested = FindDeepChild(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static Button FindButtonInSectionByHint(Transform sectionRoot, params string[] hints)
        {
            if (sectionRoot == null)
                return null;

            var buttons = sectionRoot.GetComponentsInChildren<Button>(includeInactive: true);
            for (var i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (btn == null)
                    continue;

                var name = btn.name;
                for (var h = 0; h < hints.Length; h++)
                {
                    if (name.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) >= 0)
                        return btn;
                }
            }

            return null;
        }

        private static TMP_Text FindTextInSectionByHint(Transform sectionRoot, params string[] hints)
        {
            if (sectionRoot == null)
                return null;

            var texts = sectionRoot.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            for (var i = 0; i < texts.Length; i++)
            {
                var txt = texts[i];
                if (txt == null)
                    continue;

                var name = txt.name;
                for (var h = 0; h < hints.Length; h++)
                {
                    if (name.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) >= 0)
                        return txt;
                }
            }

            return null;
        }
    }
}
