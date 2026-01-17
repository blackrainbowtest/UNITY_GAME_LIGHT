using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// Data-only battle outcome for UI.
    /// </summary>
    public sealed class BattleResultData
    {
        public bool PlayerWon { get; }
        public int GoldGained { get; }
        public IReadOnlyList<string> ItemIds { get; }

        public BattleResultData(bool playerWon, int goldGained, IReadOnlyList<string> itemIds)
        {
            PlayerWon = playerWon;
            GoldGained = goldGained;
            ItemIds = itemIds;
        }
    }
}
