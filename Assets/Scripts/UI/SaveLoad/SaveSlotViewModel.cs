namespace UDA2.UI.SaveLoad
{
    public readonly struct SaveSlotViewModel
    {
        public readonly int SlotId;
        public readonly bool IsAutosave;
        public readonly bool IsLocked;
        public readonly bool HasSave;

        // Localization keys
        public readonly string TitleKey;
        public readonly string EmptyKey;

        // Display strings (already formatted)
        public readonly string SaveTimeText;
        public readonly string LevelGoldText;

        public SaveSlotViewModel(
            int slotId,
            bool isAutosave,
            bool isLocked,
            bool hasSave,
            string titleKey,
            string emptyKey,
            string saveTimeText,
            string levelGoldText)
        {
            SlotId = slotId;
            IsAutosave = isAutosave;
            IsLocked = isLocked;
            HasSave = hasSave;
            TitleKey = titleKey;
            EmptyKey = emptyKey;
            SaveTimeText = saveTimeText;
            LevelGoldText = levelGoldText;
        }
    }
}
