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
        [SerializeField] private GameObject lockOverlay; // визуальная блокировка

        public int SlotId { get; private set; }
        public bool IsAutoSave { get; private set; }

        [Header("Input")]
        [SerializeField] private UDA2.UI.Common.LongPressHandler longPress;

        public event Action<int> PrimaryClicked;

        private void Awake()
        {
            if (primaryButton != null)
                primaryButton.onClick.AddListener(OnPrimaryButtonClicked);

            // 'button' заменено на 'primaryButton' для совместимости с объявлением
            if (primaryButton != null)
                primaryButton.onClick.AddListener(() => PrimaryClicked?.Invoke(SlotId));

            if (longPress != null)
                longPress.LongPressed += () => LongPressed?.Invoke(SlotId);
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

        public event Action<int> LongPressed;

        public void SetEmpty(int slotId)
        {
            SlotId = slotId;
            slotTitle.text = "save_slot_empty";
            metaInfo.text = "—";
            primaryButtonText.text = "—";
        }

        public void SetData(int slotId, SaveMeta meta)
        {
            SlotId = slotId;
            // Здесь можно использовать форматтеры/локализацию
            metaInfo.text = meta.saveTime;
            primaryButtonText.text = $"Lv {meta.version} • Gold {meta.playTimeSeconds}";
        }

        public void SetAutosave(bool isAutosave)
        {
            if (lockBadge != null)
                lockBadge.SetActive(isAutosave);
        }

        public void SetLocked(bool locked)
        {
            if (lockOverlay != null)
                lockOverlay.SetActive(locked);
            if (primaryButton != null)
                primaryButton.interactable = !locked;
        }
    }
}
