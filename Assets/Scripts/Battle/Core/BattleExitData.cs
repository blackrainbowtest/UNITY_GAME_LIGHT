namespace Game.Battle
{
    /// <summary>
    /// Data for leaving the battle scene.
    /// Set this before loading the battle scene.
    /// </summary>
    public sealed class BattleExitData
    {
        public string ReturnSceneName { get; }

        public BattleExitData(string returnSceneName)
        {
            ReturnSceneName = returnSceneName;
        }
    }
}
