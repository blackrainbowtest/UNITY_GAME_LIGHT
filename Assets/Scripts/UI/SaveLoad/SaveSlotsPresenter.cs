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
                    saveTimeText = NormalizeSaveTime(meta.saveTime);
                    levelGoldText = BuildLevelGoldText(meta.playerLevel, meta.playerGold);
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

        private static string BuildLevelGoldText(int level, int gold)
        {
            var settings = UDA2.Core.SettingsContext.Current;
            string lang = (settings == null || string.IsNullOrEmpty(settings.language)) ? "en" : settings.language;

            var provider = UIStringsProvider.Instance;
            if (provider != null)
                return provider.GetFormatted("save_load_level_gold", lang, level, gold);

            // Hard fallback if localization provider isn't available.
            return $"Lv {level} • Gold {gold}";
        }

        private static string NormalizeSaveTime(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "—";

            // Backward compatibility: older saves used ISO like "2026-02-04T12:34:56Z".
            var s = value.Replace('T', ' ');
            if (s.EndsWith("Z"))
                s = s.Substring(0, s.Length - 1);
            return s;
        }
    }
}
