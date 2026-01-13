namespace Game.Battle
{
    /// <summary>
    /// Runtime context for passing battle entry parameters between scenes.
    /// One-time use, resets after consume.
    /// </summary>
    public static class BattleEntryContext
    {
        private static BattleMode mode = BattleMode.Normal;

        public static void Set(BattleMode battleMode)
        {
            mode = battleMode;
        }

        public static BattleMode Consume()
        {
            var result = mode;
            mode = BattleMode.Normal; // reset to default
            return result;
        }
    }
}
