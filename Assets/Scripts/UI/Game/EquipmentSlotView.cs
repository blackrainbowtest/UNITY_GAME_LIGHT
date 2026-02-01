using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    [DisallowMultipleComponent]
    public sealed class EquipmentSlotView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private EquipmentSlotId slotId;

        [Header("Empty Visual")]
        [Tooltip("Optional icon to show when no item is equipped.")]
        [SerializeField] private Sprite emptyIcon;
        [SerializeField] private bool showEmptyIcon = true;

        [Header("Wiring")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text itemNameText;

        public EquipmentSlotId SlotId => slotId;

        public event Action<EquipmentSlotId> Clicked;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            Clicked?.Invoke(slotId);
        }

        public void SetLabel(string text)
        {
            if (labelText != null)
                labelText.text = text ?? string.Empty;
        }

        public void RenderEmpty(string emptyText = "—")
        {
            if (itemNameText != null)
                itemNameText.text = string.IsNullOrEmpty(emptyText) ? "—" : emptyText;

            if (iconImage != null)
            {
                iconImage.sprite = showEmptyIcon ? emptyIcon : null;
                iconImage.enabled = showEmptyIcon && emptyIcon != null;
            }
        }

        public void RenderItem(string displayName, Sprite icon)
        {
            if (itemNameText != null)
                itemNameText.text = string.IsNullOrEmpty(displayName) ? "—" : displayName;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
        }
    }
}
