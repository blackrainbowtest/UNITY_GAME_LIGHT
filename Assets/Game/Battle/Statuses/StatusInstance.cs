namespace Game.Battle.Statuses
{
    /// <summary>
    /// Data-only runtime instance of an active status.
    /// </summary>
    public readonly struct StatusInstance
    {
        public StatusEffectId Id { get; }
        public int TurnsLeft { get; }

        public StatusInstance(StatusEffectId id, int turnsLeft)
        {
            Id = id;
            TurnsLeft = turnsLeft;
        }

        public StatusInstance WithTurnsLeft(int turnsLeft)
            => new StatusInstance(Id, turnsLeft);
    }
}
