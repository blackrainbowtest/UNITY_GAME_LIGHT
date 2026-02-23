using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    public sealed class InventoryTabView : MonoBehaviour
    {
        private enum SortMode
        {
            ById,
            ByType,
            ByRarity,
        }

        [Header("Wiring")]
        [SerializeField] private Transform content;
        [SerializeField] private InventoryItemSlotView itemSlotPrefab;

        [Header("Sort Buttons")]
        [SerializeField] private Button filterById;
        [SerializeField] private Button filterByType;
        [SerializeField] private Button filterByRarity;

        [Header("Sort Button Visuals")]
        [Tooltip("If true, the selected sort button will be visually highlighted even if the prefab has no disabled-color tint configured.")]
        [SerializeField] private bool forceSelectedVisual = true;

        [Header("Optional: data")]
        [Tooltip("Optional. Assign the ItemDatabase asset to resolve type/rarity/icons. Kept as Object to avoid assembly reference coupling.")]
        [SerializeField] private UnityEngine.Object itemDatabase;

        public void SetItemDatabase(UnityEngine.Object db)
        {
            if (db == null)
                return;

            if (!IsCompatibleItemDatabase(db))
                return;

            if (itemDatabase == null || !IsCompatibleItemDatabase(itemDatabase))
                itemDatabase = db;
        }

        [Header("Behavior")]
        [SerializeField] private bool showEmptySlots = true;

        private readonly List<InventoryItemSlotView> _spawned = new List<InventoryItemSlotView>(128);
        private SortMode _sortMode = SortMode.ById;

        private readonly Dictionary<Button, ButtonVisualCache> _buttonVisualCache = new Dictionary<Button, ButtonVisualCache>(8);

        private void Awake()
        {
            AutoWireIfMissing();

            CacheButtonVisual(filterById);
            CacheButtonVisual(filterByType);
            CacheButtonVisual(filterByRarity);

            if (filterById != null)
                filterById.onClick.AddListener(() => { _sortMode = SortMode.ById; Refresh(); UpdateFilterButtonsVisual(); });

            if (filterByType != null)
                filterByType.onClick.AddListener(() => { _sortMode = SortMode.ByType; Refresh(); UpdateFilterButtonsVisual(); });

            if (filterByRarity != null)
                filterByRarity.onClick.AddListener(() => { _sortMode = SortMode.ByRarity; Refresh(); UpdateFilterButtonsVisual(); });

            UpdateFilterButtonsVisual();
        }

        private void AutoWireIfMissing()
        {
            if (content == null)
            {
                var rts = GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < rts.Length; i++)
                {
                    if (rts[i] != null && string.Equals(rts[i].name, "Content", StringComparison.OrdinalIgnoreCase))
                    {
                        content = rts[i];
                        break;
                    }
                }
            }

            if (filterById == null || filterByType == null || filterByRarity == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    var b = buttons[i];
                    if (b == null) continue;

                    if (filterById == null && string.Equals(b.name, "FilterById", StringComparison.OrdinalIgnoreCase))
                        filterById = b;
                    else if (filterByType == null && string.Equals(b.name, "FilterByType", StringComparison.OrdinalIgnoreCase))
                        filterByType = b;
                    else if (filterByRarity == null && string.Equals(b.name, "FilterByRarity", StringComparison.OrdinalIgnoreCase))
                        filterByRarity = b;
                }
            }

            if (itemSlotPrefab == null)
            {
                itemSlotPrefab = Resources.Load<InventoryItemSlotView>("Prefabs/UI/Profile/Inventory/InventoryItemSlotView");
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            var save = global::GameState.Instance != null ? global::GameState.Instance.CurrentSave : null;
            var inv = save != null ? save.inventory : null;

            if (content == null)
            {
                Debug.LogWarning("[InventoryTabView] Content is not assigned.");
                return;
            }

            if (itemSlotPrefab == null)
            {
                Debug.LogWarning("[InventoryTabView] ItemSlotPrefab is not assigned.");
                return;
            }

            var entries = BuildEntries(inv);
            SortEntries(entries);

            UpdateFilterButtonsVisual();

            int capacity = save != null ? InventoryCapacityRules.GetCapacity(save) : InventoryCapacityRules.BaseSlots;
            int slotCount = showEmptySlots ? Math.Max(capacity, entries.Count) : entries.Count;

            EnsureSlotInstances(slotCount);

            // Render filled
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (i < entries.Count)
                {
                    var e = entries[i];
                    _spawned[i].RenderItem(e.itemId, e.icon, e.count);
                }
                else
                {
                    _spawned[i].RenderEmpty();
                }
            }
        }

        private void UpdateFilterButtonsVisual()
        {
            ApplyFilterButtonState(filterById, _sortMode == SortMode.ById);
            ApplyFilterButtonState(filterByType, _sortMode == SortMode.ByType);
            ApplyFilterButtonState(filterByRarity, _sortMode == SortMode.ByRarity);
        }

        private void CacheButtonVisual(Button button)
        {
            if (button == null)
                return;

            if (_buttonVisualCache.ContainsKey(button))
                return;

            var graphics = button.GetComponentsInChildren<Graphic>(includeInactive: true);
            var graphicColors = new Color[graphics != null ? graphics.Length : 0];
            if (graphics != null)
            {
                for (int i = 0; i < graphics.Length; i++)
                {
                    graphicColors[i] = graphics[i] != null ? graphics[i].color : Color.white;
                }
            }

            var cache = new ButtonVisualCache
            {
                colors = button.colors,
                hasGraphic = button.targetGraphic != null,
                graphicColor = button.targetGraphic != null ? button.targetGraphic.color : Color.white,
                graphics = graphics,
                graphicColors = graphicColors,
            };

            _buttonVisualCache.Add(button, cache);
        }

        private void ApplyFilterButtonState(Button button, bool isSelected)
        {
            if (button == null)
                return;

            CacheButtonVisual(button);

            if (!_buttonVisualCache.TryGetValue(button, out var cache))
                return;

            if (!forceSelectedVisual)
            {
                button.interactable = !isSelected;
                return;
            }

            if (isSelected)
            {
                var selectedColor = DeriveSelectedColor(cache.colors, cache.hasGraphic ? cache.graphicColor : (Color?)null);

                // Keep the same interaction model as tabs (selected is not interactable),
                // but force a visible tint even if the prefab's disabled color is not configured.
                var colors = cache.colors;
                colors.disabledColor = selectedColor;
                button.colors = colors;
                button.interactable = false;
                ApplyTintToCachedGraphics(cache, selectedColor);
            }
            else
            {
                button.interactable = true;
                button.colors = cache.colors;
                RestoreCachedGraphics(cache);
            }
        }

        private static void ApplyTintToCachedGraphics(ButtonVisualCache cache, Color color)
        {
            if (cache.graphics == null)
                return;

            for (int i = 0; i < cache.graphics.Length; i++)
            {
                var g = cache.graphics[i];
                if (g == null)
                    continue;

                g.color = color;
            }
        }

        private static void RestoreCachedGraphics(ButtonVisualCache cache)
        {
            if (cache.graphics == null || cache.graphicColors == null)
                return;

            int count = Math.Min(cache.graphics.Length, cache.graphicColors.Length);
            for (int i = 0; i < count; i++)
            {
                var g = cache.graphics[i];
                if (g == null)
                    continue;

                g.color = cache.graphicColors[i];
            }
        }

        private static Color DeriveSelectedColor(ColorBlock baseColors, Color? baseGraphicColor)
        {
            // Prefer existing palette from the prefab.
            if (baseColors.highlightedColor != baseColors.normalColor)
                return ForceOpaque(baseColors.highlightedColor);

            if (baseColors.pressedColor != baseColors.normalColor)
                return ForceOpaque(baseColors.pressedColor);

            // Fallback: slightly brighten whatever the button/graphic currently uses.
            var c = baseGraphicColor ?? baseColors.normalColor;
            return ForceOpaque(Color.Lerp(c, Color.white, 0.25f));
        }

        private static Color ForceOpaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        private List<Entry> BuildEntries(SaveData.Inventory inv)
        {
            var list = new List<Entry>();

            if (inv == null || inv.items == null)
                return list;

            for (int i = 0; i < inv.items.Count; i++)
            {
                var it = inv.items[i];
                if (it == null)
                    continue;

                if (string.IsNullOrWhiteSpace(it.itemId))
                    continue;

                if (it.count <= 0)
                    continue;

                var entry = new Entry
                {
                    itemId = it.itemId.Trim(),
                    count = it.count,
                };

                if (itemDatabase != null)
                    TryResolveItemFromDatabase(itemDatabase, entry.itemId, out entry.icon, out entry.typeId, out entry.rarityId);

                list.Add(entry);
            }

            return list;
        }

        private void SortEntries(List<Entry> entries)
        {
            if (entries == null)
                return;

            entries.Sort((a, b) =>
            {
                int cmp;

                switch (_sortMode)
                {
                    case SortMode.ByType:
                        cmp = CompareType(a.typeId, b.typeId);
                        if (cmp != 0) return cmp;
                        cmp = string.Compare(a.itemId, b.itemId, StringComparison.OrdinalIgnoreCase);
                        if (cmp != 0) return cmp;
                        return 0;

                    case SortMode.ByRarity:
                        cmp = CompareRarity(a.rarityId, b.rarityId);
                        if (cmp != 0) return cmp;
                        cmp = string.Compare(a.itemId, b.itemId, StringComparison.OrdinalIgnoreCase);
                        if (cmp != 0) return cmp;
                        return 0;

                    case SortMode.ById:
                    default:
                        return string.Compare(a.itemId, b.itemId, StringComparison.OrdinalIgnoreCase);
                }
            });
        }

        private void EnsureSlotInstances(int desired)
        {
            desired = Math.Max(0, desired);

            // Shrink
            for (int i = _spawned.Count - 1; i >= desired; i--)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i].gameObject);
                _spawned.RemoveAt(i);
            }

            // Grow
            while (_spawned.Count < desired)
            {
                var inst = Instantiate(itemSlotPrefab, content);
                if (inst != null)
                {
                    inst.SetItemDatabase(itemDatabase);

                    var trigger = inst.GetComponent<ItemTooltipTrigger>();
                    if (trigger == null)
                        trigger = inst.gameObject.AddComponent<ItemTooltipTrigger>();

                    trigger.SetItemDatabase(itemDatabase);
                    trigger.SetMode(ItemTooltipTrigger.TriggerMode.InventoryOrStorage);
                }
                _spawned.Add(inst);
            }
        }

        private static int CompareType(string a, string b)
        {
            int oa = TypeOrder(a);
            int ob = TypeOrder(b);
            int cmp = oa.CompareTo(ob);
            if (cmp != 0) return cmp;
            return string.Compare(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static int TypeOrder(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return 999;
            // REMEMBERME: __type
            return type.Trim().ToLowerInvariant() switch
            {
                "currency" => 0,
                "consumable" => 1,
                "resource" => 2,
                "equipment" => 3,
                _ => 100,
            };
        }

        private static int CompareRarity(string a, string b)
        {
            int ra = RarityOrder(a);
            int rb = RarityOrder(b);
            int cmp = ra.CompareTo(rb);
            if (cmp != 0) return cmp;
            return string.Compare(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static int RarityOrder(string rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return 999;
            // REMEMBERME: __rarity
            return rarity.Trim().ToLowerInvariant() switch
            {
                "common" => 0,
                "uncommon" => 1,
                "rare" => 2,
                "epic" => 3,
                "legendary" => 4,
                "mythic" => 5,
                "unique" => 6,
                _ => 100,
            };
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

        private static bool TryResolveItemFromDatabase(UnityEngine.Object db, string itemId, out Sprite icon, out string typeId, out string rarityId)
        {
            icon = null;
            typeId = null;
            rarityId = null;

            if (db == null || string.IsNullOrEmpty(itemId))
                return false;

            try
            {
                var dbType = db.GetType();
                var getById = dbType.GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public);
                if (getById == null)
                    return false;

                var def = getById.Invoke(db, new object[] { itemId });
                if (def == null)
                    return false;

                var defType = def.GetType();

                var iconProp = defType.GetProperty("Icon", BindingFlags.Instance | BindingFlags.Public);
                var typeProp = defType.GetProperty("Type", BindingFlags.Instance | BindingFlags.Public);
                var rarityProp = defType.GetProperty("Rarity", BindingFlags.Instance | BindingFlags.Public);

                if (iconProp != null)
                    icon = iconProp.GetValue(def) as Sprite;

                if (typeProp != null)
                {
                    var t = typeProp.GetValue(def);
                    typeId = t != null ? t.ToString() : null;
                }

                if (rarityProp != null)
                {
                    var r = rarityProp.GetValue(def);
                    rarityId = r != null ? r.ToString() : null;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private struct Entry
        {
            public string itemId;
            public int count;
            public Sprite icon;
            public string typeId;
            public string rarityId;
        }

        private struct ButtonVisualCache
        {
            public ColorBlock colors;
            public bool hasGraphic;
            public Color graphicColor;
            public Graphic[] graphics;
            public Color[] graphicColors;
        }
    }
}
