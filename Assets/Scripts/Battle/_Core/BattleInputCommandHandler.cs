using Game.Battle.Combat.Actions;

namespace Game.Battle
{
    public sealed class BattleInputCommandHandler
    {
        private readonly System.Func<bool> isBattleStarted;
        private readonly System.Func<bool> isPlayerTurn;
        private readonly System.Action onAttack;
        private readonly System.Action<CombatActionId> onCombatActionSelected;
        private readonly System.Action onItem;
        private readonly System.Action onRun;
        private readonly System.Action onSurrender;
        private readonly System.Action onSkipTurn;
        private readonly System.Action onExit;

        public BattleInputCommandHandler(
            System.Func<bool> isBattleStarted,
            System.Func<bool> isPlayerTurn,
            System.Action onAttack,
            System.Action<CombatActionId> onCombatActionSelected,
            System.Action onItem,
            System.Action onRun,
            System.Action onSurrender,
            System.Action onSkipTurn,
            System.Action onExit)
        {
            this.isBattleStarted = isBattleStarted;
            this.isPlayerTurn = isPlayerTurn;
            this.onAttack = onAttack;
            this.onCombatActionSelected = onCombatActionSelected;
            this.onItem = onItem;
            this.onRun = onRun;
            this.onSurrender = onSurrender;
            this.onSkipTurn = onSkipTurn;
            this.onExit = onExit;
        }

        public void OnAttackPressed()
        {
            if (!CanHandlePlayerTurnInput())
                return;
            onAttack?.Invoke();
        }

        public void OnCombatActionSelected(CombatActionId actionId)
        {
            if (!CanHandlePlayerTurnInput())
                return;
            onCombatActionSelected?.Invoke(actionId);
        }

        public void OnItemPressed()
        {
            if (!CanHandlePlayerTurnInput())
                return;
            onItem?.Invoke();
        }

        public void OnRunPressed()
        {
            if (!CanHandlePlayerTurnInput())
                return;
            onRun?.Invoke();
        }

        public void OnSurrenderPressed()
        {
            if (!CanHandlePlayerTurnInput())
                return;
            onSurrender?.Invoke();
        }

        public void OnSkipTurnPressed()
        {
            if (!CanHandlePlayerTurnInput())
                return;
            onSkipTurn?.Invoke();
        }

        public void OnExitPressed()
        {
            if (!isBattleStarted())
                return;
            onExit?.Invoke();
        }

        private bool CanHandlePlayerTurnInput()
        {
            return isBattleStarted() && isPlayerTurn();
        }
    }
}
