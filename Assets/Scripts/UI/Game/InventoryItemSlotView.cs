using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    public sealed class InventoryItemSlotView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject filledStateRoot;

        public void RenderEmpty()
        {
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
            if (emptyStateRoot != null) emptyStateRoot.SetActive(false);
            if (filledStateRoot != null) filledStateRoot.SetActive(true);

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (countText != null)
            {
                countText.text = count > 1 ? count.ToString() : string.Empty;
            }
        }
    }
}
