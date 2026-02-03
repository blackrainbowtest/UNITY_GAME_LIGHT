public static class StorageCapacityRules
{
    public const int BaseSlots = 100;

    public static int GetCapacity(SaveData save)
    {
        // Reserved for future rules (upgrades, perks, etc.).
        return BaseSlots;
    }
}
