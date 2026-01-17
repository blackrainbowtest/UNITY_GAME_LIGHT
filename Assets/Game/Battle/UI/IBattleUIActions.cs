namespace Game.Battle.UI
{
    /// <summary>
    /// UI -> Battle actions. UI sends intents, battle decides what to do.
    /// </summary>
    public interface IBattleUIActions
    {
        void OnCombatActionSelected(Game.Battle.Combat.Actions.CombatActionId actionId);
        void OnAttackPressed();
        void OnItemPressed();
        void OnExitPressed();
    }
}
