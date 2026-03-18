using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UDA2.SaveSystem.Guild;

namespace UDA2.UI.Guild
{
    public sealed class GuildRegistrarResourceRowView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text itemIdText;
        [SerializeField] private TMP_Text requiredText;
        [SerializeField] private TMP_Text currentText;

        public void Render(GuildItemRequirementProgress req, Sprite icon)
        {
            if (req == null)
                return;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (itemIdText != null)
                itemIdText.text = req.itemId ?? string.Empty;

            if (requiredText != null)
                requiredText.text = req.required.ToString();

            if (currentText != null)
                currentText.text = $"{req.totalOwned} ({req.inventoryOwned}+{req.storageOwned})";

            if (currentText != null)
                currentText.color = req.isMet ? new Color(0.25f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.3f, 0.3f, 1f);
        }
    }
}
