namespace Game.Battle
{
    /// <summary>
    /// Immutable snapshot of player combat state at battle start.
    /// Extended later with equipment, buffs, etc.
    /// </summary>
    public class PlayerCombatSnapshot
    {
        public int MaxHP { get; }
        public int CurrentHP { get; }

        public PlayerCombatSnapshot(int maxHp, int currentHp)
        {
            MaxHP = maxHp;
            CurrentHP = currentHp;
        }
    }
}
