using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UDA2.UI.Game;
using Random = UnityEngine.Random;

namespace Game.Battle.UI
{
    /// <summary>
    /// Simple modal for showing battle results.
    /// UI-only component, no combat logic.
    /// </summary>
    public sealed class BattleResultModalController : MonoBehaviour
    {
        [Header("Wiring + Data Sources")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text leftTipText;
        [SerializeField] private TMP_Text rightTipText;
        [SerializeField] private Image topSeparatorImage;
        [SerializeField] private BattleResultOutcomeUiCatalogAsset outcomeUiCatalog;

        [Header("Items Data Source (Optional)")]
        [Tooltip("Optional. Assign ItemDatabase asset to resolve tooltip/name/rarity. Kept as Object to avoid assembly reference coupling.")]
        [SerializeField] private UnityEngine.Object itemDatabase;

        [Header("Optional Prefab Loot List")]
        [Tooltip("ScrollView Content transform (with GridLayoutGroup). If assigned, modal will spawn slots here.")]
        [SerializeField] private Transform rewardsContent;

        [Tooltip("Prefab with InventoryItemSlotView for icon + count.")]
        [SerializeField] private GameObject rewardSlotPrefab;

        [Header("Outcome Presentation (Optional)")]
        [SerializeField] private BattleOutcomePresentationCatalogAsset presentationCatalog;
        [SerializeField] private Image outcomeImage;
        [SerializeField] private Image playerImage;
        [SerializeField] private Image enemyImage;
        [SerializeField] private bool showCharacterAnimationsAsStaticImages = true;
        [SerializeField] private bool enablePresentationDebugLogs = true;

        [Header("Ordering")]
        [Tooltip("If true, EXP will be spawned as a reward entry with id 'exp'.")]
        [SerializeField] private bool showExpAsSlot = true;

        [Header("Achievements (Optional)")]
        [Tooltip("Whole achievements block root. Shown only if at least one achievement was unlocked in this battle.")]
        [SerializeField] private GameObject achievementsBlock;

        [Tooltip("Catalog for resolving achievement id -> icon/title/description.")]
        [SerializeField] private BattleAchievementCatalogAsset achievementCatalog;

        [Tooltip("Scroll content where unlocked achievement items are spawned.")]
        [SerializeField] private Transform achievementsContent;

        [Tooltip("Template view for one achievement item (can stay disabled in hierarchy).")]
        [SerializeField] private BattleResultAchievementItemView achievementItemTemplate;

        [Header("Battle Summary")]
        [Tooltip("Optional text field for battle duration (mm:ss).")]
        [SerializeField] private TMP_Text battleDurationText;

        [Tooltip("Optional text field for total damage dealt to enemy in this battle.")]
        [SerializeField] private TMP_Text totalDamageDealtText;

        [Tooltip("Optional text field for total damage taken from enemy in this battle.")]
        [SerializeField] private TMP_Text totalDamageTakenText;

        [Header("Controllers")]
        [Tooltip("Confirmation button that closes the result modal and continues flow.")]
        [SerializeField] private Button okButton;

        private Action onOk;
        private bool _suppressHideOnAwake;
        private int _lastLeftTipIndex = -1;
        private int _lastRightTipIndex = -1;
        private Color _defaultTitleColor = Color.white;
        private Color _defaultLeftTipColor = Color.white;
        private Color _defaultRightTipColor = Color.white;

        public void SetItemDatabase(UnityEngine.Object db)
        {
            if (db == null)
                return;

            if (!IsCompatibleItemDatabase(db))
                return;

            if (itemDatabase == null || !IsCompatibleItemDatabase(itemDatabase))
                itemDatabase = db;
        }

        public void SetAchievementCatalog(BattleAchievementCatalogAsset catalog)
        {
            if (catalog != null)
                achievementCatalog = catalog;
        }

        private static bool IsCompatibleItemDatabase(UnityEngine.Object db)
        {
            if (db == null)
                return false;

            try
            {
                var dbType = db.GetType();
                var getById = dbType.GetMethod("GetById");
                return getById != null;
            }
            catch
            {
                return false;
            }
        }

        private void Awake()
        {
            AutoWireIfMissing();
            CacheDefaultTextColors();

            if (okButton != null)
                okButton.onClick.AddListener(OnOkClicked);

            if (!_suppressHideOnAwake)
                Hide();
        }

        public void Show(
            Game.Battle.BattleResultData data,
            Action onOkClicked,
            Game.Battle.BattleFinishReason reason = Game.Battle.BattleFinishReason.Defeat,
            string enemyId = null,
            string locationId = null,
            string sourceLocationId = null,
            Sprite fallbackLocationBackground = null)
        {
            onOk = onOkClicked;

            // This modal might be instantiated hidden (inactive GameObject). Ensure it's enabled before toggling child roots.
            _suppressHideOnAwake = true;
            gameObject.SetActive(true);
            _suppressHideOnAwake = false;

            // Bring to front so it isn't hidden behind other panels.
            transform.SetAsLastSibling();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UDA2.Logging.Logger.LogInfo($"[BattleResultModal] Show called. Won={data.PlayerWon}, Gold={data.GoldGained}, Items={(data.Items != null ? data.Items.Count : 0)}", UDA2.Logging.LogChannel.UI, this);
#endif

            AutoWireIfMissing();

            ApplyOutcomeUi(reason, data);
            ApplyBattleSummary(data);
            ApplyAchievements(data);

            ApplyOutcomePresentation(reason, enemyId, locationId, sourceLocationId, fallbackLocationBackground);


            // Prefab list: we show ONLY slots (currencies first, then items).
            if (rewardsContent != null && rewardSlotPrefab != null)
            {
                RenderRewardsAsSlots(data);
            }
            else
            {
                Debug.LogWarning("[BattleResultModal] Rewards list is not wired (rewardsContent or rewardSlotPrefab is null). No reward slots will be spawned.", this);
            }

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        private void ApplyOutcomeUi(Game.Battle.BattleFinishReason reason, Game.Battle.BattleResultData data)
        {
            string titleKey = ResolveDefaultTitleKey(data);

            Sprite separator = null;
            Color? titleColorOverride = null;
            LocalizedTextStyle leftTipStyle = LocalizedTextStyle.Empty;
            LocalizedTextStyle rightTipStyle = LocalizedTextStyle.Empty;

            if (outcomeUiCatalog != null && outcomeUiCatalog.TryGet(reason, out var entry) && entry != null)
            {
                if (!string.IsNullOrWhiteSpace(entry.titleLocalizationKey))
                    titleKey = entry.titleLocalizationKey.Trim();

                separator = entry.topSeparator;

                if (entry.useCustomTitleColor)
                    titleColorOverride = entry.titleColor;

                leftTipStyle = PickRandomTipStyle(
                    entry.leftTipVariants,
                    entry.leftTipLocalizationKeys,
                    ref _lastLeftTipIndex);

                rightTipStyle = PickRandomTipStyle(
                    entry.rightTipVariants,
                    entry.rightTipLocalizationKeys,
                    ref _lastRightTipIndex);
            }

            ApplyLocalizedText(titleText, titleKey, data.PlayerWon ? "Victory" : "Defeat");
            ApplyTextColor(titleText, titleColorOverride, _defaultTitleColor);

            ApplyLocalizedText(leftTipText, leftTipStyle.Key, string.Empty);
            ApplyTextColor(leftTipText, leftTipStyle.HasColor ? leftTipStyle.Color : (Color?)null, _defaultLeftTipColor);

            ApplyLocalizedText(rightTipText, rightTipStyle.Key, string.Empty);
            ApplyTextColor(rightTipText, rightTipStyle.HasColor ? rightTipStyle.Color : (Color?)null, _defaultRightTipColor);

            if (topSeparatorImage != null)
            {
                topSeparatorImage.sprite = separator;
                topSeparatorImage.enabled = topSeparatorImage.sprite != null;
            }
        }

        private static string ResolveDefaultTitleKey(Game.Battle.BattleResultData data)
        {
            // Prefer 'lose', fallback to legacy 'loes' in ApplyLocalizedText.
            return data.PlayerWon ? "victory" : "lose";
        }

        private void CacheDefaultTextColors()
        {
            if (titleText != null)
                _defaultTitleColor = titleText.color;

            if (leftTipText != null)
                _defaultLeftTipColor = leftTipText.color;

            if (rightTipText != null)
                _defaultRightTipColor = rightTipText.color;
        }

        private static void ApplyTextColor(TMP_Text target, Color? overrideColor, Color defaultColor)
        {
            if (target == null)
                return;

            target.color = overrideColor ?? defaultColor;
        }

        private static void ApplyLocalizedText(TMP_Text target, string key, string fallback)
        {
            if (target == null)
                return;

            if (string.IsNullOrWhiteSpace(key))
            {
                target.text = fallback ?? string.Empty;
                return;
            }

            var normalized = key.Trim();

            var localized = target.GetComponent<LocalizedGlobalComponent>();
            if (localized != null)
            {
                localized.Key = normalized;
                localized.ClearArgs();
                localized.UpdateText();

                if (string.Equals(normalized, "lose", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(target.text, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    localized.Key = "loes";
                    localized.UpdateText();
                }

                return;
            }

            var resolved = UDA2.Core.LocalizationManager.Get(normalized);
            if (string.IsNullOrWhiteSpace(resolved) || string.Equals(resolved, normalized, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(normalized, "lose", StringComparison.OrdinalIgnoreCase))
                {
                    var legacy = UDA2.Core.LocalizationManager.Get("loes");
                    target.text = !string.IsNullOrWhiteSpace(legacy) && !string.Equals(legacy, "loes", StringComparison.OrdinalIgnoreCase)
                        ? legacy
                        : fallback ?? string.Empty;
                    return;
                }

                target.text = fallback ?? normalized;
                return;
            }

            target.text = resolved;
        }

        private static string PickRandomKey(System.Collections.Generic.IReadOnlyList<string> keys, ref int lastIndex)
        {
            if (keys == null || keys.Count == 0)
            {
                lastIndex = -1;
                return null;
            }

            if (keys.Count == 1)
            {
                lastIndex = 0;
                return string.IsNullOrWhiteSpace(keys[0]) ? null : keys[0].Trim();
            }

            int index = UnityEngine.Random.Range(0, keys.Count);
            if (index == lastIndex)
                index = (index + 1) % keys.Count;

            lastIndex = index;
            return string.IsNullOrWhiteSpace(keys[index]) ? null : keys[index].Trim();
        }

        private static LocalizedTextStyle PickRandomTipStyle(
            System.Collections.Generic.IReadOnlyList<BattleResultOutcomeUiCatalogAsset.TipVariant> variants,
            System.Collections.Generic.IReadOnlyList<string> legacyKeys,
            ref int lastIndex)
        {
            if (legacyKeys != null && legacyKeys.Count > 1)
            {
                var randomKey = PickRandomKey(legacyKeys, ref lastIndex);
                return string.IsNullOrWhiteSpace(randomKey)
                    ? LocalizedTextStyle.Empty
                    : new LocalizedTextStyle(randomKey, hasColor: false, Color.white);
            }

            if (variants != null && variants.Count > 0)
            {
                int index = PickRandomIndex(variants.Count, ref lastIndex);
                if (index < 0)
                    return LocalizedTextStyle.Empty;

                var selected = variants[index];
                if (selected == null || string.IsNullOrWhiteSpace(selected.localizationKey))
                    return LocalizedTextStyle.Empty;

                return new LocalizedTextStyle(
                    selected.localizationKey.Trim(),
                    selected.useCustomColor,
                    selected.color);
            }

            return LocalizedTextStyle.Empty;
        }

        private static int PickRandomIndex(int count, ref int lastIndex)
        {
            if (count <= 0)
            {
                lastIndex = -1;
                return -1;
            }

            if (count == 1)
            {
                lastIndex = 0;
                return 0;
            }

            int index = UnityEngine.Random.Range(0, count);
            if (index == lastIndex)
                index = (index + 1) % count;

            lastIndex = index;
            return index;
        }

        private readonly struct LocalizedTextStyle
        {
            public static readonly LocalizedTextStyle Empty = new LocalizedTextStyle(null, false, Color.white);

            public readonly string Key;
            public readonly bool HasColor;
            public readonly Color Color;

            public LocalizedTextStyle(string key, bool hasColor, Color color)
            {
                Key = key;
                HasColor = hasColor;
                Color = color;
            }
        }

        private void RenderRewardsAsSlots(Game.Battle.BattleResultData data)
        {
            ClearChildren(rewardsContent);


            // Currency first (always in fixed order). The slot view decides how to render each id.
            if (data.GoldGained > 0)
                SpawnSlot("gold", data.GoldGained);

            if (data.ManaCrystalsGained > 0)
                SpawnSlot("mana_crystal", data.ManaCrystalsGained);
            if (data.DemonCrystalsGained > 0)
                SpawnSlot("demon_crystal", data.DemonCrystalsGained);

            // EXP as a pseudo-item entry.
            if (showExpAsSlot && data.ExpGained > 0)
                SpawnSlot("exp", data.ExpGained);

            // Then normal items.
            if (data.Items != null && data.Items.Count > 0)
            {
                for (int i = 0; i < data.Items.Count; i++)
                {
                    var r = data.Items[i];
                    if (string.IsNullOrWhiteSpace(r.ItemId) || r.Count <= 0)
                        continue;

                    SpawnSlot(r.ItemId.Trim(), r.Count);
                }
            }

            RefreshRewardsScrollableLayout();
        }

        private void AutoWireIfMissing()
        {
            if (root == null)
                root = gameObject;

            if (titleText == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    var t = texts[i];
                    if (t == null) continue;
                    if (t.name.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        titleText = t;
                        break;
                    }
                }
            }

            if (leftTipText == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    var t = texts[i];
                    if (t == null || t == titleText)
                        continue;

                    var n = t.name;
                    if (n.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0
                        && (n.IndexOf("tip", StringComparison.OrdinalIgnoreCase) >= 0
                            || n.IndexOf("advice", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        leftTipText = t;
                        break;
                    }
                }
            }

            if (rightTipText == null)
            {
                var texts = GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    var t = texts[i];
                    if (t == null || t == titleText || t == leftTipText)
                        continue;

                    var n = t.name;
                    if (n.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0
                        && (n.IndexOf("tip", StringComparison.OrdinalIgnoreCase) >= 0
                            || n.IndexOf("advice", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        rightTipText = t;
                        break;
                    }
                }
            }

            if (topSeparatorImage == null)
            {
                var images = GetComponentsInChildren<Image>(includeInactive: true);
                for (int i = 0; i < images.Length; i++)
                {
                    var img = images[i];
                    if (img == null || img == GetComponent<Image>())
                        continue;

                    var n = img.name;
                    if (n.IndexOf("separator", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("top_separator", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        topSeparatorImage = img;
                        break;
                    }
                }
            }

            if (okButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    var b = buttons[i];
                    if (b == null) continue;

                    var n = b.name;
                    if (n.IndexOf("ok", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("confirm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("continue", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        okButton = b;
                        break;
                    }
                }

                if (okButton == null)
                {
                    // Fallback: if there's exactly one button, it's probably OK.
                    if (buttons != null && buttons.Length == 1)
                        okButton = buttons[0];
                }
            }

            if (rewardsContent == null)
            {
                // Prefer reward/loot-specific containers and avoid generic "Content" (can be achievements scroll content).
                var rts = GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < rts.Length; i++)
                {
                    var rt = rts[i];
                    if (rt == null)
                        continue;

                    var n = rt.name;
                    bool looksLikeRewards =
                        n.IndexOf("reward", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("loot", StringComparison.OrdinalIgnoreCase) >= 0;

                    bool looksLikeContainer =
                        n.IndexOf("content", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("container", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (looksLikeRewards && looksLikeContainer)
                    {
                        rewardsContent = rt;
                        break;
                    }
                }
            }

            if (rewardSlotPrefab == null)
            {
                // Default: reuse inventory slot view prefab.
                var slot = Resources.Load<InventoryItemSlotView>("Prefabs/UI/Profile/Inventory/InventoryItemSlotView");
                if (slot != null)
                    rewardSlotPrefab = slot.gameObject;
            }

            if (outcomeImage == null)
            {
                var images = GetComponentsInChildren<Image>(includeInactive: true);
                for (int i = 0; i < images.Length; i++)
                {
                    var img = images[i];
                    if (img == null || img == GetComponent<Image>())
                        continue;

                    var n = img.name;
                    if (n.IndexOf("outcome", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("result", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        outcomeImage = img;
                        break;
                    }
                }
            }

            if (playerImage == null)
            {
                var images = GetComponentsInChildren<Image>(includeInactive: true);
                for (int i = 0; i < images.Length; i++)
                {
                    var img = images[i];
                    if (img == null || img == outcomeImage || img == GetComponent<Image>())
                        continue;

                    if (img.name.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        playerImage = img;
                        break;
                    }
                }
            }

            if (enemyImage == null)
            {
                var images = GetComponentsInChildren<Image>(includeInactive: true);
                for (int i = 0; i < images.Length; i++)
                {
                    var img = images[i];
                    if (img == null || img == outcomeImage || img == GetComponent<Image>())
                        continue;

                    if (img.name.IndexOf("enemy", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        enemyImage = img;
                        break;
                    }
                }
            }

            if (battleDurationText == null)
            {
                battleDurationText = FindValueTextByNameContains("duration", "time");
            }

            if (totalDamageDealtText == null)
            {
                totalDamageDealtText = FindValueTextByNameContains("dealt", "damage_dealt", "damage out");
            }

            if (totalDamageTakenText == null)
            {
                totalDamageTakenText = FindValueTextByNameContains("taken", "damage_taken", "damage in");
            }

            if (achievementsContent == null)
            {
                var rts = GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < rts.Length; i++)
                {
                    var rt = rts[i];
                    if (rt == null)
                        continue;

                    var n = rt.name;
                    if (n.IndexOf("achievement", StringComparison.OrdinalIgnoreCase) >= 0
                        && n.IndexOf("content", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        achievementsContent = rt;
                        break;
                    }
                }
            }

            if (achievementItemTemplate == null)
            {
                achievementItemTemplate = GetComponentInChildren<BattleResultAchievementItemView>(true);
            }

            if (achievementsContent == null && achievementItemTemplate != null)
            {
                var parent = achievementItemTemplate.transform.parent;
                if (parent != null)
                    achievementsContent = parent;
            }

            if (achievementsBlock == null && achievementsContent != null)
            {
                achievementsBlock = achievementsContent.gameObject;
            }
        }

        private TMP_Text FindValueTextByNameContains(params string[] tokens)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                if (t == null || t == titleText || t == leftTipText || t == rightTipText)
                    continue;

                // Do not bind value outputs to localized labels (e.g. time_in_battle_fmt).
                if (t.GetComponent<LocalizedGlobalComponent>() != null)
                    continue;

                var n = t.name;
                for (int j = 0; j < tokens.Length; j++)
                {
                    var token = tokens[j];
                    if (string.IsNullOrWhiteSpace(token))
                        continue;

                    if (n.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        return t;
                }
            }

            return null;
        }

        private void ApplyBattleSummary(Game.Battle.BattleResultData data)
        {
            if (data == null)
                return;

            if (battleDurationText != null && battleDurationText.GetComponent<LocalizedGlobalComponent>() == null)
                battleDurationText.text = FormatDuration(data.BattleDurationSeconds);

            if (totalDamageDealtText != null && totalDamageDealtText.GetComponent<LocalizedGlobalComponent>() == null)
                totalDamageDealtText.text = Mathf.Max(0, data.PlayerHpDamageDealt).ToString();

            if (totalDamageTakenText != null && totalDamageTakenText.GetComponent<LocalizedGlobalComponent>() == null)
                totalDamageTakenText.text = Mathf.Max(0, data.PlayerHpDamageTaken).ToString();
        }

        private static string FormatDuration(int totalSeconds)
        {
            totalSeconds = Mathf.Max(0, totalSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        private void ApplyAchievements(Game.Battle.BattleResultData data)
        {
            var ids = data != null ? data.NewlyUnlockedAchievementIds : null;
            bool hasAny = ids != null && ids.Count > 0;

            ClearAchievementItems();

            if (achievementsBlock != null)
                achievementsBlock.SetActive(hasAny);

            if (!hasAny || achievementsContent == null || achievementItemTemplate == null)
                return;

            int spawned = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var itemGo = Instantiate(achievementItemTemplate.gameObject);
                itemGo.transform.SetParent(achievementsContent, worldPositionStays: false);
                itemGo.SetActive(true);

                var itemView = itemGo.GetComponent<BattleResultAchievementItemView>();
                if (itemView == null)
                    continue;

                var trimmed = id.Trim();
                if (achievementCatalog != null && achievementCatalog.TryGetById(trimmed, out var definition))
                    itemView.Bind(definition);
                else
                    itemView.BindFallback(trimmed, trimmed, string.Empty, null);

                spawned++;
            }

            if (achievementsBlock != null)
                achievementsBlock.SetActive(spawned > 0);
        }

        private void ClearAchievementItems()
        {
            if (achievementsContent == null)
                return;

            for (int i = achievementsContent.childCount - 1; i >= 0; i--)
            {
                var child = achievementsContent.GetChild(i);
                if (child == null)
                    continue;

                if (achievementItemTemplate != null && child == achievementItemTemplate.transform)
                    continue;

                Destroy(child.gameObject);
            }

            if (achievementItemTemplate != null)
                achievementItemTemplate.gameObject.SetActive(false);
        }

        private void ApplyOutcomePresentation(
            Game.Battle.BattleFinishReason reason,
            string enemyId,
            string locationId,
            string sourceLocationId,
            Sprite fallbackLocationBackground)
        {
            if (outcomeImage == null && playerImage == null && enemyImage == null)
                return;

            ClearCharacterPresentation();

            var save = global::GameState.Instance?.CurrentSave;
            var resolvedEnemyId = !string.IsNullOrWhiteSpace(enemyId)
                ? enemyId
                : save?.sceneState?.pendingBattle?.enemyId;
            var resolvedLocationId = !string.IsNullOrWhiteSpace(locationId)
                ? locationId
                : save?.sceneState?.pendingBattle?.locationId;
            var resolvedSourceLocationId = !string.IsNullOrWhiteSpace(sourceLocationId)
                ? sourceLocationId
                : save?.sceneState?.pendingBattle?.locationId;
            var normalizedLocationId = NormalizeLocationId(resolvedLocationId);
            var normalizedSourceLocationId = NormalizeLocationId(resolvedSourceLocationId);

            if (presentationCatalog == null)
            {
                if (outcomeImage != null)
                {
                    outcomeImage.sprite = fallbackLocationBackground;
                    outcomeImage.enabled = outcomeImage.sprite != null;
                }
                return;
            }

            var resolvedFallback = presentationCatalog.ResolveLocationFallbackSprite(
                normalizedLocationId,
                normalizedSourceLocationId,
                fallbackLocationBackground,
                out _);

            var variants = presentationCatalog.ResolveVariants(
                reason,
                resolvedEnemyId,
                normalizedLocationId,
                normalizedSourceLocationId,
                save,
                debugLogs: enablePresentationDebugLogs,
                out _);

            if (variants == null || variants.Count == 0)
            {
                if (outcomeImage != null)
                {
                    outcomeImage.sprite = resolvedFallback;
                    outcomeImage.enabled = outcomeImage.sprite != null;
                }
                return;
            }

            var selected = SelectWeightedVariant(variants);
            if (selected == null)
            {
                if (outcomeImage != null)
                {
                    outcomeImage.sprite = resolvedFallback;
                    outcomeImage.enabled = outcomeImage.sprite != null;
                }
                return;
            }

            var variantSprite = presentationCatalog.UseVariantSpriteOverrides ? selected.sprite : null;
            if (outcomeImage != null)
            {
                outcomeImage.sprite = variantSprite != null ? variantSprite : resolvedFallback;
                outcomeImage.enabled = outcomeImage.sprite != null;
            }

            if (showCharacterAnimationsAsStaticImages)
            {
                ApplyAnimationFirstFrameToImage(selected.playerAnimation, playerImage);
                ApplyAnimationFirstFrameToImage(selected.enemyAnimation, enemyImage);
            }
        }

        private void ClearCharacterPresentation()
        {
            if (!showCharacterAnimationsAsStaticImages)
                return;

            if (playerImage != null)
            {
                playerImage.sprite = null;
                playerImage.enabled = false;
            }

            if (enemyImage != null)
            {
                enemyImage.sprite = null;
                enemyImage.enabled = false;
            }
        }

        private static void ApplyAnimationFirstFrameToImage(IdleAnimation animation, Image target)
        {
            if (target == null)
                return;

            if (animation == null || animation.FramesArray == null || animation.FramesArray.Length == 0)
            {
                target.sprite = null;
                target.enabled = false;
                return;
            }

            target.sprite = animation.FramesArray[0];
            target.enabled = target.sprite != null;
        }

        private static BattleOutcomePresentationCatalogAsset.VisualVariant SelectWeightedVariant(
            System.Collections.Generic.IReadOnlyList<BattleOutcomePresentationCatalogAsset.VisualVariant> variants)
        {
            if (variants == null || variants.Count == 0)
                return null;

            var totalWeight = 0;
            for (int i = 0; i < variants.Count; i++)
            {
                var v = variants[i];
                if (v == null || (v.sprite == null && v.animatedPrefab == null && v.playerAnimation == null && v.enemyAnimation == null))
                    continue;

                totalWeight += Mathf.Max(1, v.weight);
            }

            if (totalWeight <= 0)
                return null;

            var roll = Random.Range(0, totalWeight);
            var cumulative = 0;
            for (int i = 0; i < variants.Count; i++)
            {
                var v = variants[i];
                if (v == null || (v.sprite == null && v.animatedPrefab == null && v.playerAnimation == null && v.enemyAnimation == null))
                    continue;

                cumulative += Mathf.Max(1, v.weight);
                if (roll < cumulative)
                    return v;
            }

            return variants[0];
        }

        private static string NormalizeLocationId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            var value = id.Trim();
            if (string.Equals(value, "location", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return value;
        }

        private void SpawnSlot(string rewardId, int count)
        {
            if (rewardSlotPrefab == null || rewardsContent == null)
                return;

            // Unity cannot Instantiate(prefab, parent) if parent is in a persistent (DontDestroyOnLoad) scene.
            // Instantiate first, then SetParent.
            var go = Instantiate(rewardSlotPrefab);
            go.transform.SetParent(rewardsContent, worldPositionStays: false);
            go.SetActive(true);

            var rewardView = go.GetComponent<BattleRewardSlotView>();
            if (rewardView != null)
            {
                rewardView.SetItemDatabase(itemDatabase);
                rewardView.Render(rewardId, count);
                return;
            }

            var view = go.GetComponent<InventoryItemSlotView>();
            if (view != null)
            {
                view.SetItemDatabase(itemDatabase);
                view.RebindParentScrollRectFromHierarchy();
                view.RenderItem(rewardId, icon: null, count: count);

                var trigger = view.GetComponent<ItemTooltipTrigger>();
                if (trigger == null)
                    trigger = view.gameObject.AddComponent<ItemTooltipTrigger>();

                trigger.SetItemDatabase(itemDatabase);
                // In battle result rewards list we want long-press tooltip and ignore short taps.
                trigger.SetMode(ItemTooltipTrigger.TriggerMode.InventoryOrStorage);
            }
        }

        private void RefreshRewardsScrollableLayout()
        {
            if (rewardsContent == null)
                return;

            var contentRt = rewardsContent as RectTransform;
            if (contentRt == null)
                return;

            var grid = rewardsContent.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                int childCount = rewardsContent.childCount;
                int columns = 1;

                if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                {
                    columns = Mathf.Max(1, grid.constraintCount);
                }
                else if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount)
                {
                    int rows = Mathf.Max(1, grid.constraintCount);
                    columns = Mathf.Max(1, Mathf.CeilToInt(childCount / (float)rows));
                }
                else
                {
                    float parentWidth = 0f;
                    if (contentRt.parent is RectTransform viewportRt)
                        parentWidth = viewportRt.rect.width;

                    float availableWidth = Mathf.Max(1f, parentWidth - grid.padding.left - grid.padding.right);
                    float cellPlusSpacing = Mathf.Max(1f, grid.cellSize.x + grid.spacing.x);
                    columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + grid.spacing.x) / cellPlusSpacing));
                }

                int rowsCount = Mathf.Max(1, Mathf.CeilToInt(childCount / (float)Mathf.Max(1, columns)));
                float preferredHeight =
                    grid.padding.top +
                    grid.padding.bottom +
                    rowsCount * grid.cellSize.y +
                    Mathf.Max(0, rowsCount - 1) * grid.spacing.y;

                contentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            Canvas.ForceUpdateCanvases();

            var scrollRect = rewardsContent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        public void Hide()
        {
            ItemTooltip.Hide();

            ClearCharacterPresentation();

            ApplyLocalizedText(leftTipText, null, string.Empty);
            ApplyLocalizedText(rightTipText, null, string.Empty);
            ApplyTextColor(titleText, null, _defaultTitleColor);
            ApplyTextColor(leftTipText, null, _defaultLeftTipColor);
            ApplyTextColor(rightTipText, null, _defaultRightTipColor);

            if (battleDurationText != null)
                battleDurationText.text = "00:00";

            if (totalDamageDealtText != null)
                totalDamageDealtText.text = "0";

            if (totalDamageTakenText != null)
                totalDamageTakenText.text = "0";

            if (achievementsBlock != null)
                achievementsBlock.SetActive(false);

            ClearAchievementItems();

            if (topSeparatorImage != null)
            {
                topSeparatorImage.sprite = null;
                topSeparatorImage.enabled = false;
            }

            if (outcomeImage != null)
            {
                outcomeImage.sprite = null;
                outcomeImage.enabled = false;
            }

            if (root != null)
                root.SetActive(false);

            // Always disable the whole modal object to avoid leaving any overlay elements active.
            gameObject.SetActive(false);
        }

        private void OnOkClicked()
        {
            var cb = onOk;
            onOk = null;
            cb?.Invoke();
        }
    }
}
