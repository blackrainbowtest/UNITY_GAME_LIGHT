namespace Game.Battle.Combat.Actions
{
    /// <summary>
    /// Data-only description of a combat action.
    /// Contains costs, damage and requirements. No logic.
    /// </summary>
    public sealed class CombatActionData
    {
        public CombatActionId Id { get; }
        public CombatActionCategory Category { get; }

        public int HpDamage { get; }
        public int MpCost { get; }
        public int SpCost { get; }
        public int LpCost { get; }

        public bool RequiresPlayerBlockedLastTurn { get; }

        public CombatActionData(
            CombatActionId id,
            CombatActionCategory category,
            int hpDamage,
            int mpCost,
            int spCost,
            int lpCost,
            bool requiresPlayerBlockedLastTurn)
        {
            Id = id;
            Category = category;
            HpDamage = hpDamage;
            MpCost = mpCost;
            SpCost = spCost;
            LpCost = lpCost;
            RequiresPlayerBlockedLastTurn = requiresPlayerBlockedLastTurn;
        }
    }
}
