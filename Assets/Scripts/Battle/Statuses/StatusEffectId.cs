namespace Game.Battle.Statuses
{
    // STATUSES
    /// <summary>
    /// Stable IDs for status effects.
    /// Used by combat and UI.
    /// </summary>
    public enum StatusEffectId
    {
        Poison = 0,         // яд
        Bleeding = 1,       // кровотечение
        Burning = 2,        // ожог
        Freeze = 3,         // заморозка
        Silence = 4,        // молчание
        Block = 5,          // блок
        PassiveHeal = 6,    // пассивное лечение
        Taunt = 7,          // провокация
        Charm = 8,          // очарование
        Weaken = 9,         // ослабление
    }
}
