namespace Game.Battle.UI
{
    /// <summary>
    /// Battle -> UI contract. Battle provides data, UI renders it.
    /// </summary>
    public interface IBattleHUDView
    {
        void SetActions(IBattleUIActions actions);
        void UpdateState(BattleHUDState state);
        void Show();
        void Hide();
    }
}
