using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.SaveLoad
{
    public class SaveSlotView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TMP_Text slotTitle;
        [SerializeField] private TMP_Text metaInfo;

        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryButtonText;

        [Header("Optional visuals")]
        [SerializeField] private GameObject lockBadge; // show for autosave if needed

        public int SlotId { get; private set; }
        public bool IsAutoSave { get; private set; }

        public event Action<int> PrimaryClicked;

        private void Awake()
        {
            if (primaryButton != null)
                primaryButton.onClick.AddListener(OnPrimaryButtonClicked);
        }

        private void OnDestroy()
        {
            if (primaryButton != null)
                primaryButton.onClick.RemoveListener(OnPrimaryButtonClicked);
        }

        private void OnPrimaryButtonClicked()
        {
            PrimaryClicked?.Invoke(SlotId);
        }

        public void Setup(
            int slotId,
            bool isAutoSave,
            string title,
            string meta,
            string primaryLabel,
            bool primaryInteractable)
        {
            SlotId = slotId;
            IsAutoSave = isAutoSave;

            if (slotTitle != null) slotTitle.text = title;
            if (metaInfo != null) metaInfo.text = meta;

            if (primaryButtonText != null) primaryButtonText.text = primaryLabel;
            if (primaryButton != null) primaryButton.interactable = primaryInteractable;

            if (lockBadge != null) lockBadge.SetActive(isAutoSave);
        }

        // Удобно для "пустого слота"
        public void SetEmpty(string title, string primaryLabel, bool primaryInteractable)
        {
            Setup(SlotId, IsAutoSave, title, "Empty", primaryLabel, primaryInteractable);
        }
    }
}
