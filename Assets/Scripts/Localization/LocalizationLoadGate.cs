using System.Collections.Generic;

public static class LocalizationLoadGate
{
    private static readonly Queue<LocalizedGlobalComponent> Pending = new();
    private static readonly HashSet<int> PendingIds = new();
    private static bool _isDeferring;

    public static bool IsDeferring => _isDeferring;
    public static bool HasPending => Pending.Count > 0;

    public static void BeginDeferring()
    {
        _isDeferring = true;
        Pending.Clear();
        PendingIds.Clear();
    }

    public static void EndDeferring()
    {
        _isDeferring = false;
    }

    public static void Register(LocalizedGlobalComponent component)
    {
        if (!_isDeferring || component == null)
            return;

        int id = component.GetInstanceID();
        if (!PendingIds.Add(id))
            return;

        Pending.Enqueue(component);
    }

    public static void Unregister(LocalizedGlobalComponent component)
    {
        if (component == null)
            return;

        PendingIds.Remove(component.GetInstanceID());
    }

    public static int DrainBatch(int maxCount)
    {
        if (maxCount <= 0)
            return 0;

        int drained = 0;
        while (drained < maxCount && Pending.Count > 0)
        {
            var component = Pending.Dequeue();
            if (component == null)
                continue;

            int id = component.GetInstanceID();
            PendingIds.Remove(id);

            if (!component.isActiveAndEnabled)
                continue;

            component.UpdateText();
            drained++;
        }

        return drained;
    }
}
