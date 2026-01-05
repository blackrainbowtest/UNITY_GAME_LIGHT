using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UDA2.UI.Common;

namespace UDA2.UI.SaveLoad
{
    public class SaveLoadModalController : MonoBehaviour
    {
        public enum Mode { Load, Save }

        [Header("UI References")]
        [SerializeField] private Transform slotsParent; // Контейнер для слотов
        [SerializeField] private GameObject slotTemplate; // Prefab кнопки-слота (выключен по умолчанию)
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text headerText;

        // ConfirmDialog теперь вызывается как сервис через статический метод

        private Mode currentMode;
        private List<SaveSlotView> slotViews = new List<SaveSlotView>();
        private const int SlotCount = 11;
        private bool isInitialized = false;

        public static SaveLoadModalController Show(Mode mode)
        {
            var prefab = Resources.Load<SaveLoadModalController>("Prefabs/UI/SaveLoad/SaveLoadModal");
            var instance = Instantiate(prefab);
            instance.OpenInternal(mode);
            return instance;
        }

        private void OpenInternal(Mode mode)
        {
            currentMode = mode;
            gameObject.SetActive(true); // Гарантируем видимость модального окна
            if (!isInitialized)
            {
                InitSlots();
                isInitialized = true;
                if (closeButton != null)
                    closeButton.onClick.AddListener(Close);
            }
            RefreshSlots();
            // Локализация через компонент (например, LocalizedTextSetter)
            // Временно прямое присваивание ключа локализации
            headerText.text = mode == Mode.Load ? "save_load_title_load" : "save_load_title_save";
        }

        private void InitSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var slotObj = Instantiate(slotTemplate, slotsParent);
                slotObj.SetActive(true);
                var slotView = slotObj.GetComponent<SaveSlotView>();
                slotView.SetAutosave(i == 0);
                slotView.SetLocked(i == 0); // автосейв сразу заблокирован
                slotView.SetEmpty(i);
                slotView.PrimaryClicked += OnSlotClicked;
                slotView.LongPressed += OnSlotLongPressed;
                slotViews.Add(slotView);
            }
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var slotView = slotViews[i];
                int slotId = i;
                bool hasSave = SaveSlotsManager.HasSave(slotId);
                if (slotId == 0)
                {
                    // Автосейв
                    slotView.SetAutosave(true);
                    slotView.SetLocked(true);
                    if (hasSave)
                        slotView.SetData(slotId, SaveSlotsManager.GetMeta(slotId));
                    else
                        slotView.SetEmpty(slotId);
                }
                else
                {
                    slotView.SetAutosave(false);
                    slotView.SetLocked(false);
                    if (hasSave)
                        slotView.SetData(slotId, SaveSlotsManager.GetMeta(slotId));
                    else
                        slotView.SetEmpty(slotId);
                }
            }
        }

        private void OnSlotClicked(int slotId)
        {
            if (currentMode == Mode.Load)
            {
                if (!SaveSlotsManager.HasSave(slotId)) return;
                var save = SaveSlotsManager.LoadFromSlot(slotId);
                if (save != null)
                {
                    GameState.Instance.CurrentSave = save;
                    // Здесь можно вызвать переход в нужную сцену или событие
                    Debug.Log($"Loaded save from slot {slotId}");
                    Close();
                }
            }
            else // Save Mode
            {
                if (slotId == 0) return; // автосейв заблокирован
                if (!SaveSlotsManager.HasSave(slotId))
                {
                    SaveSlotsManager.SaveToSlot(slotId, GameState.Instance.CurrentSave);
                    RefreshSlots();
                }
                else
                {
                    ConfirmDialog.Show(
                        "confirm_overwrite_save",
                        onYes: () => { SaveSlotsManager.SaveToSlot(slotId, GameState.Instance.CurrentSave); RefreshSlots(); },
                        onNo: null
                    );
                }
            }
        }

        private void OnSlotLongPressed(int slotId)
        {
            if (slotId == 0) return; // автосейв нельзя удалить
            if (!SaveSlotsManager.HasSave(slotId)) return;
            ConfirmDialog.Show(
                "confirm_delete_save",
                onYes: () => { SaveSlotsManager.DeleteSlot(slotId); RefreshSlots(); },
                onNo: null
            );
        }

        public void Close()
        {
            Destroy(gameObject);
        }
    }

    // LongPressHandler теперь должен быть частью SlotTemplate prefab и SaveSlotView пробрасывает LongPressed наружу
}
