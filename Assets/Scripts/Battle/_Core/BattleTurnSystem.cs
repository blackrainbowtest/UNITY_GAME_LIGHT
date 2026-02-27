namespace Game.Battle
{
    public enum BattleTurnPhase
    {
        NotStarted = 0,
        PlayerTurn = 1,
        EnemyTurn = 2,
        BattleOver = 3
    }

    public sealed class BattleTurnSystem
    {
        public BattleTurnPhase Phase { get; private set; } = BattleTurnPhase.NotStarted;

        public bool IsPlayerTurn => Phase == BattleTurnPhase.PlayerTurn;
        public bool IsEnemyTurn => Phase == BattleTurnPhase.EnemyTurn;
        public bool IsBattleOver => Phase == BattleTurnPhase.BattleOver;

        public void Reset()
        {
            Phase = BattleTurnPhase.NotStarted;
        }

        public void BeginPlayerTurn()
        {
            Phase = BattleTurnPhase.PlayerTurn;
        }

        public void BeginEnemyTurn()
        {
            Phase = BattleTurnPhase.EnemyTurn;
        }

        public void EndBattle()
        {
            Phase = BattleTurnPhase.BattleOver;
        }
    }
}
