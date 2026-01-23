namespace UDA2.UI.SaveLoad
{
    /// <summary>
    /// Pure logic: decides how each slot should look/behave (locked/autosave/title/meta texts)
    /// without touching Unity UI components.
    /// </summary>
    public static class SaveSlotsPresenter
    {
        private const int AutosaveSlotId = 0;

        public static SaveSlotViewModel Build(SaveLoadMode mode, int slotId)
        {
            bool isAutosave = slotId == AutosaveSlotId;
            bool hasSave = SaveSlotsManager.HasSave(slotId);

            bool isLocked;
            if (mode == SaveLoadMode.Load)
            {
                // In Load mode autosave is loadable, empty manual slots are locked.
                isLocked = !hasSave;
            }
            else
            {
                // In Save mode autosave cannot be overwritten.
                isLocked = isAutosave;
            }

            // Title always identifies the slot.
            string titleKey = $"save_load_slot_{slotId}";
            string emptyKey = "save_load_empty";

            string saveTimeText;
            string levelGoldText;

            if (hasSave)
            {
                var meta = SaveSlotsManager.GetMeta(slotId);
                if (meta != null)
                {
                    saveTimeText = meta.saveTime;
                    levelGoldText = $"Lv {meta.playerLevel} • Gold {meta.playTimeSeconds}";
                }
                else
                {
                    // Corrupted/missing meta: show as empty-ish but keep HasSave=true so user can overwrite.
                    saveTimeText = "—";
                    levelGoldText = "—";
                }
            }
            else
            {
                saveTimeText = null; // view will show EmptyKey
                levelGoldText = "—";
            }

            return new SaveSlotViewModel(
                slotId: slotId,
                isAutosave: isAutosave,
                isLocked: isLocked,
                hasSave: hasSave,
                titleKey: titleKey,
                emptyKey: emptyKey,
                saveTimeText: saveTimeText,
                levelGoldText: levelGoldText);
        }
    }
}
