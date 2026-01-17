namespace Game.Battle.Combat.Actions
{
    /// <summary>
    /// Stable identifiers for combat actions.
    /// UI sends only these IDs. Combat decides what they do.
    /// </summary>
    public enum CombatActionId
    {
        FastAttack = 0,
        NormalAttack = 1,
        HeavyAttack = 2,
        CounterAttack = 3,

        Block = 10,

        FireSpell = 20,
        IceSpell = 21,
        HolySpell = 22,
        DarkSpell = 23,

        SeductionAct1 = 30,
        SeductionAct2 = 31,
        SeductionAct3 = 32
    }
}
