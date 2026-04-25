using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UDA2.UI.Game;

namespace Game.Battle.UI
{
    /// <summary>
    /// Simple modal for showing battle results.
    /// UI-only component, no combat logic.
    /// </summary>
    public sealed class BattleResultModalController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button okButton;

        [Header("Optional Prefab Loot List")]
        [Tooltip("ScrollView Content transform (with GridLayoutGroup). If assigned, modal will spawn slots here.")]
        [SerializeField] private Transform rewardsContent;

        [Tooltip("Prefab with InventoryItemSlotView for icon + count.")]
        [SerializeField] private GameObject rewardSlotPrefab;

        [Header("Optional: data")]
        [Tooltip("Optional. Assign ItemDatabase asset to resolve tooltip/name/rarity. Kept as Object to avoid assembly reference coupling.")]
        [SerializeField] private UnityEngine.Object itemDatabase;

        [Header("Ordering")]
        [Tooltip("If true, EXP will be spawned as a reward entry with id 'exp'.")]
        [SerializeField] private bool showExpAsSlot = true;

        private Action onOk;
        private bool _suppressHideOnAwake;

        public void SetItemDatabase(UnityEngine.Object db)
        {
            if (db == null)
                return;

            if (!IsCompatibleItemDatabase(db))
                return;

            if (itemDatabase == null || !IsCompatibleItemDatabase(itemDatabase))
                itemDatabase = db;
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

            if (okButton != null)
                okButton.onClick.AddListener(OnOkClicked);

            if (!_suppressHideOnAwake)
                Hide();
        }

        public void Show(Game.Battle.BattleResultData data, Action onOkClicked)
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

            ApplyLocalizedTitle(data);

            AutoWireIfMissing();


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

        private void ApplyLocalizedTitle(Game.Battle.BattleResultData data)
        {
            if (titleText == null)
                return;

            // Prefer 'lose', fallback to legacy 'loes' (your CSV currently has *loes;).
            var key = data.PlayerWon ? "victory" : "lose";

            var localized = titleText.GetComponent<LocalizedGlobalComponent>();
            if (localized != null)
            {
                localized.Key = key;
                localized.ClearArgs();
                localized.UpdateText();

                // If 'lose' isn't in localization, try legacy key.
                if (!data.PlayerWon && string.Equals(titleText.text, key, StringComparison.OrdinalIgnoreCase))
                {
                    localized.Key = "loes";
                    localized.UpdateText();
                }
            }

            // Hard fallback (protects against missing localization wiring)
            if (localized == null)
                titleText.text = data.PlayerWon ? "Victory" : "Defeat";
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
            if (data.Items == null || data.Items.Count == 0)
                return;

            for (int i = 0; i < data.Items.Count; i++)
            {
                var r = data.Items[i];
                if (string.IsNullOrWhiteSpace(r.ItemId) || r.Count <= 0)
                    continue;

                SpawnSlot(r.ItemId.Trim(), r.Count);
            }
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

                if (titleText == null)
                    titleText = GetComponentInChildren<TMP_Text>(true);
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
                // Try to find a child named "RewardsContent" or "Content".
                var rts = GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < rts.Length; i++)
                {
                    var rt = rts[i];
                    if (rt == null) continue;
                    if (string.Equals(rt.name, "RewardsContent", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(rt.name, "Content", StringComparison.OrdinalIgnoreCase))
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
                view.RenderItem(rewardId, icon: null, count: count);

                var trigger = view.GetComponent<ItemTooltipTrigger>();
                if (trigger == null)
                    trigger = view.gameObject.AddComponent<ItemTooltipTrigger>();

                trigger.SetItemDatabase(itemDatabase);
                trigger.SetMode(ItemTooltipTrigger.TriggerMode.Outside);
            }
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
