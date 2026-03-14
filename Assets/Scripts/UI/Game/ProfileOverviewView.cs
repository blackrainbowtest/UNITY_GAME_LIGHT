using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UDA2.Core;

namespace UDA2.UI.Game
{
    public sealed class ProfileOverviewView : MonoBehaviour
    {
        [Serializable]
        private sealed class ProfileRowBinding
        {
            [SerializeField] private Transform rowRoot;
            [SerializeField] private string titleObjectName = "content_title";
            [SerializeField] private string valueObjectName = "content_value";

            [NonSerialized] private TMP_Text cachedTitle;
            [NonSerialized] private TMP_Text cachedValue;

            public static ProfileRowBinding Create(Transform root, string titleName, string valueName)
            {
                return new ProfileRowBinding
                {
                    rowRoot = root,
                    titleObjectName = titleName,
                    valueObjectName = valueName
                };
            }

            public TMP_Text GetTitleText()
            {
                if (cachedTitle != null)
                    return cachedTitle;

                if (rowRoot == null)
                    return null;

                var titleNode = FindDescendantByName(rowRoot, titleObjectName);
                cachedTitle = titleNode != null ? titleNode.GetComponent<TMP_Text>() : null;
                return cachedTitle;
            }

            public TMP_Text GetValueText()
            {
                if (cachedValue != null)
                    return cachedValue;

                if (rowRoot == null)
                    return null;

                var valueNode = FindDescendantByName(rowRoot, valueObjectName);
                cachedValue = valueNode != null ? valueNode.GetComponent<TMP_Text>() : null;
                return cachedValue;
            }

            private static Transform FindDescendantByName(Transform root, string nodeName)
            {
                if (root == null || string.IsNullOrWhiteSpace(nodeName))
                    return null;

                if (string.Equals(root.name, nodeName, StringComparison.Ordinal))
                    return root;

                for (int i = 0; i < root.childCount; i++)
                {
                    var child = root.GetChild(i);
                    var found = FindDescendantByName(child, nodeName);
                    if (found != null)
                        return found;
                }

                return null;
            }
        }

        private enum ProfileRowId
        {
            PlayerName,
            PlayerId,
            Level,
            Rank,
            Location,
            DayAndTime,
            PlayTime,
            Gold,
            ManaCrystals,
            DemonCrystals,
            StatusEffects,
            RealTimePlayed,
            BattlesFinished,
            BattlesWon,
            BattlesLost,
            BattlesSurrendered,
            EscapesSuccessful,
            EscapesFailed,
            TotalMobKills,
            TotalGoldEarned,
            TotalExpEarned,
            PhysicalDamage,
            MagicDamage,
            PhysicalResistance,
            MagicResistance,
            AttackSpeed,
            CritChance,
            CritMultiplier,
            EvasionChance,
            HitChance,
        }

        private readonly struct RowTemplateSpec
        {
            public readonly ProfileRowId Id;
            public readonly string TitleLocalizationKey;
            public readonly bool IsCombatSection;

            public RowTemplateSpec(ProfileRowId id, string titleLocalizationKey, bool isCombatSection = false)
            {
                Id = id;
                TitleLocalizationKey = titleLocalizationKey;
                IsCombatSection = isCombatSection;
            }
        }

        private static readonly RowTemplateSpec[] AutoTemplateSpecs =
        {
            new RowTemplateSpec(ProfileRowId.PlayerName, "profile_player_name_title"),
            new RowTemplateSpec(ProfileRowId.PlayerId, "profile_player_id_title"),
            new RowTemplateSpec(ProfileRowId.Level, "profile_level_title"),
            new RowTemplateSpec(ProfileRowId.Rank, "profile_rank_title"),
            new RowTemplateSpec(ProfileRowId.Location, "profile_location_title"),
            new RowTemplateSpec(ProfileRowId.DayAndTime, "profile_day_time_title"),
            new RowTemplateSpec(ProfileRowId.PlayTime, "profile_play_time_title"),
            new RowTemplateSpec(ProfileRowId.Gold, "profile_gold_title"),
            new RowTemplateSpec(ProfileRowId.ManaCrystals, "profile_mana_crystals_title"),
            new RowTemplateSpec(ProfileRowId.DemonCrystals, "profile_demon_crystals_title"),
            new RowTemplateSpec(ProfileRowId.StatusEffects, "profile_status_effects_title"),
            new RowTemplateSpec(ProfileRowId.RealTimePlayed, "profile_real_time_played_title"),
            new RowTemplateSpec(ProfileRowId.BattlesFinished, "profile_battles_finished_title"),
            new RowTemplateSpec(ProfileRowId.BattlesWon, "profile_battles_won_title"),
            new RowTemplateSpec(ProfileRowId.BattlesLost, "profile_battles_lost_title"),
            new RowTemplateSpec(ProfileRowId.BattlesSurrendered, "profile_battles_surrendered_title"),
            new RowTemplateSpec(ProfileRowId.EscapesSuccessful, "profile_escapes_successful_title"),
            new RowTemplateSpec(ProfileRowId.EscapesFailed, "profile_escapes_failed_title"),
            new RowTemplateSpec(ProfileRowId.TotalMobKills, "profile_total_mob_kills_title"),
            new RowTemplateSpec(ProfileRowId.TotalGoldEarned, "profile_total_gold_earned_title"),
            new RowTemplateSpec(ProfileRowId.TotalExpEarned, "profile_total_exp_earned_title"),
            new RowTemplateSpec(ProfileRowId.PhysicalDamage, "profile_physical_damage_title", isCombatSection: true),
            new RowTemplateSpec(ProfileRowId.MagicDamage, "profile_magic_damage_title", isCombatSection: true),
            new RowTemplateSpec(ProfileRowId.PhysicalResistance, "profile_physical_resistance_title", isCombatSection: true),
            new RowTemplateSpec(ProfileRowId.MagicResistance, "profile_magic_resistance_title", isCombatSection: true),
            new RowTemplateSpec(ProfileRowId.AttackSpeed, "profile_attack_speed_title", isCombatSection: true),
            new RowTemplateSpec(ProfileRowId.CritChance, "profile_crit_chance_title", isCombatSection: true),
            new RowTemplateSpec(ProfileRowId.CritMultiplier, "profile_crit_multiplier_title", isCombatSection: true),
            new RowTemplateSpec(ProfileRowId.EvasionChance, "profile_evasion_chance_title", isCombatSection: true),
            new RowTemplateSpec(ProfileRowId.HitChance, "profile_hit_chance_title", isCombatSection: true),
        };

        private static Type localizedGlobalComponentType;
        private static PropertyInfo localizedGlobalComponentKeyProperty;
        private static bool localizationDriverTypeResolved;

        private readonly Dictionary<ProfileRowId, ProfileRowBinding> autoRows = new Dictionary<ProfileRowId, ProfileRowBinding>(32);

        [Header("Wiring")]
        [SerializeField] private ProfileRowBinding playerNameRow;
        [SerializeField] private ProfileRowBinding playerIdRow;
        [SerializeField] private ProfileRowBinding levelRow;
        [SerializeField] private ProfileRowBinding rankRow;
        [SerializeField] private ProfileRowBinding locationRow;
        [SerializeField] private ProfileRowBinding dayAndTimeRow;
        [SerializeField] private ProfileRowBinding playTimeRow;
        [SerializeField] private ProfileRowBinding goldRow;
        [SerializeField] private ProfileRowBinding manaCrystalsRow;
        [SerializeField] private ProfileRowBinding demonCrystalsRow;
        [SerializeField] private ProfileRowBinding statusEffectsRow;
        [SerializeField] private ProfileRowBinding realTimePlayedRow;
        [SerializeField] private ProfileRowBinding battlesFinishedRow;
        [SerializeField] private ProfileRowBinding battlesWonRow;
        [SerializeField] private ProfileRowBinding battlesLostRow;
        [SerializeField] private ProfileRowBinding battlesSurrenderedRow;
        [SerializeField] private ProfileRowBinding escapesSuccessfulRow;
        [SerializeField] private ProfileRowBinding escapesFailedRow;
        [SerializeField] private ProfileRowBinding totalMobKillsRow;
        [SerializeField] private ProfileRowBinding totalGoldEarnedRow;
        [SerializeField] private ProfileRowBinding totalExpEarnedRow;
        [SerializeField] private ProfileRowBinding physicalDamageRow;
        [SerializeField] private ProfileRowBinding magicDamageRow;
        [SerializeField] private ProfileRowBinding physicalResistanceRow;
        [SerializeField] private ProfileRowBinding magicResistanceRow;
        [SerializeField] private ProfileRowBinding attackSpeedRow;
        [SerializeField] private ProfileRowBinding critChanceRow;
        [SerializeField] private ProfileRowBinding critMultiplierRow;
        [SerializeField] private ProfileRowBinding evasionChanceRow;
        [SerializeField] private ProfileRowBinding hitChanceRow;

        [Header("Localization")]
        [SerializeField] private bool refreshOnLanguageChange = true;
        [SerializeField] private string noStatusEffectsKey = "profile_no_status_effects";
        [SerializeField] private string statNotImplementedKey = "profile_stat_not_implemented";

        [Header("Auto Build From Template")]
        [SerializeField] private bool autoBuildRowsFromTemplate;
        [SerializeField] private Transform autoRowsContainer;
        [SerializeField] private GameObject autoRowTemplate;
        [SerializeField] private bool clearContainerBeforeBuild = true;
        [SerializeField] private string autoTitleObjectName = "content_title";
        [SerializeField] private string autoValueObjectName = "content_value";
        [SerializeField] private bool forceStretchRowWidth = true;
        [SerializeField] private bool forceStretchInnerContent = true;
        [SerializeField, Range(0.1f, 0.9f)] private float titleWidthRatio = 0.45f;
        [SerializeField] private bool forceInnerLayoutElements = true;

        [Header("Combat Section (Optional)")]
        [SerializeField] private bool useSeparateCombatSection;
        [SerializeField] private Transform combatRowsContainer;
        [SerializeField] private GameObject combatRowTemplate;
        [SerializeField] private bool clearCombatContainerBeforeBuild = true;
        [SerializeField] private string combatTitleObjectName = "content_title";
        [SerializeField] private string combatValueObjectName = "content_value";

        [Header("Formatting")]
        [SerializeField] private string notAvailableText = "-";
        [SerializeField] private string noStatusEffectsText = "No status effects";
        [SerializeField] private string statNotImplementedText = "-";

        [Header("Behavior")]
        [SerializeField] private bool refreshOnEnable = true;
        [SerializeField] private bool refreshWhileVisible = true;
        [SerializeField, Min(0.1f)] private float refreshIntervalSeconds = 1f;

        private Coroutine delayedLayoutRebuildRoutine;
        private float refreshTimer;

        private void OnEnable()
        {
            if (autoBuildRowsFromTemplate)
                EnsureAutoRowsBuilt();

            EnsureConfiguredRowsInnerContentLayout();
            refreshTimer = 0f;

            if (refreshOnLanguageChange)
                SettingsContext.OnLanguageChanged += HandleLanguageChanged;

            if (refreshOnEnable)
                RefreshFromCurrentSave();
        }

        [ContextMenu("Rebuild Auto Rows Now")]
        private void RebuildAutoRowsNow()
        {
            ClearAutoRowsContainer();
            autoRows.Clear();
            EnsureAutoRowsBuilt();
            RefreshFromCurrentSave();
        }

        private void OnDisable()
        {
            if (refreshOnLanguageChange)
                SettingsContext.OnLanguageChanged -= HandleLanguageChanged;

            if (delayedLayoutRebuildRoutine != null)
            {
                StopCoroutine(delayedLayoutRebuildRoutine);
                delayedLayoutRebuildRoutine = null;
            }

            refreshTimer = 0f;
        }

        private void Update()
        {
            if (!refreshWhileVisible)
                return;

            refreshTimer += Mathf.Max(0f, Time.unscaledDeltaTime);
            if (refreshTimer < refreshIntervalSeconds)
                return;

            refreshTimer = 0f;
            RefreshFromCurrentSave();
        }

        private void HandleLanguageChanged(string _)
        {
            RefreshFromCurrentSave();
        }

        public void RefreshFromCurrentSave()
        {
            var save = global::GameState.Instance != null ? global::GameState.Instance.CurrentSave : null;
            Refresh(save);
        }

        public void Refresh(SaveData save)
        {
            var player = save != null ? save.player : null;
            var meta = save != null ? save.meta : null;
            var time = save != null ? save.time : null;
            var inventory = save != null ? save.inventory : null;
            var progress = save != null ? save.progress : null;
            var achievementStats = save != null ? save.achievementStats : null;
            string plannedStatsPlaceholder = GetLocalizedOrFallback(statNotImplementedKey, statNotImplementedText);

            SetRowValue(GetRow(ProfileRowId.PlayerName, playerNameRow), EmptyToFallback(player != null ? player.name : null));
            SetRowValue(GetRow(ProfileRowId.PlayerId, playerIdRow), EmptyToFallback(player != null ? player.id : null));
            SetRowValue(GetRow(ProfileRowId.Level, levelRow), player != null ? player.level.ToString() : notAvailableText);
            SetRowValue(GetRow(ProfileRowId.Rank, rankRow), GetRankText(progress));
            SetRowValue(GetRow(ProfileRowId.Location, locationRow), EmptyToFallback(player != null ? player.sceneName : null));
            SetRowValue(GetRow(ProfileRowId.DayAndTime, dayAndTimeRow), BuildDayAndTime(time));
            SetRowValue(GetRow(ProfileRowId.PlayTime, playTimeRow), FormatPlayTime(meta != null ? meta.playTimeSeconds : 0));
            SetRowValue(GetRow(ProfileRowId.Gold, goldRow), inventory != null ? inventory.gold.ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.ManaCrystals, manaCrystalsRow), inventory != null ? inventory.manaCrystals.ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.DemonCrystals, demonCrystalsRow), inventory != null ? inventory.demonCrystals.ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.StatusEffects, statusEffectsRow), BuildStatusEffectsText(player));

            SetRowValue(GetRow(ProfileRowId.RealTimePlayed, realTimePlayedRow), FormatPlayTime(achievementStats != null ? achievementStats.realTimePlayedSeconds : 0));
            SetRowValue(GetRow(ProfileRowId.BattlesFinished, battlesFinishedRow), achievementStats != null ? Mathf.Max(0, achievementStats.battlesFinished).ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.BattlesWon, battlesWonRow), achievementStats != null ? Mathf.Max(0, achievementStats.battlesWon).ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.BattlesLost, battlesLostRow), achievementStats != null ? Mathf.Max(0, achievementStats.battlesLost).ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.BattlesSurrendered, battlesSurrenderedRow), achievementStats != null ? Mathf.Max(0, achievementStats.battlesSurrendered).ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.EscapesSuccessful, escapesSuccessfulRow), achievementStats != null ? Mathf.Max(0, achievementStats.escapesSuccessful).ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.EscapesFailed, escapesFailedRow), achievementStats != null ? Mathf.Max(0, achievementStats.escapesFailed).ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.TotalMobKills, totalMobKillsRow), achievementStats != null ? Mathf.Max(0, achievementStats.totalMobKills).ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.TotalGoldEarned, totalGoldEarnedRow), achievementStats != null ? Mathf.Max(0, achievementStats.totalGoldEarned).ToString() : "0");
            SetRowValue(GetRow(ProfileRowId.TotalExpEarned, totalExpEarnedRow), achievementStats != null ? Mathf.Max(0, achievementStats.totalExpEarned).ToString() : "0");

            // Planned combat stats: placeholders until combat formulas and save fields are implemented.
            SetRowValue(GetRow(ProfileRowId.PhysicalDamage, physicalDamageRow), plannedStatsPlaceholder);
            SetRowValue(GetRow(ProfileRowId.MagicDamage, magicDamageRow), plannedStatsPlaceholder);
            SetRowValue(GetRow(ProfileRowId.PhysicalResistance, physicalResistanceRow), plannedStatsPlaceholder);
            SetRowValue(GetRow(ProfileRowId.MagicResistance, magicResistanceRow), plannedStatsPlaceholder);
            SetRowValue(GetRow(ProfileRowId.AttackSpeed, attackSpeedRow), plannedStatsPlaceholder);
            SetRowValue(GetRow(ProfileRowId.CritChance, critChanceRow), plannedStatsPlaceholder);
            SetRowValue(GetRow(ProfileRowId.CritMultiplier, critMultiplierRow), plannedStatsPlaceholder);
            SetRowValue(GetRow(ProfileRowId.EvasionChance, evasionChanceRow), plannedStatsPlaceholder);
            SetRowValue(GetRow(ProfileRowId.HitChance, hitChanceRow), plannedStatsPlaceholder);
        }

        private ProfileRowBinding GetRow(ProfileRowId id, ProfileRowBinding fallback)
        {
            if (autoRows.TryGetValue(id, out var row) && row != null)
                return row;

            return fallback;
        }

        private void EnsureAutoRowsBuilt()
        {
            if (!autoBuildRowsFromTemplate)
                return;

            if (autoRowsContainer == null || autoRowTemplate == null)
                return;

            if (HasValidAutoRows())
                return;

            autoRows.Clear();

            if (clearContainerBeforeBuild)
                ClearAutoRowsContainer();

            if (useSeparateCombatSection && clearCombatContainerBeforeBuild)
                ClearCombatRowsContainer();

            for (int i = 0; i < AutoTemplateSpecs.Length; i++)
            {
                var spec = AutoTemplateSpecs[i];
                var targetContainer = ResolveContainerForSpec(spec);
                var template = ResolveTemplateForSpec(spec);
                var titleNodeName = ResolveTitleNodeNameForSpec(spec);
                var valueNodeName = ResolveValueNodeNameForSpec(spec);

                if (targetContainer == null || template == null)
                    continue;

                var rowGo = Instantiate(template, targetContainer, worldPositionStays: false);
                rowGo.name = $"row_{spec.Id}";
                rowGo.SetActive(true);
                if (forceStretchRowWidth)
                    StretchRowToContainerWidth(rowGo.transform as RectTransform);
                EnsureRowComponentsEnabled(rowGo.transform);

                var binding = ProfileRowBinding.Create(rowGo.transform, titleNodeName, valueNodeName);
                EnsureInnerContentLayout(binding);
                ApplyLocalizedTitleKey(binding, spec.TitleLocalizationKey);
                autoRows[spec.Id] = binding;
            }

            RebuildContainerLayoutNow();

            if (delayedLayoutRebuildRoutine != null)
                StopCoroutine(delayedLayoutRebuildRoutine);
            delayedLayoutRebuildRoutine = StartCoroutine(RebuildContainerLayoutNextFrame());
        }

        private void EnsureConfiguredRowsInnerContentLayout()
        {
            EnsureInnerContentLayout(playerNameRow);
            EnsureInnerContentLayout(playerIdRow);
            EnsureInnerContentLayout(levelRow);
            EnsureInnerContentLayout(rankRow);
            EnsureInnerContentLayout(locationRow);
            EnsureInnerContentLayout(dayAndTimeRow);
            EnsureInnerContentLayout(playTimeRow);
            EnsureInnerContentLayout(goldRow);
            EnsureInnerContentLayout(manaCrystalsRow);
            EnsureInnerContentLayout(demonCrystalsRow);
            EnsureInnerContentLayout(statusEffectsRow);
            EnsureInnerContentLayout(realTimePlayedRow);
            EnsureInnerContentLayout(battlesFinishedRow);
            EnsureInnerContentLayout(battlesWonRow);
            EnsureInnerContentLayout(battlesLostRow);
            EnsureInnerContentLayout(battlesSurrenderedRow);
            EnsureInnerContentLayout(escapesSuccessfulRow);
            EnsureInnerContentLayout(escapesFailedRow);
            EnsureInnerContentLayout(totalMobKillsRow);
            EnsureInnerContentLayout(totalGoldEarnedRow);
            EnsureInnerContentLayout(totalExpEarnedRow);
            EnsureInnerContentLayout(physicalDamageRow);
            EnsureInnerContentLayout(magicDamageRow);
            EnsureInnerContentLayout(physicalResistanceRow);
            EnsureInnerContentLayout(magicResistanceRow);
            EnsureInnerContentLayout(attackSpeedRow);
            EnsureInnerContentLayout(critChanceRow);
            EnsureInnerContentLayout(critMultiplierRow);
            EnsureInnerContentLayout(evasionChanceRow);
            EnsureInnerContentLayout(hitChanceRow);

            foreach (var kv in autoRows)
                EnsureInnerContentLayout(kv.Value);
        }

        private void EnsureInnerContentLayout(ProfileRowBinding row)
        {
            if (row == null)
                return;

            var title = row.GetTitleText();
            var value = row.GetValueText();
            if (title == null || value == null)
                return;

            var titleRect = title.rectTransform;
            var valueRect = value.rectTransform;

            if (forceStretchInnerContent)
            {
                float split = Mathf.Clamp01(titleWidthRatio);
                StretchRectX(titleRect, 0f, split);
                StretchRectX(valueRect, split, 1f);
            }

            if (forceInnerLayoutElements)
            {
                ForceFlexibleLayout(title.gameObject, 1f);
                ForceFlexibleLayout(value.gameObject, 1f);
            }
        }

        private static void StretchRectX(RectTransform rt, float minX, float maxX)
        {
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(minX, rt.anchorMin.y);
            rt.anchorMax = new Vector2(maxX, rt.anchorMax.y);
            rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
        }

        private static void ForceFlexibleLayout(GameObject go, float flexibleWidth)
        {
            if (go == null)
                return;

            var le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();

            le.ignoreLayout = false;
            le.minWidth = 0f;
            le.preferredWidth = -1f;
            le.flexibleWidth = Mathf.Max(0f, flexibleWidth);
        }

        private static void StretchRowToContainerWidth(RectTransform rt)
        {
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
            rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
            rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);
        }

        private void RebuildContainerLayoutNow()
        {
            Canvas.ForceUpdateCanvases();

            if (autoRowsContainer is RectTransform containerRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

                if (containerRect.parent is RectTransform parentRect)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            if (useSeparateCombatSection && combatRowsContainer is RectTransform combatRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(combatRect);

                if (combatRect.parent is RectTransform combatParentRect)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(combatParentRect);
            }
        }

        private System.Collections.IEnumerator RebuildContainerLayoutNextFrame()
        {
            yield return null;
            RebuildContainerLayoutNow();
            delayedLayoutRebuildRoutine = null;
        }

        private bool HasValidAutoRows()
        {
            if (autoRows.Count != AutoTemplateSpecs.Length)
                return false;

            foreach (var spec in AutoTemplateSpecs)
            {
                if (!autoRows.TryGetValue(spec.Id, out var row) || row == null)
                    return false;

                if (row.GetValueText() == null)
                    return false;
            }

            return true;
        }

        private void ClearAutoRowsContainer()
        {
            if (autoRowsContainer == null)
                return;

            for (int i = autoRowsContainer.childCount - 1; i >= 0; i--)
            {
                var child = autoRowsContainer.GetChild(i);
                if (child == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void ClearCombatRowsContainer()
        {
            if (combatRowsContainer == null)
                return;

            for (int i = combatRowsContainer.childCount - 1; i >= 0; i--)
            {
                var child = combatRowsContainer.GetChild(i);
                if (child == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private Transform ResolveContainerForSpec(RowTemplateSpec spec)
        {
            if (spec.IsCombatSection && useSeparateCombatSection && combatRowsContainer != null)
                return combatRowsContainer;

            return autoRowsContainer;
        }

        private GameObject ResolveTemplateForSpec(RowTemplateSpec spec)
        {
            if (spec.IsCombatSection && useSeparateCombatSection && combatRowTemplate != null)
                return combatRowTemplate;

            return autoRowTemplate;
        }

        private string ResolveTitleNodeNameForSpec(RowTemplateSpec spec)
        {
            if (spec.IsCombatSection && useSeparateCombatSection)
                return string.IsNullOrWhiteSpace(combatTitleObjectName) ? autoTitleObjectName : combatTitleObjectName;

            return autoTitleObjectName;
        }

        private string ResolveValueNodeNameForSpec(RowTemplateSpec spec)
        {
            if (spec.IsCombatSection && useSeparateCombatSection)
                return string.IsNullOrWhiteSpace(combatValueObjectName) ? autoValueObjectName : combatValueObjectName;

            return autoValueObjectName;
        }

        private string BuildDayAndTime(SaveData.TimeState time)
        {
            if (time == null)
                return notAvailableText;

            int day = Mathf.Max(1, time.day);
            int minuteOfDay = Mathf.Clamp(time.minuteOfDay, 0, 1439);
            int hours = minuteOfDay / 60;
            int minutes = minuteOfDay % 60;

            return $"{day}  {hours:00}:{minutes:00}";
        }

        private string BuildStatusEffectsText(SaveData.Player player)
        {
            if (player == null || player.statusEffects == null || player.statusEffects.Count == 0)
                return GetLocalizedOrFallback(noStatusEffectsKey, noStatusEffectsText);

            return string.Join(", ", player.statusEffects);
        }

        private string GetRankText(SaveData.Progress progress)
        {
            if (progress == null)
                return notAvailableText;

            return progress.adventurerRank.ToString();
        }

        private string EmptyToFallback(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? notAvailableText : value;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static void SetRowValue(ProfileRowBinding row, string value)
        {
            if (row == null)
                return;

            SetText(row.GetValueText(), value);
        }

        private static void ApplyLocalizedTitleKey(ProfileRowBinding row, string localizationKey)
        {
            if (row == null || string.IsNullOrWhiteSpace(localizationKey))
                return;

            var titleText = row.GetTitleText();
            if (titleText == null)
                return;

            if (!TryResolveLocalizedGlobalComponentType())
                return;

            var component = titleText.GetComponent(localizedGlobalComponentType);
            if (component == null)
                component = titleText.gameObject.AddComponent(localizedGlobalComponentType);

            if (component is Behaviour behaviour)
                behaviour.enabled = true;

            localizedGlobalComponentKeyProperty?.SetValue(component, localizationKey);
        }

        private static void EnsureRowComponentsEnabled(Transform rowRoot)
        {
            if (rowRoot == null)
                return;

            var texts = rowRoot.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                    texts[i].enabled = true;
            }

            var layouts = rowRoot.GetComponentsInChildren<LayoutElement>(includeInactive: true);
            for (int i = 0; i < layouts.Length; i++)
            {
                if (layouts[i] != null)
                    layouts[i].enabled = true;
            }

            if (!TryResolveLocalizedGlobalComponentType())
                return;

            var localizedBehaviours = rowRoot.GetComponentsInChildren(localizedGlobalComponentType, includeInactive: true);
            for (int i = 0; i < localizedBehaviours.Length; i++)
            {
                if (localizedBehaviours[i] is Behaviour b)
                    b.enabled = true;
            }
        }

        private static bool TryResolveLocalizedGlobalComponentType()
        {
            if (localizationDriverTypeResolved)
                return localizedGlobalComponentType != null && localizedGlobalComponentKeyProperty != null;

            localizationDriverTypeResolved = true;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var asm = assemblies[i];
                if (asm == null)
                    continue;

                var type = asm.GetType("LocalizedGlobalComponent", throwOnError: false);
                if (type == null)
                    continue;

                var keyProp = type.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
                if (keyProp == null || !keyProp.CanWrite)
                    continue;

                localizedGlobalComponentType = type;
                localizedGlobalComponentKeyProperty = keyProp;
                break;
            }

            return localizedGlobalComponentType != null && localizedGlobalComponentKeyProperty != null;
        }

        private string GetLocalizedOrFallback(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback;

            string localized = LocalizationManager.Get(key);
            return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
                ? fallback
                : localized;
        }

        private static string FormatPlayTime(int totalSeconds)
        {
            int safeSeconds = Mathf.Max(0, totalSeconds);
            TimeSpan span = TimeSpan.FromSeconds(safeSeconds);

            if (span.TotalHours >= 100)
                return $"{(int)span.TotalHours:0}:{span.Minutes:00}:{span.Seconds:00}";

            return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
        }
    }
}