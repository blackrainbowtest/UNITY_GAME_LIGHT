using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    public sealed class InventoryItemSlotView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Wiring")]
        [Tooltip("Optional. If not assigned, will try to use Image on the same GameObject.")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject filledStateRoot;

        [Header("Rendering")]
        [Tooltip("If true, shows count even when it equals 1.")]
        [SerializeField] private bool showCountWhenOne = false;

        [Tooltip("If true, tints the slot background based on item rarity (for filled slots only).")]
        [SerializeField] private bool tintBackgroundByRarity = true;

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = new Color(0.70f, 0.70f, 0.70f, 1f);
        [SerializeField] private Color uncommonColor = new Color(0.35f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color rareColor = new Color(0.35f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color epicColor = new Color(0.75f, 0.35f, 0.95f, 1f);
        [SerializeField] private Color legendaryColor = new Color(0.95f, 0.65f, 0.20f, 1f);
        [SerializeField] private Color mythicColor = new Color(0.95f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color uniqueColor = new Color(0.95f, 0.90f, 0.25f, 1f);

        [Header("Optional: Icon Sources")]
        [Tooltip("Optional. Assign ItemDatabase asset to resolve item icons by itemId. Kept as Object to avoid asmdef coupling.")]
        [SerializeField] private UnityEngine.Object itemDatabase;

        public UnityEngine.Object ItemDatabase => itemDatabase;

        public void SetItemDatabase(UnityEngine.Object db)
        {
            if (db == null)
                return;

            if (itemDatabase == null)
                itemDatabase = db;
        }

        [Header("Input")]
        [SerializeField] private bool enableInput = true;
        [Tooltip("Seconds to trigger long press.")]
        [SerializeField] private float longPressDuration = 0.7f;

        [Header("Long Press Visual")]
        [Tooltip("If true, shows a circular progress indicator under the finger while holding.")]
        [SerializeField] private bool showLongPressProgress = true;
        [Tooltip("Delay before showing the progress UI (prevents flashing on quick taps).")]
        [SerializeField] private float progressShowDelay = 0.15f;

        [Header("Scroll/Drag Guard")]
        [SerializeField] private ScrollRect parentScrollRect;
        [Tooltip("If moved more than this (px), treat as scroll/drag and ignore tap. <=0 uses EventSystem.pixelDragThreshold")]
        [SerializeField] private float dragThresholdPixels = -1f;
        [Tooltip("If ScrollRect normalizedPosition changes more than this while pressed, cancel tap/long press.")]
        [SerializeField] private float scrollCancelThreshold = 0.0005f;

        public string ItemId { get; private set; } = string.Empty;
        public int Count { get; private set; }

        public event Action<InventoryItemSlotView, Vector2> Clicked;
        public event Action<InventoryItemSlotView, Vector2> LongPressed;

        private bool _isEmpty = true;
        private bool _hasCachedEmptyColor;
        private Color _emptyBackgroundColor;
        private bool _isPointerDown;
        private bool _wasLongPressed;
        private bool _canceledByScroll;
        private Vector2 _pointerDownPosition;
        private Vector2 _scrollPosOnPointerDown;
        private float _pointerDownTime;
        private bool _waitingToShowProgress;
        private float _progressShowTimer;
        private global::LongPressHandler _longPress;

        private void Awake()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (backgroundImage != null)
            {
                _emptyBackgroundColor = backgroundImage.color;
                _hasCachedEmptyColor = true;
            }

            if (parentScrollRect == null)
                parentScrollRect = GetComponentInParent<ScrollRect>();

            _longPress = new global::LongPressHandler(Mathf.Max(0.01f, longPressDuration));
            _longPress.OnCompleted += HandleLongPressCompleted;
            _longPress.OnCanceled += HandleLongPressCanceled;
            _longPress.OnProgress += HandleLongPressProgress;
        }

        private void Update()
        {
            if (!_isPointerDown || !enableInput)
                return;

            if (!_canceledByScroll && parentScrollRect != null)
            {
                var delta = parentScrollRect.normalizedPosition - _scrollPosOnPointerDown;
                if (delta.sqrMagnitude > scrollCancelThreshold * scrollCancelThreshold)
                    CancelByScroll();
            }

            _longPress?.Update(Time.unscaledDeltaTime);

            if (showLongPressProgress && _waitingToShowProgress)
            {
                _progressShowTimer += Time.unscaledDeltaTime;
                if (_progressShowTimer >= Mathf.Max(0f, progressShowDelay))
                {
                    _waitingToShowProgress = false;
                    LongPressProgressHud.Show(GetInstanceID(), _pointerDownPosition);
                }
            }
        }

        public void RenderEmpty()
        {
            ItemId = string.Empty;
            Count = 0;
            _isEmpty = true;

            if (backgroundImage != null && _hasCachedEmptyColor)
                backgroundImage.color = _emptyBackgroundColor;

            if (filledStateRoot != null) filledStateRoot.SetActive(false);
            if (emptyStateRoot != null) emptyStateRoot.SetActive(true);

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (countText != null)
                countText.text = string.Empty;
        }

        public void RenderItem(Sprite icon, int count)
        {
            RenderItem(itemId: string.Empty, icon: icon, count: count);
        }

        public void RenderItem(string itemId, Sprite icon, int count)
        {
            ItemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
            Count = Mathf.Max(0, count);
            _isEmpty = false;

            if (icon == null)
                icon = ResolveIcon(ItemId);

            if (emptyStateRoot != null) emptyStateRoot.SetActive(false);
            if (filledStateRoot != null) filledStateRoot.SetActive(true);

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (countText != null)
            {
                countText.text = (Count > 1 || (showCountWhenOne && Count > 0)) ? Count.ToString() : string.Empty;
            }

            if (tintBackgroundByRarity)
                ApplyRarityTint(ItemId);
        }

        private void ApplyRarityTint(string itemId)
        {
            if (backgroundImage == null)
                return;

            if (string.IsNullOrWhiteSpace(itemId))
                return;

            // If we can't resolve rarity, default to Common.
            var rarity = ResolveRarityKey(itemId);
            backgroundImage.color = GetColorForRarity(rarity);
        }

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

        private RarityKey ResolveRarityKey(string itemId)
        {
            if (itemDatabase == null || string.IsNullOrWhiteSpace(itemId))
                return RarityKey.Common;

            try
            {
                var dbType = itemDatabase.GetType();
                var getById = dbType.GetMethod("GetById");
                if (getById == null)
                    return RarityKey.Common;

                var def = getById.Invoke(itemDatabase, new object[] { itemId.Trim() });
                if (def == null)
                    return RarityKey.Common;

                var defType = def.GetType();

                // Prefer strongly typed enum property if present.
                var rarityProp = defType.GetProperty("Rarity");
                if (rarityProp != null)
                {
                    var r = rarityProp.GetValue(def);
                    if (r != null && System.Enum.TryParse(r.ToString(), ignoreCase: true, out RarityKey parsedEnum))
                        return parsedEnum;
                }

                // Fallback: string rarity id.
                var rarityIdProp = defType.GetProperty("RarityId");
                if (rarityIdProp != null)
                {
                    var raw = rarityIdProp.GetValue(def)?.ToString();
                    if (!string.IsNullOrWhiteSpace(raw) && System.Enum.TryParse(raw.Trim(), ignoreCase: true, out RarityKey parsed))
                        return parsed;
                }
            }
            catch
            {
                // ignored
            }

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

        private Sprite ResolveIcon(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            return TryResolveIconFromDatabase(itemId);
        }

        private Sprite TryResolveIconFromDatabase(string itemId)
        {
            if (itemDatabase == null)
                return null;

            try
            {
                var dbType = itemDatabase.GetType();
                var getById = dbType.GetMethod("GetById");
                if (getById == null)
                    return null;

                var def = getById.Invoke(itemDatabase, new object[] { itemId.Trim() });
                if (def == null)
                    return null;

                var defType = def.GetType();
                var iconProp = defType.GetProperty("Icon");
                if (iconProp == null)
                    return null;

                return iconProp.GetValue(def) as Sprite;
            }
            catch
            {
                return null;
            }
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enableInput || _isEmpty)
                return;

            _isPointerDown = true;
            _canceledByScroll = false;
            _wasLongPressed = false;

            _pointerDownPosition = eventData != null ? eventData.position : Vector2.zero;
            _pointerDownTime = Time.unscaledTime;

            if (showLongPressProgress)
            {
                _progressShowTimer = 0f;
                _waitingToShowProgress = true;
                LongPressProgressHud.Begin(GetInstanceID());
            }

            if (parentScrollRect != null)
                _scrollPosOnPointerDown = parentScrollRect.normalizedPosition;

            _longPress?.CancelPress();
            _longPress?.StartPress();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!enableInput || _isEmpty)
                return;

            _isPointerDown = false;
            _waitingToShowProgress = false;
            _longPress?.CancelPress();

            if (showLongPressProgress)
                LongPressProgressHud.End(GetInstanceID());

            if (_canceledByScroll)
                return;

            // Drag guard.
            if (eventData != null)
            {
                float threshold = dragThresholdPixels > 0f
                    ? dragThresholdPixels
                    : (EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f);

                var moved = (eventData.position - _pointerDownPosition).sqrMagnitude;
                if (moved > threshold * threshold)
                    return;

                if (eventData.dragging)
                    return;
            }

            // Click only if it wasn't a long press.
            if (!_wasLongPressed)
                Clicked?.Invoke(this, eventData != null ? eventData.position : _pointerDownPosition);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!enableInput || _isEmpty)
                return;

            _isPointerDown = false;
            _waitingToShowProgress = false;
            _canceledByScroll = true;
            _longPress?.CancelPress();

            if (showLongPressProgress)
                LongPressProgressHud.End(GetInstanceID());
        }

        private void HandleLongPressCompleted()
        {
            if (_isEmpty || _canceledByScroll)
                return;

            _wasLongPressed = true;
            _isPointerDown = false;
            _waitingToShowProgress = false;

            if (showLongPressProgress)
                LongPressProgressHud.End(GetInstanceID());

            LongPressed?.Invoke(this, _pointerDownPosition);
        }

        private void HandleLongPressProgress(float progress)
        {
            if (!showLongPressProgress)
                return;

            LongPressProgressHud.SetProgress(GetInstanceID(), progress);
        }

        private void HandleLongPressCanceled()
        {
            _waitingToShowProgress = false;

            if (showLongPressProgress)
                LongPressProgressHud.End(GetInstanceID());
        }

        private void CancelByScroll()
        {
            _canceledByScroll = true;
            _isPointerDown = false;
            _waitingToShowProgress = false;
            _longPress?.CancelPress();

            if (showLongPressProgress)
                LongPressProgressHud.End(GetInstanceID());
        }
    }
}
