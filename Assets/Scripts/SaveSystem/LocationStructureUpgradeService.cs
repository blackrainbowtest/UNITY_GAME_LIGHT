using System;
using System.Collections.Generic;

public static class LocationStructureUpgradeService
{
    public sealed class ResourceState
    {
        public string itemId;
        public int required;
        public int inventory;
        public int storage;
        public int total;
        public int missing;
    }

    public sealed class UpgradePreview
    {
        public LocationStructureType structureType;
        public int currentLevel;
        public int targetLevel;
        public bool hasNextLevel;
        public bool canUpgrade;
        public List<ResourceState> resources = new List<ResourceState>();
    }

    public static UpgradePreview BuildPreview(SaveData save, LocationStructureType structureType, LocationUpgradeTableAsset table)
    {
        LocationStructureStateService.EnsureInitialized(save);

        var preview = new UpgradePreview
        {
            structureType = structureType,
            currentLevel = LocationStructureLevels.GetLevel(save, structureType),
            hasNextLevel = false,
            canUpgrade = false
        };

        if (table == null)
            return preview;

        int targetLevel = preview.currentLevel + 1;
        preview.targetLevel = targetLevel;

        if (!table.TryGetUpgradeLevel(structureType, targetLevel, out var config) || config == null)
            return preview;

        preview.hasNextLevel = true;
        preview.canUpgrade = true;

        var requirements = config.requiredResources;
        if (requirements == null)
            return preview;

        for (int i = 0; i < requirements.Count; i++)
        {
            var req = requirements[i];
            if (req == null || string.IsNullOrWhiteSpace(req.itemId) || req.amount <= 0)
                continue;

            int inv = GetItemCount(save.inventory != null ? save.inventory.items : null, req.itemId);
            int stor = GetItemCount(save.storage != null ? save.storage.items : null, req.itemId);
            int total = inv + stor;
            int missing = Math.Max(0, req.amount - total);

            preview.resources.Add(new ResourceState
            {
                itemId = req.itemId,
                required = req.amount,
                inventory = inv,
                storage = stor,
                total = total,
                missing = missing
            });

            if (missing > 0)
                preview.canUpgrade = false;
        }

        return preview;
    }

    public static bool TryApplyUpgrade(SaveData save, LocationStructureType structureType, LocationUpgradeTableAsset table, out UpgradePreview preview)
    {
        preview = BuildPreview(save, structureType, table);
        if (!preview.hasNextLevel || !preview.canUpgrade)
            return false;

        for (int i = 0; i < preview.resources.Count; i++)
        {
            var r = preview.resources[i];
            if (r.required <= 0 || string.IsNullOrWhiteSpace(r.itemId))
                continue;

            int remaining = r.required;
            remaining = ConsumeItem(save.inventory != null ? save.inventory.items : null, r.itemId, remaining);
            remaining = ConsumeItem(save.storage != null ? save.storage.items : null, r.itemId, remaining);

            if (remaining > 0)
                return false;
        }

        LocationStructureLevels.SetLevel(save, structureType, preview.targetLevel);
        preview.currentLevel = preview.targetLevel;
        preview.targetLevel = preview.currentLevel + 1;
        return true;
    }

    private static int GetItemCount(List<SaveData.Item> list, string itemId)
    {
        if (list == null || string.IsNullOrWhiteSpace(itemId))
            return 0;

        int count = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            if (item == null)
                continue;
            if (!string.Equals(item.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (item.count > 0)
                count += item.count;
        }

        return count;
    }

    private static int ConsumeItem(List<SaveData.Item> list, string itemId, int amount)
    {
        if (list == null || amount <= 0 || string.IsNullOrWhiteSpace(itemId))
            return amount;

        int remaining = amount;

        for (int i = 0; i < list.Count && remaining > 0; i++)
        {
            var item = list[i];
            if (item == null)
                continue;
            if (!string.Equals(item.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (item.count <= 0)
                continue;

            int take = Math.Min(item.count, remaining);
            item.count -= take;
            remaining -= take;
        }

        // Cleanup zero/negative stacks.
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var item = list[i];
            if (item == null || item.count <= 0)
                list.RemoveAt(i);
        }

        return remaining;
    }
}
