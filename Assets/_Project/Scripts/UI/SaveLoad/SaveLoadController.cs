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
            autoSlot.Setup(0, true, "AutoSave", GetMetaString(0), "Load", SaveSlotsManager.HasSave(0));
            autoSlot.PrimaryClicked += OnSlotClicked;
            slotViews.Add(autoSlot);

            // Ручные слоты (id 1-10)
            for (int i = 1; i <= ManualSlotsCount; i++)
            {
                var slot = Instantiate(slotPrefab, slotsParent);
                slot.Setup(i, false, $"Slot {i}", GetMetaString(i), "Load", SaveSlotsManager.HasSave(i));
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
                var meta = GetMetaString(slotId);
                slotViews[i].Setup(slotId, slotId == 0, slotId == 0 ? "AutoSave" : $"Slot {slotId}", meta, "Load", hasSave);
                if (!hasSave)
                    slotViews[i].SetEmpty(slotId == 0 ? "AutoSave" : $"Slot {slotId}", "Load", false);
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
