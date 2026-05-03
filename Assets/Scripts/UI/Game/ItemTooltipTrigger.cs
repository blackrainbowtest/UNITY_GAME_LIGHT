using System;
using UnityEngine;

namespace UDA2.UI.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventoryItemSlotView))]
    public sealed class ItemTooltipTrigger : MonoBehaviour
    {
        public enum TriggerMode
        {
            Auto = 0,
            InventoryOrStorage = 1,
            Outside = 2,
        }

        [Header("Mode")]
        [SerializeField] private TriggerMode mode = TriggerMode.Auto;

        [Header("Optional: data")]
        [Tooltip("Optional. If not assigned, will try to reuse ItemDatabase from InventoryItemSlotView.")]
        [SerializeField] private UnityEngine.Object itemDatabase;

        private InventoryItemSlotView _slot;
        private TriggerMode _cachedAutoMode;
        private bool _hasCachedAutoMode;

        private void Awake()
        {
            _slot = GetComponent<InventoryItemSlotView>();
        }

        private void OnEnable()
        {
            if (_slot == null)
                _slot = GetComponent<InventoryItemSlotView>();

            _hasCachedAutoMode = false;
            if (mode == TriggerMode.Auto)
            {
                _cachedAutoMode = GetComponentInParent<InventoryTabView>() != null
                    ? TriggerMode.InventoryOrStorage
                    : TriggerMode.Outside;
                _hasCachedAutoMode = true;
            }

            if (_slot != null)
            {
                _slot.Clicked += HandleClicked;
                _slot.LongPressed += HandleLongPressed;
            }
        }

        private void OnDisable()
        {
            if (_slot != null)
            {
                _slot.Clicked -= HandleClicked;
                _slot.LongPressed -= HandleLongPressed;
            }

            // If the source slot/trigger is being disabled (panel close, view rebuild),
            // force-close tooltip so overlay can't keep blocking scene input.
            if (ItemTooltip.IsVisible)
                ItemTooltip.Hide();
        }

        public void SetMode(TriggerMode newMode)
        {
            mode = newMode;
        }

        public void SetItemDatabase(UnityEngine.Object db)
        {
            if (db == null)
                return;

            itemDatabase = db;
        }

        private TriggerMode ResolveMode()
        {
            if (mode != TriggerMode.Auto)
                return mode;

            if (_hasCachedAutoMode)
                return _cachedAutoMode;

            // Minimal heuristic for now:
            // - if we're under InventoryTabView, treat as inventory/storage UI
            // - otherwise, treat as "outside"
            var inv = GetComponentInParent<InventoryTabView>();
            return inv != null ? TriggerMode.InventoryOrStorage : TriggerMode.Outside;
        }

        private UnityEngine.Object ResolveDatabase()
        {
            if (itemDatabase != null)
                return itemDatabase;

            if (_slot != null && _slot.ItemDatabase != null)
                return _slot.ItemDatabase;

            return null;
        }

        private void HandleClicked(InventoryItemSlotView slot, Vector2 screenPos)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.ItemId))
                return;

            // In inventory/storage: clicking does something else (equip, use, move).
            // In other contexts (e.g. shop preview, reward screen): click shows tooltip.
            if (ResolveMode() == TriggerMode.InventoryOrStorage)
                return;

            ItemTooltip.Show(ResolveDatabase(), slot.ItemId, screenPos);
        }

        private void HandleLongPressed(InventoryItemSlotView slot, Vector2 screenPos)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.ItemId))
                return;

            // Long-press always shows the item detail tooltip regardless of mode.
            ItemTooltip.Show(ResolveDatabase(), slot.ItemId, screenPos);
        }
    }
}
