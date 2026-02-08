using System;
using System.Collections.Generic;
using UnityEngine;
using UDA2.UI.Game;

namespace Game.Battle.UI
{
    public sealed class BattleInventoryItemClickHook : MonoBehaviour
    {
        private Action<string> onItemClicked;
        private Func<bool> resolveIsAllowed;
        private bool requireInsideInventoryTab;

        private readonly HashSet<InventoryItemSlotView> wired = new HashSet<InventoryItemSlotView>();

        public void Init(Action<string> onItemClicked, Func<bool> resolveIsAllowed, bool requireInsideInventoryTabView)
        {
            this.onItemClicked = onItemClicked;
            this.resolveIsAllowed = resolveIsAllowed;
            requireInsideInventoryTab = requireInsideInventoryTabView;
        }

        private void OnEnable()
        {
            WireSlots();
            InvokeRepeating(nameof(WireSlots), 0.15f, 0.25f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(WireSlots));
            UnwireAll();
        }

        private void WireSlots()
        {
            var slots = GetComponentsInChildren<InventoryItemSlotView>(includeInactive: true);
            if (slots == null || slots.Length == 0)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null)
                    continue;

                if (requireInsideInventoryTab)
                {
                    if (slot.GetComponentInParent<InventoryTabView>() == null)
                        continue;
                }

                if (wired.Contains(slot))
                    continue;

                wired.Add(slot);
                slot.Clicked += HandleSlotClicked;
            }
        }

        private void UnwireAll()
        {
            foreach (var slot in wired)
            {
                if (slot == null) continue;
                slot.Clicked -= HandleSlotClicked;
            }
            wired.Clear();
        }

        private void HandleSlotClicked(InventoryItemSlotView slot, Vector2 screenPos)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.ItemId))
                return;

            if (resolveIsAllowed != null && !resolveIsAllowed())
                return;

            onItemClicked?.Invoke(slot.ItemId);
        }
    }
}
