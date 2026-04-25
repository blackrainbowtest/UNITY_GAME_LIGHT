using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    public sealed class ItemTooltipModalController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Button backdropButton;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text buyPriceText;
        [SerializeField] private TMP_Text sellPriceText;

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = new Color(0.70f, 0.70f, 0.70f, 1f);
        [SerializeField] private Color uncommonColor = new Color(0.35f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color rareColor = new Color(0.35f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color epicColor = new Color(0.75f, 0.35f, 0.95f, 1f);
        [SerializeField] private Color legendaryColor = new Color(0.95f, 0.65f, 0.20f, 1f);
        [SerializeField] private Color mythicColor = new Color(0.95f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color uniqueColor = new Color(0.95f, 0.90f, 0.25f, 1f);

        [Header("Positioning")]
        [Tooltip("If true, clamps tooltip inside Screen.safeArea (useful for notches/rounded corners).")]
        [SerializeField] private bool clampToSafeArea = true;
        [Tooltip("Minimum margin (in canvas local units) from safe-area edges.")]
        [SerializeField] private float safeMargin = 16f;
        [Tooltip("Base offset from the pointer position (in canvas local units).")]
        [SerializeField] private Vector2 pointerOffset = new Vector2(28f, -28f);

        private UnityEngine.Object _itemDatabase;
        private string _itemId;

        private static readonly Dictionary<Type, MethodInfo> GetByIdCache = new Dictionary<Type, MethodInfo>(8);
        private static readonly Dictionary<(Type type, string name), PropertyInfo> PropCache = new Dictionary<(Type, string), PropertyInfo>(64);

        private enum RarityKey
        {
            Common,
            Uncommon,
            Rare,
            Epic,
            Legendary,
            Mythic,
            Unique,
        }

        private void Awake()
        {
            if (backdropButton != null)
                backdropButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            ItemTooltip.NotifyDestroyed(this);
        }

        public void Show(UnityEngine.Object itemDatabase, string itemId, Vector2 screenPoint)
        {
            _itemDatabase = itemDatabase;
            _itemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();

            ApplyData();
            PositionAt(screenPoint);
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        private void ApplyData()
        {
            if (string.IsNullOrWhiteSpace(_itemId))
            {
                ApplyNoData();
                return;
            }

            // Tooltip texts are dynamic and set at runtime.
            // For title/description we can use localization components (we set their keys at runtime).
            // For other fields (composite strings like "Type: ..."), localization components would overwrite runtime values.
            DisableLocalizedTextSetter(typeText);
            DisableLocalizedTextSetter(rarityText);
            DisableLocalizedTextSetter(buyPriceText);
            DisableLocalizedTextSetter(sellPriceText);

            var def = TryResolveDefinition(_itemDatabase, _itemId);

            string nameKey = TryGetStringProp(def, "DisplayNameKey");
            string descKey = TryGetStringProp(def, "DescriptionKey");

            // If item definitions don't store localization keys, fall back to ui_items convention.
            // Example: item.gold.name / item.gold.desc
            if (string.IsNullOrWhiteSpace(nameKey))
                nameKey = $"item.{_itemId}.name";
            if (string.IsNullOrWhiteSpace(descKey))
                descKey = $"item.{_itemId}.desc";

            string nameFallback = TryGetStringProp(def, "DisplayName");
            string descFallback = TryGetStringProp(def, "Description");

            var icon = TryGetSpriteProp(def, "Icon");

            string typeRaw = TryGetPropToString(def, "Type");
            string rarityRaw = TryGetPropToString(def, "Rarity");
            string rarityIdRaw = TryGetStringProp(def, "RarityId");

            int sell = TryGetIntProp(def, "SellPrice");
            if (sell < 0)
                sell = TryGetIntProp(def, "Value");
            if (sell < 0)
                sell = 0;

            int buy = TryGetIntProp(def, "BuyPrice");
            if (buy < 0)
                buy = sell * 3;

            var provider = UIStringsProvider.Instance;
            string lang = ResolveLanguage(provider);

            ApplyTitleAndDescription(provider, lang, nameKey, nameFallback, descKey, descFallback);

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            string typeKey = NormalizeKey(typeRaw);
            string rarityKeyRaw = !string.IsNullOrWhiteSpace(rarityIdRaw) ? rarityIdRaw : rarityRaw;
            string rarityKey = NormalizeKey(rarityKeyRaw);

            if (typeText != null)
            {
                string typeValue = provider != null ? provider.Get(typeKey, lang) : typeKey;
                typeText.text = FormatLine(provider, lang, "type_fmt", "type", "Type", typeValue);
            }

            var rarityEnum = ParseRarity(rarityKey);
            var rarityColor = GetColorForRarity(rarityEnum);

            if (rarityText != null)
            {
                string rarityValue = provider != null ? provider.Get(rarityKey, lang) : rarityKey;
                rarityText.text = FormatLine(provider, lang, "rarity_fmt", "rarity", "Rarity", rarityValue);
                rarityText.color = rarityColor;
            }

            if (buyPriceText != null)
            {
                buyPriceText.text = FormatLine(provider, lang, "buy_price_fmt", "buy_price", "Buy", buy.ToString());
            }

            if (sellPriceText != null)
            {
                sellPriceText.text = FormatLine(provider, lang, "sell_price_fmt", "sell_price", "Sell", sell.ToString());
            }
        }

        private static string FormatLine(
            UIStringsProvider provider,
            string lang,
            string formatKey,
            string labelKey,
            string labelFallback,
            string value)
        {
            value ??= string.Empty;

            if (provider != null)
            {
                // Preferred: fully localized template like "Тип: {0}".
                var fmt = provider.Get(formatKey, lang);
                if (!string.IsNullOrWhiteSpace(fmt) && fmt != formatKey)
                {
                    try
                    {
                        return string.Format(System.Globalization.CultureInfo.InvariantCulture, fmt, value);
                    }
                    catch (FormatException)
                    {
                        // fall through
                    }
                }

                // Fallback: localized label + ":".
                var label = provider.Get(labelKey, lang);
                if (!string.IsNullOrWhiteSpace(label) && label != labelKey)
                    return $"{label}: {value}";
            }

            return $"{labelFallback}: {value}";
        }

        private void ApplyNoData()
        {
            if (titleText != null) titleText.text = "—";
            if (descriptionText != null) descriptionText.text = "—";
            if (typeText != null) typeText.text = "";
            if (rarityText != null) rarityText.text = "";
            if (buyPriceText != null) buyPriceText.text = "";
            if (sellPriceText != null) sellPriceText.text = "";

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        private static void DisableLocalizedTextSetter(TMP_Text t)
        {
            if (t == null)
                return;

            var localized = t.GetComponent<LocalizedGlobalComponent>();
            if (localized != null && localized.enabled)
                localized.enabled = false;
        }

        private void ApplyTitleAndDescription(
            UIStringsProvider provider,
            string lang,
            string nameKey,
            string nameFallback,
            string descKey,
            string descFallback)
        {
            if (titleText != null)
            {
                if (!TryApplyLocalizedKey(titleText, provider, lang, nameKey))
                {
                    DisableLocalizedTextSetter(titleText);
                    titleText.text = ResolveLocalized(provider, lang, nameKey, nameFallback, _itemId);
                }
            }

            if (descriptionText != null)
            {
                if (!TryApplyLocalizedKey(descriptionText, provider, lang, descKey))
                {
                    DisableLocalizedTextSetter(descriptionText);
                    var desc = ResolveLocalized(provider, lang, descKey, descFallback, string.Empty);
                    descriptionText.text = string.IsNullOrWhiteSpace(desc) ? "—" : desc;
                }
            }
        }

        private static bool TryApplyLocalizedKey(TMP_Text text, UIStringsProvider provider, string lang, string key)
        {
            if (text == null || provider == null || string.IsNullOrWhiteSpace(key))
                return false;

            key = key.Trim();

            // Only use key-driven components if the key actually resolves;
            // otherwise we'd display the key itself.
            var resolved = provider.Get(key, lang);
            if (string.IsNullOrWhiteSpace(resolved) || resolved == key)
                return false;

            var localized = text.GetComponent<LocalizedGlobalComponent>();
            if (localized != null)
            {
                localized.enabled = true;
                localized.Key = key;
                localized.ClearArgs();
                localized.UpdateText(lang);
                return true;
            }

            return false;
        }

        private void PositionAt(Vector2 screenPoint)
        {
            if (panel == null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            var canvasRt = canvas.transform as RectTransform;
            if (canvasRt == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPoint, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local))
                return;

            var clampRect = GetClampRectLocal(canvas, canvasRt);
            clampRect.xMin += safeMargin;
            clampRect.xMax -= safeMargin;
            clampRect.yMin += safeMargin;
            clampRect.yMax -= safeMargin;

            // Choose an offset direction away from the closest edges.
            var offset = pointerOffset;
            var center = clampRect.center;
            offset.x = Mathf.Abs(offset.x) * (local.x > center.x ? -1f : 1f);
            offset.y = Mathf.Abs(offset.y) * (local.y > center.y ? -1f : 1f);

            // Place near finger/mouse.
            panel.anchoredPosition = local + offset;

            // Clamp into safe/canvas bounds (respect pivot).
            var rect = clampRect;
            var size = panel.rect.size;
            var pivot = panel.pivot;

            float xMin = rect.xMin + size.x * pivot.x;
            float xMax = rect.xMax - size.x * (1f - pivot.x);
            float yMin = rect.yMin + size.y * pivot.y;
            float yMax = rect.yMax - size.y * (1f - pivot.y);

            var p = panel.anchoredPosition;
            p.x = Mathf.Clamp(p.x, xMin, xMax);
            p.y = Mathf.Clamp(p.y, yMin, yMax);
            panel.anchoredPosition = p;
        }

        private Rect GetClampRectLocal(Canvas canvas, RectTransform canvasRt)
        {
            if (!clampToSafeArea || canvas == null || canvasRt == null)
                return canvasRt != null ? canvasRt.rect : new Rect(-500, -500, 1000, 1000);

            var safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f)
                return canvasRt.rect;

            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            // Convert safe area corners from screen space to canvas local space.
            var p0 = new Vector2(safe.xMin, safe.yMin);
            var p1 = new Vector2(safe.xMax, safe.yMax);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, p0, cam, out var l0))
                return canvasRt.rect;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, p1, cam, out var l1))
                return canvasRt.rect;

            float xMin = Mathf.Min(l0.x, l1.x);
            float xMax = Mathf.Max(l0.x, l1.x);
            float yMin = Mathf.Min(l0.y, l1.y);
            float yMax = Mathf.Max(l0.y, l1.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static string ResolveLocalized(UIStringsProvider provider, string lang, string key, string fallback, string finalFallback)
        {
            if (provider != null && !string.IsNullOrWhiteSpace(key))
            {
            var s = provider.Get(key.Trim(), lang);
                if (!string.IsNullOrWhiteSpace(s) && s != key)
                    return s;
            }

            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback;

            return finalFallback;
        }

        private static string ResolveLanguage(UIStringsProvider provider)
        {
            var settings = UDA2.Core.SettingsContext.Current;
            if (settings != null && !string.IsNullOrWhiteSpace(settings.language))
                return settings.language;

            return provider != null && !string.IsNullOrWhiteSpace(provider.CurrentLanguage)
                ? provider.CurrentLanguage
                : "en";
        }

        private static string NormalizeKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            return raw.Trim().ToLowerInvariant();
        }

        private RarityKey ParseRarity(string rarityKey)
        {
            if (string.IsNullOrWhiteSpace(rarityKey))
                return RarityKey.Common;

            if (Enum.TryParse(rarityKey.Trim(), ignoreCase: true, out RarityKey parsed))
                return parsed;

            return RarityKey.Common;
        }

        private Color GetColorForRarity(RarityKey rarity)
        {
            return rarity switch
            {
                RarityKey.Uncommon => uncommonColor,
                RarityKey.Rare => rareColor,
                RarityKey.Epic => epicColor,
                RarityKey.Legendary => legendaryColor,
                RarityKey.Mythic => mythicColor,
                RarityKey.Unique => uniqueColor,
                RarityKey.Common => commonColor,
                _ => commonColor,
            };
        }

        private static object TryResolveDefinition(UnityEngine.Object itemDatabase, string itemId)
        {
            if (itemDatabase == null || string.IsNullOrWhiteSpace(itemId))
                return null;

            try
            {
                var dbType = itemDatabase.GetType();
                if (!GetByIdCache.TryGetValue(dbType, out var getById))
                {
                    getById = dbType.GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public);
                    GetByIdCache[dbType] = getById;
                }

                if (getById == null)
                    return null;

                return getById.Invoke(itemDatabase, new object[] { itemId.Trim() });
            }
            catch
            {
                return null;
            }
        }

        private static string TryGetStringProp(object obj, string prop)
        {
            if (obj == null)
                return null;

            try
            {
                var p = GetCachedProp(obj.GetType(), prop);
                if (p == null)
                    return null;

                return p.GetValue(obj) as string;
            }
            catch
            {
                return null;
            }
        }

        private static string TryGetPropToString(object obj, string prop)
        {
            if (obj == null)
                return null;

            try
            {
                var p = GetCachedProp(obj.GetType(), prop);
                if (p == null)
                    return null;

                var v = p.GetValue(obj);
                return v != null ? v.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        private static Sprite TryGetSpriteProp(object obj, string prop)
        {
            if (obj == null)
                return null;

            try
            {
                var p = GetCachedProp(obj.GetType(), prop);
                if (p == null)
                    return null;

                return p.GetValue(obj) as Sprite;
            }
            catch
            {
                return null;
            }
        }

        private static int TryGetIntProp(object obj, string prop)
        {
            if (obj == null)
                return -1;

            try
            {
                var p = GetCachedProp(obj.GetType(), prop);
                if (p == null)
                    return -1;

                var v = p.GetValue(obj);
                if (v is int i) return i;
                if (v is long l) return (int)l;
                if (v is float f) return Mathf.RoundToInt(f);
                if (v is double d) return (int)Math.Round(d);

                if (v != null && int.TryParse(v.ToString(), out var parsed))
                    return parsed;
            }
            catch
            {
                // ignored
            }

            return -1;
        }

        private static PropertyInfo GetCachedProp(Type type, string name)
        {
            if (type == null || string.IsNullOrWhiteSpace(name))
                return null;

            var key = (type, name);
            if (PropCache.TryGetValue(key, out var p))
                return p;

            p = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            PropCache[key] = p;
            return p;
        }
    }
}
