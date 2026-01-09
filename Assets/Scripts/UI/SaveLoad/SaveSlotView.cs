using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TODO: fix comments and remove debugs
namespace UDA2.UI.SaveLoad
{
    public class SaveSlotView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private TMP_Text slotTitle;
        [SerializeField] private TMP_Text saveTimeText;
        [SerializeField] private TMP_Text levelGoldText;

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

            if (longPress != null)
                longPress.LongPressed += () => {
                    Debug.Log($"SaveSlotView: LongPressed {SlotId}");
                    LongPressed?.Invoke(SlotId);
                };
        }

        private void OnDestroy()
        {
            if (primaryButton != null)
                primaryButton.onClick.RemoveListener(OnPrimaryButtonClicked);
        }

        private void OnPrimaryButtonClicked()
        {
            Debug.Log($"SaveSlotView: PrimaryClicked {SlotId}");
            PrimaryClicked?.Invoke(SlotId);
        }

        public event Action<int> LongPressed;

        public void SetEmpty(int slotId)
        {
            SlotId = slotId;
            var setter = slotTitle.GetComponent<LocalizedTextSetter>();
            if (setter != null)
            {
                setter.key = "save_load_empty";
                setter.UpdateText();
            }
            var comp = slotTitle.GetComponent<LocalizedTextComponent>();
            if (comp != null)
            {
                comp.textKey = "save_load_empty";
                comp.UpdateText();
            }
            saveTimeText.text = "—";
            levelGoldText.text = "—";
            primaryButtonText.text = "—";
        }

        public void SetData(int slotId, SaveMeta meta)
        {
            SlotId = slotId;
            var setter = slotTitle.GetComponent<LocalizedTextSetter>();
            if (setter != null)
            {
                setter.key = $"save_load_slot_{slotId}";
                setter.UpdateText();
            }
            var comp = slotTitle.GetComponent<LocalizedTextComponent>();
            if (comp != null)
            {
                comp.textKey = $"save_load_slot_{slotId}";
                comp.UpdateText();
            }
            saveTimeText.text = meta.saveTime;
            levelGoldText.text = $"Lv {meta.playerLevel} • Gold {meta.playTimeSeconds}";
            primaryButtonText.text = "Load";
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
