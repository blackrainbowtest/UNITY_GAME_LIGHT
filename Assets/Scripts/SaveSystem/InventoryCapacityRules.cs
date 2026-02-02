public static class InventoryCapacityRules
{
    public const int BaseSlots = 10;

    /// <summary>
    /// Temporary rule: any equipped bag grants +10 slots.
    /// Later this can be data-driven from item definitions.
    /// </summary>
    public const int DefaultBagBonusSlots = 10;

    public static int GetCapacity(SaveData save)
    {
        if (save == null)
            return BaseSlots;

        var eq = save.player != null ? save.player.equipment : null;
        var bagBonus = (eq != null && !string.IsNullOrEmpty(eq.bagItemId))
            ? DefaultBagBonusSlots
            : 0;

        return BaseSlots + bagBonus;
    }
}
