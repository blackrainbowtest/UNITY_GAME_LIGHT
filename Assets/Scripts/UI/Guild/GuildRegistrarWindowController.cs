using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UDA2.SaveSystem.Guild;
using UDA2.UI.Common;

namespace UDA2.UI.Guild
{
    public sealed class GuildRegistrarWindowController : MonoBehaviour
    {
        [Header("Rank")]
        [SerializeField] private TMP_Text currentRankText;
        [SerializeField] private TMP_Text targetRankText;
        [SerializeField] private Image currentRankIcon;
        [SerializeField] private Image targetRankIcon;
        [SerializeField] private GuildRankVisualConfigAsset rankVisualConfig;
        [SerializeField] private GuildRankProgressionConfigAsset rankProgressionConfig;

        [Header("Guild Config")]
        [SerializeField] private GuildQuestBoardConfigAsset questBoardConfig;
        [SerializeField] private bool configureGuildRuntimeApiOnEnable = true;

        [Header("Requirements")]
        [SerializeField] private TMP_Text goldRequirementText;
        [SerializeField] private TMP_Text levelRequirementText;
        [SerializeField] private TMP_Text questsRequirementText;

        [Header("Resource Rows")]
        [SerializeField] private Transform resourcesRoot;
        [SerializeField] private GuildRegistrarResourceRowView resourceRowPrefab;
        [Tooltip("Optional scene/template row under resourcesRoot. It can stay disabled and will be cloned at runtime.")]
        [SerializeField] private GuildRegistrarResourceRowView resourceRowTemplate;
        [SerializeField] private UnityEngine.Object itemDatabase;

        [Header("Actions")]
        [SerializeField] private Button rankUpButton;
        [SerializeField] private bool refreshOnEnable = true;

        private readonly List<GuildRegistrarResourceRowView> spawnedRows = new List<GuildRegistrarResourceRowView>();
        private bool runtimeConfigured;

        private void Awake()
        {
            ResolveResourceRowTemplateIfNeeded();

            if (rankUpButton != null)
                rankUpButton.onClick.AddListener(HandleRankUpClick);
        }

        private void OnDestroy()
        {
            if (rankUpButton != null)
                rankUpButton.onClick.RemoveListener(HandleRankUpClick);
        }

        private void OnEnable()
        {
            EnsureGuildRuntimeConfigured();

            if (refreshOnEnable)
                RefreshView();
        }

        public void RefreshView()
        {
            EnsureGuildRuntimeConfigured();

            if (!GuildRuntimeAPI.TryGetRankUpViewData(out var data) || data == null)
            {
                ApplyNoNextRankState();
                return;
            }

            ApplyRankTexts(data.currentRank, data.targetRank);
            ApplyRankVisual(data.currentRank, currentRankText, currentRankIcon);
            ApplyRankVisual(data.targetRank, targetRankText, targetRankIcon);

            if (goldRequirementText != null)
                goldRequirementText.text = $"{data.currentGold}/{data.requiredGold}";

            if (levelRequirementText != null)
                levelRequirementText.text = $"{data.currentHeroLevel}/{data.requiredHeroLevel}";

            if (questsRequirementText != null)
                questsRequirementText.text = $"{data.currentCompletedQuests}/{data.requiredCompletedQuests}";

            if (rankUpButton != null)
                rankUpButton.interactable = data.canRankUpNow;

            RebuildResourceRows(data.requiredItems);
        }

        private void HandleRankUpClick()
        {
            if (GuildRuntimeAPI.TryRankUp(out _))
                RefreshView();
        }

        private void ApplyNoNextRankState()
        {
            if (currentRankText != null)
                currentRankText.text = GuildRuntimeAPI.GetCurrentRank().ToString();

            if (targetRankText != null)
                targetRankText.text = "MAX";

            if (goldRequirementText != null)
                goldRequirementText.text = "-";

            if (levelRequirementText != null)
                levelRequirementText.text = "-";

            if (questsRequirementText != null)
                questsRequirementText.text = "-";

            if (rankUpButton != null)
                rankUpButton.interactable = false;

            RebuildResourceRows(null);
        }

        private void ApplyRankTexts(AdventurerRank current, AdventurerRank target)
        {
            if (currentRankText != null)
                currentRankText.text = current.ToString();

            if (targetRankText != null)
                targetRankText.text = target.ToString();
        }

        private void ApplyRankVisual(AdventurerRank rank, TMP_Text text, Image icon)
        {
            if (rankVisualConfig == null || !rankVisualConfig.TryGet(rank, out var visual) || visual == null)
                return;

            if (text != null)
                text.color = visual.textColor;

            if (icon != null)
            {
                icon.sprite = visual.icon;
                icon.enabled = visual.icon != null;

                if (visual.icon != null)
                {
                    var adaptiveSize = icon.GetComponent<RectSizeByParent>();
                    if (adaptiveSize != null)
                    {
                        adaptiveSize.ApplyNow();
                    }
                }
            }
        }

        private void RebuildResourceRows(List<GuildItemRequirementProgress> items)
        {
            for (var i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i] != null)
                    Destroy(spawnedRows[i].gameObject);
            }

            spawnedRows.Clear();

            if (resourcesRoot == null || items == null)
                return;

            var rowPrototype = ResolveRowPrototype();
            if (rowPrototype == null)
            {
                Debug.LogWarning("[GuildRegistrarWindowController] Resource row source is missing. Assign resourceRowPrefab or resourceRowTemplate.", this);
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var req = items[i];
                if (req == null)
                    continue;

                var row = Instantiate(rowPrototype, resourcesRoot, false);
                row.gameObject.SetActive(true);
                row.Render(req, ResolveItemIcon(req.itemId));
                spawnedRows.Add(row);
            }
        }

        private Sprite ResolveItemIcon(string itemId)
        {
            if (itemDatabase == null || string.IsNullOrWhiteSpace(itemId))
                return null;

            try
            {
                var dbType = itemDatabase.GetType();
                var getById = dbType.GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public);
                if (getById == null)
                    return null;

                var def = getById.Invoke(itemDatabase, new object[] { itemId.Trim() });
                if (def == null)
                    return null;

                var iconProp = def.GetType().GetProperty("Icon", BindingFlags.Instance | BindingFlags.Public);
                return iconProp != null ? iconProp.GetValue(def) as Sprite : null;
            }
            catch
            {
                return null;
            }
        }

        private void EnsureGuildRuntimeConfigured()
        {
            if (!configureGuildRuntimeApiOnEnable || runtimeConfigured)
                return;

            if (rankProgressionConfig == null)
            {
                Debug.LogWarning("[GuildRegistrarWindowController] rankProgressionConfig is not assigned. Next rank requirement data may be unavailable.", this);
                return;
            }

            GuildRuntimeAPI.Configure(rankProgressionConfig, questBoardConfig);
            runtimeConfigured = true;
        }

        private GuildRegistrarResourceRowView ResolveRowPrototype()
        {
            if (resourceRowPrefab != null)
                return resourceRowPrefab;

            ResolveResourceRowTemplateIfNeeded();
            return resourceRowTemplate;
        }

        private void ResolveResourceRowTemplateIfNeeded()
        {
            if (resourceRowTemplate != null || resourcesRoot == null)
                return;

            resourceRowTemplate = resourcesRoot.GetComponentInChildren<GuildRegistrarResourceRowView>(includeInactive: true);
        }
    }
}
