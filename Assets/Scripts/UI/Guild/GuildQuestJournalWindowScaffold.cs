using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UDA2.UI.Guild
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class GuildQuestJournalWindowScaffold : MonoBehaviour
    {
        [SerializeField] private bool autoBuildIfMissing = true;
        [SerializeField] private float sectionBodyHeight = 260f;
        [SerializeField] private float rowHeight = 56f;

        [ContextMenu("Build Quest Journal UI")]
        public void BuildQuestJournalUi()
        {
            var root = FindOrCreateContentRoot();
            if (root == null)
                return;

            ClearChildren(root);
            EnsureVerticalLayout(root.gameObject);

            var active = CreateSection(root, "ActiveSection", "Активные квесты", "guild_journal_active");
            var completed = CreateSection(root, "CompletedSection", "Завершенные", "guild_journal_completed");
            var failed = CreateSection(root, "FailedSection", "Проваленные", "guild_journal_failed");

            var controller = GetComponentInChildren<GuildQuestJournalWindowController>(true);
            if (controller != null)
                controller.ConfigureGeneratedSections(active, completed, failed);

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
#endif
        }

        private void OnEnable()
        {
            if (!autoBuildIfMissing)
                return;

            var root = FindOrCreateContentRoot();
            if (root == null)
                return;

            if (HasSection(root, "ActiveSection") && HasSection(root, "CompletedSection") && HasSection(root, "FailedSection"))
                return;

            BuildQuestJournalUi();
        }

        private RectTransform FindOrCreateContentRoot()
        {
            var existing = transform.Find("ShelterBedWindow/ContentRow");
            if (existing is RectTransform existingRt)
                return existingRt;

            existing = transform.Find("ContentRow");
            if (existing is RectTransform existingSimple)
                return existingSimple;

            var go = new GameObject("ContentRow", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(40f, 40f);
            rt.offsetMax = new Vector2(-40f, -40f);
            return rt;
        }

        private static bool HasSection(RectTransform root, string name)
        {
            return root.Find(name) != null;
        }

        private static void ClearChildren(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child == null)
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(child.gameObject);
                else
#endif
                    Object.Destroy(child.gameObject);
            }
        }

        private static void EnsureVerticalLayout(GameObject go)
        {
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = go.AddComponent<VerticalLayoutGroup>();

            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 12f;
            vlg.padding = new RectOffset(8, 8, 8, 8);

            var fitter = go.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private GuildQuestJournalWindowController.CategorySection CreateSection(RectTransform parent, string sectionName, string fallbackTitle, string localizationKey)
        {
            var sectionGo = new GameObject(sectionName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            var sectionRt = sectionGo.GetComponent<RectTransform>();
            sectionRt.SetParent(parent, false);

            var sectionImg = sectionGo.GetComponent<Image>();
            sectionImg.color = new Color(0.09f, 0.11f, 0.18f, 0.85f);

            var sectionLayout = sectionGo.GetComponent<VerticalLayoutGroup>();
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;
            sectionLayout.spacing = 6f;
            sectionLayout.padding = new RectOffset(8, 8, 8, 8);

            var sectionLe = sectionGo.GetComponent<LayoutElement>();
            sectionLe.minHeight = 80f;

            var headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.SetParent(sectionRt, false);
            var headerImg = headerGo.GetComponent<Image>();
            headerImg.color = new Color(0.16f, 0.2f, 0.32f, 1f);
            var headerButton = headerGo.GetComponent<Button>();

            var headerH = headerGo.GetComponent<HorizontalLayoutGroup>();
            headerH.childControlWidth = true;
            headerH.childControlHeight = true;
            headerH.childForceExpandWidth = false;
            headerH.childForceExpandHeight = false;
            headerH.spacing = 8f;
            headerH.padding = new RectOffset(12, 12, 8, 8);
            headerH.childAlignment = TextAnchor.MiddleLeft;

            var headerLe = headerGo.GetComponent<LayoutElement>();
            headerLe.preferredHeight = 56f;

            var arrowGo = CreateTextNode(headerRt, "Arrow", "▼", 28, TextAlignmentOptions.Center);
            var arrowRt = arrowGo.GetComponent<RectTransform>();
            var arrowLe = arrowGo.AddComponent<LayoutElement>();
            arrowLe.preferredWidth = 36f;
            arrowLe.preferredHeight = 36f;

            var titleGo = CreateTextNode(headerRt, "Title", fallbackTitle, 30, TextAlignmentOptions.Left);
            var titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;
            var titleLocalized = titleGo.AddComponent<LocalizedGlobalComponent>();
            titleLocalized.Key = localizationKey;
            titleLocalized.ClearArgs();

            var countGo = CreateTextNode(headerRt, "Count", "0", 30, TextAlignmentOptions.Right);
            var countLe = countGo.AddComponent<LayoutElement>();
            countLe.preferredWidth = 80f;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.SetParent(sectionRt, false);
            bodyGo.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.16f, 0.9f);
            var bodyLe = bodyGo.GetComponent<LayoutElement>();
            bodyLe.preferredHeight = sectionBodyHeight;
            bodyLe.minHeight = 120f;

            var scrollGo = new GameObject("Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.SetParent(bodyRt, false);
            Stretch(scrollRt, 8f, 8f, 8f, 8f);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.SetParent(scrollRt, false);
            Stretch(viewportRt, 0f, 0f, 0f, 0f);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(viewportRt, false);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(6, 6, 6, 6);

            var csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var rowTemplate = CreateRowTemplate(contentRt);
            rowTemplate.gameObject.SetActive(false);

            var section = new GuildQuestJournalWindowController.CategorySection
            {
                id = sectionName,
                titleLocalizationKey = localizationKey,
                titleFallback = fallbackTitle,
                headerButton = headerButton,
                titleText = titleGo.GetComponent<TMP_Text>(),
                titleLocalized = titleLocalized,
                countText = countGo.GetComponent<TMP_Text>(),
                arrow = arrowRt,
                dropdownBody = bodyGo,
                listContent = contentRt,
                rowTemplate = rowTemplate,
                expanded = true
            };

            return section;
        }

        private GuildQuestJournalQuestRowView CreateRowTemplate(RectTransform parent)
        {
            var rowGo = new GameObject("QuestRowTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(GuildQuestJournalQuestRowView));
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(parent, false);

            rowGo.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.38f, 1f);

            var rowLe = rowGo.GetComponent<LayoutElement>();
            rowLe.preferredHeight = rowHeight;
            rowLe.minHeight = rowHeight;

            var titleGo = CreateTextNode(rowRt, "Title", "Quest", 26, TextAlignmentOptions.Left);
            var titleRt = titleGo.GetComponent<RectTransform>();
            Stretch(titleRt, 16f, 12f, 0f, 0f);

            var rowView = rowGo.GetComponent<GuildQuestJournalQuestRowView>();

#if UNITY_EDITOR
            // Best effort auto-wire for inspector convenience.
            var so = new SerializedObject(rowView);
            so.FindProperty("rowButton").objectReferenceValue = rowGo.GetComponent<Button>();
            so.FindProperty("titleText").objectReferenceValue = titleGo.GetComponent<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
#endif

            return rowView;
        }

        private static GameObject CreateTextNode(Transform parent, string name, string value, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            return go;
        }

        private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
