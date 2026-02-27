using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UDA2.UI.Common;
using UDA2.SaveSystem;
using UDA2.GameTime;

namespace UDA2.UI.SaveLoad
{
    public class SaveLoadModalController : MonoBehaviour
    {

        [Header("UI References")]
        [SerializeField] private Transform slotsParent; // Enables dynamic slot layout
        [SerializeField] private GameObject slotTemplate; // Allows flexible slot instantiation
        [SerializeField] private Button closeButton; // Prevents modal from blocking UI
        [SerializeField] private TMP_Text headerText; // Ensures correct context for user

        private SaveLoadMode currentMode;
        private List<SaveSlotView> slotViews = new List<SaveSlotView>(); // Enables batch slot updates
        [Header("Long Press Progress")]
        [SerializeField] private LongPressProgressView progressViewPrefab;
        [SerializeField] private Transform canvasTransform;
        private const int SlotCount = 11; // Limits save slots for UX clarity
        private bool isInitialized = false; // Prevents redundant UI setup

        [Header("Dialog")]
        [SerializeField] private UDA2.UI.Common.ConfirmDialog confirmDialogPrefab;
        [SerializeField] private Transform dialogParent;

        /// <summary>
        /// Displays modal, blocking background interaction until closed.
        /// </summary>
        public static SaveLoadModalController Show(SaveLoadMode mode)
        {
            var prefab = Resources.Load<SaveLoadModalController>("Prefabs/UI/SaveLoad/SaveLoadModal");
            var instance = Instantiate(prefab);
            instance.OpenInternal(mode);
            return instance;
        }

        /// <summary>
        /// Prepares modal for user action, guaranteeing up-to-date slot state.
        /// </summary>
        private void OpenInternal(SaveLoadMode mode)
        {
            currentMode = mode;
            gameObject.SetActive(true); // Prevents accidental background interaction

            // Ensure we always have a valid SaveData instance when opening the modal.
            // This modal can be opened from scenes where GameBootstrapper might not have run.
            if (global::GameState.Instance.CurrentSave == null)
            {
                global::GameState.Instance.CurrentSave = SaveData.CreateDefault(Application.version);
            }

            if (!isInitialized)
            {
                InitSlots();
                isInitialized = true;
                if (closeButton != null)
                    closeButton.onClick.AddListener(Close); // Allows user to exit modal
            }
            RefreshSlots();
            // Guarantees correct header localization for current mode
            var setter = headerText.GetComponent<LocalizedTextSetter>();
            if (setter != null)
            {
                setter.key = mode == SaveLoadMode.Load ? "save_load_title_load" : "save_load_title_save";
                setter.UpdateText();
            }
            var comp = headerText.GetComponent<LocalizedTextComponent>();
            if (comp != null)
            {
                comp.textKey = mode == SaveLoadMode.Load ? "save_load_title_load" : "save_load_title_save";
                comp.UpdateText();
            }
        }

        /// <summary>
        /// Populates modal with interactive slots, enabling save/load actions.
        /// </summary>
        private void InitSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var slotObj = Instantiate(slotTemplate, slotsParent);
                slotObj.SetActive(true);
                var slotView = slotObj.GetComponent<SaveSlotView>();
                // Instantiate progress view prefab for each slot
                var progressViewInstance = Instantiate(progressViewPrefab, canvasTransform);
                progressViewInstance.gameObject.SetActive(false);
                slotView.SetProgressView(progressViewInstance);
                slotView.SetAutosave(i == 0);
                slotView.SetLocked(i == 0); // Prevents user from modifying autosave slot
                slotView.SetEmpty(i);
                slotView.PrimaryClicked += OnSlotClicked; // Enables slot selection
                slotView.LongPressed += OnSlotLongPressed; // Enables slot deletion
                slotViews.Add(slotView);
            }
        }

        /// <summary>
        /// Reflects current save/load state, preventing invalid actions.
        /// </summary>
        private void RefreshSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var slotView = slotViews[i];

                // Set the save mode to ensure correct operation of clicks on empty slots
                slotView.SetSaveMode(currentMode == SaveLoadMode.Save);

                int slotId = i;
                var model = SaveSlotsPresenter.Build(currentMode, slotId);
                slotView.Render(model);
            }
        }

        /// <summary>
        /// Loads game or overwrites save, depending on user intent.
        /// </summary>
        private void OnSlotClicked(int slotId)
        {
            if (currentMode == SaveLoadMode.Load)
            {
                if (!SaveSlotsManager.HasSave(slotId)) return;
                var save = SaveSlotsManager.LoadFromSlot(slotId);
                if (save != null)
                {
                    GameState.Instance.CurrentSave = save;
                    GameTimeService.Instance?.SyncFromCurrentSave();

                    var targetScene = save.player.sceneName;
                    if (save.sceneState != null && save.sceneState.pendingBattle != null && save.sceneState.pendingBattle.isPending)
                    {
                        if (!string.IsNullOrEmpty(save.sceneState.pendingBattle.battleSceneName))
                            targetScene = save.sceneState.pendingBattle.battleSceneName;
                    }

                    // Ensures correct scene transition after load
                    if (UDA2.SceneFlow.SceneFlowManager.Instance != null)
                        UDA2.SceneFlow.SceneFlowManager.Instance.LoadScene(targetScene);
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
                    Close();
                }
            }
            else // Save Mode
            {
                if (slotId == 0) return; // Prevents overwriting autosave

                var activeScene = SceneManager.GetActiveScene().name;
                if (!SceneCategoryResolver.IsSaveAllowed(activeScene))
                {
                    ConfirmDialog.Show(
                        confirmDialogPrefab,
                        dialogParent != null ? dialogParent : transform,
                        "Сохранение в этой сцене недоступно (бой/катсцена). Сохраняйся в городе или безопасной локации.",
                        onYes: null,
                        onNo: null
                    );
                    return;
                }

                if (!SaveSlotsManager.HasSave(slotId))
                {
                    SaveSlotsManager.SaveToSlot(slotId, GameState.Instance.CurrentSave);
                    RefreshSlots();
                }
                else
                {
                    ConfirmDialog.Show(
                        confirmDialogPrefab,
                        dialogParent != null ? dialogParent : transform,
                        "confirm_overwrite_save",
                        onYes: () => { SaveSlotsManager.SaveToSlot(slotId, GameState.Instance.CurrentSave); RefreshSlots(); },
                        onNo: null
                    );
                }
            }
        }

        /// <summary>
        /// Prevents accidental deletion by requiring confirmation.
        /// </summary>
        private void OnSlotLongPressed(int slotId)
        {
            if (slotId == 0) return; // Disables deletion for autosave
            if (!SaveSlotsManager.HasSave(slotId)) return; // Prevents deletion of empty slot
            ConfirmDialog.Show(
                confirmDialogPrefab,
                dialogParent != null ? dialogParent : transform,
                "confirm_delete_save",
                onYes: () => { SaveSlotsManager.DeleteSlot(slotId); RefreshSlots(); },
                onNo: null
            );
            if (slotId >= 0 && slotId < slotViews.Count)
                slotViews[slotId].ResetLongPressFlag();
        }

        /// <summary>
        /// Restores background interaction and cleans up modal.
        /// </summary>
        public void Close()
        {
            Destroy(gameObject);
        }
    }

    // LongPressHandler теперь должен быть частью SlotTemplate prefab и SaveSlotView пробрасывает LongPressed наружу
}
