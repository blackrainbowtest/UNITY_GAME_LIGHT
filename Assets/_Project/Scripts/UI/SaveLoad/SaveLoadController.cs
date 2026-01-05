using System.Collections.Generic;
using UnityEngine;
using UDA2.UI.SaveLoad;

namespace UDA2.UI.SaveLoad
{
    public class SaveLoadController : MonoBehaviour
    {
        [Header("Slot Prefab & Parent")]
        [SerializeField] private SaveSlotView slotPrefab;
        [SerializeField] private Transform slotsParent;

        private const int ManualSlotsCount = 10;
        private List<SaveSlotView> slotViews = new List<SaveSlotView>();

        private void Start()
        {
            CreateSlots();
            RefreshSlots();
        }

        private void CreateSlots()
        {
            slotViews.Clear();
            // Автосейв (id 0)
            var autoSlot = Instantiate(slotPrefab, slotsParent);
            autoSlot.SetAutosave(true);
            autoSlot.SetLocked(true);
            if (SaveSlotsManager.HasSave(0))
                autoSlot.SetData(0, SaveSlotsManager.GetMeta(0));
            else
                autoSlot.SetEmpty(0);
            autoSlot.PrimaryClicked += OnSlotClicked;
            slotViews.Add(autoSlot);

            // Ручные слоты (id 1-10)
            for (int i = 1; i <= ManualSlotsCount; i++)
            {
                var slot = Instantiate(slotPrefab, slotsParent);
                slot.SetAutosave(false);
                slot.SetLocked(false);
                if (SaveSlotsManager.HasSave(i))
                    slot.SetData(i, SaveSlotsManager.GetMeta(i));
                else
                    slot.SetEmpty(i);
                slot.PrimaryClicked += OnSlotClicked;
                slotViews.Add(slot);
            }
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < slotViews.Count; i++)
            {
                int slotId = slotViews[i].SlotId;
                bool hasSave = SaveSlotsManager.HasSave(slotId);
                if (slotId == 0)
                {
                    slotViews[i].SetAutosave(true);
                    slotViews[i].SetLocked(true);
                    if (hasSave)
                        slotViews[i].SetData(slotId, SaveSlotsManager.GetMeta(slotId));
                    else
                        slotViews[i].SetEmpty(slotId);
                }
                else
                {
                    slotViews[i].SetAutosave(false);
                    slotViews[i].SetLocked(false);
                    if (hasSave)
                        slotViews[i].SetData(slotId, SaveSlotsManager.GetMeta(slotId));
                    else
                        slotViews[i].SetEmpty(slotId);
                }
            }
        }

        private string GetMetaString(int slotId)
        {
            var meta = SaveSlotsManager.GetMeta(slotId);
            if (meta == null) return "Empty";
            return $"{meta.saveTime}\nPlaytime: {meta.playTimeSeconds / 60} min";
        }

        private void OnSlotClicked(int slotId)
        {
            // Пример: загрузка сейва
            var save = SaveSlotsManager.LoadFromSlot(slotId);
            if (save != null)
            {
                GameState.Instance.CurrentSave = save;
                // Здесь можно вызвать переход в нужную сцену или событие
                Debug.Log($"Loaded save from slot {slotId}");
            }
        }
    }
}
