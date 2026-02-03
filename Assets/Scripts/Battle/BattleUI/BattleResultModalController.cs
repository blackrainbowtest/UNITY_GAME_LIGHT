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

        [Header("Ordering")]
        [Tooltip("If true, EXP will be spawned as a reward entry with id 'exp'.")]
        [SerializeField] private bool showExpAsSlot = true;

        private Action onOk;

        private void Awake()
        {
            if (okButton != null)
                okButton.onClick.AddListener(OnOkClicked);

            Hide();
        }

        public void Show(Game.Battle.BattleResultData data, Action onOkClicked)
        {
            onOk = onOkClicked;

            ApplyLocalizedTitle(data);

            // Prefab list: we show ONLY slots (currencies first, then items).
            if (rewardsContent != null && rewardSlotPrefab != null)
                RenderRewardsAsSlots(data);

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

            var setter = titleText.GetComponent<LocalizedTextSetter>();
            if (setter != null)
            {
                setter.key = key;
                setter.targetText = titleText;
                setter.UpdateText();

                // If 'lose' isn't in localization, try legacy key.
                if (!data.PlayerWon && string.Equals(titleText.text, key, StringComparison.OrdinalIgnoreCase))
                {
                    setter.key = "loes";
                    setter.UpdateText();
                }
            }

            var comp = titleText.GetComponent<LocalizedTextComponent>();
            if (comp != null)
            {
                comp.textKey = data.PlayerWon ? "victory" : "loes";
                comp.UpdateText();
            }

            // Hard fallback (protects against missing localization wiring)
            if (setter == null && comp == null)
                titleText.text = data.PlayerWon ? "Victory" : "Defeat";
        }

        private void RenderRewardsAsSlots(Game.Battle.BattleResultData data)
        {
            ClearChildren(rewardsContent);

            // Currency first (always in fixed order). The slot view decides how to render each id.
            // Gold is always spawned first (even if 0, so layout stays consistent).
            SpawnSlot("gold", data.GoldGained);

            if (data.ManaCrystalsGained != 0)
                SpawnSlot("mana_crystal", data.ManaCrystalsGained);
            if (data.DemonCrystalsGained != 0)
                SpawnSlot("demon_crystal", data.DemonCrystalsGained);

            // EXP as a pseudo-item entry.
            if (showExpAsSlot)
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

        private void SpawnSlot(string rewardId, int count)
        {
            if (rewardSlotPrefab == null || rewardsContent == null)
                return;

            var go = Instantiate(rewardSlotPrefab, rewardsContent);
            go.SetActive(true);

            var rewardView = go.GetComponent<BattleRewardSlotView>();
            if (rewardView != null)
            {
                rewardView.Render(rewardId, count);
                return;
            }

            var view = go.GetComponent<InventoryItemSlotView>();
            if (view != null)
                view.RenderItem(icon: null, count: count);
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
            else
                gameObject.SetActive(false);
        }

        private void OnOkClicked()
        {
            var cb = onOk;
            onOk = null;
            Hide();
            cb?.Invoke();
        }
    }
}
