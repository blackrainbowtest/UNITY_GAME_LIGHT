public static class LocationStructureLevels
{
    public static int GetLevel(SaveData save, LocationStructureType structureType)
    {
        if (save == null)
            return 0;

        if (save.locationStructures == null)
            save.locationStructures = new SaveData.LocationStructuresState();

        var state = save.locationStructures;
        return structureType switch
        {
            LocationStructureType.Bed => state.bedLevel,
            LocationStructureType.Campfire => state.campfireLevel,
            LocationStructureType.Workbench => state.workbenchLevel,
            LocationStructureType.Storage => state.storageLevel,
            _ => 0
        };
    }

    public static void SetLevel(SaveData save, LocationStructureType structureType, int level)
    {
        if (save == null)
            return;

        if (save.locationStructures == null)
            save.locationStructures = new SaveData.LocationStructuresState();

        var clampedLevel = level < 0 ? 0 : level;
        var state = save.locationStructures;

        switch (structureType)
        {
            case LocationStructureType.Bed:
                state.bedLevel = clampedLevel;
                break;
            case LocationStructureType.Campfire:
                state.campfireLevel = clampedLevel;
                break;
            case LocationStructureType.Workbench:
                state.workbenchLevel = clampedLevel;
                break;
            case LocationStructureType.Storage:
                state.storageLevel = clampedLevel;
                break;
        }
    }
}
