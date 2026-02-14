public static class LocationStructureStateService
{
    public static void EnsureInitialized(SaveData save)
    {
        if (save == null)
            return;

        if (save.locationStructures == null)
            save.locationStructures = new SaveData.LocationStructuresState();

        var state = save.locationStructures;
        if (state.bedLevel < 0) state.bedLevel = 0;
        if (state.campfireLevel < 0) state.campfireLevel = 0;
        if (state.workbenchLevel < 0) state.workbenchLevel = 0;
        if (state.storageLevel < 0) state.storageLevel = 0;
    }
}
