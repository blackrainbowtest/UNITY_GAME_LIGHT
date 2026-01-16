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
        public int MaxMP { get; }
        public int CurrentMP { get; }
        public int MaxSP { get; }
        public int CurrentSP { get; }
        public int MaxLP { get; }
        public int CurrentLP { get; }

        public PlayerCombatSnapshot(int maxHp, int currentHp, int maxMp = 0, int currentMp = 0, int maxSp = 0, int currentSp = 0, int maxLp = 0, int currentLp = 0)
        {
            MaxHP = maxHp;
            CurrentHP = currentHp;
            MaxMP = maxMp;
            CurrentMP = currentMp;
            MaxSP = maxSp;
            CurrentSP = currentSp;
            MaxLP = maxLp;
            CurrentLP = currentLp;
        }
    }
}
