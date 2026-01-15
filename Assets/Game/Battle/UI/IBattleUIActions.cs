namespace Game.Battle.UI
{
    /// <summary>
    /// UI -> Battle actions. UI sends intents, battle decides what to do.
    /// </summary>
    public interface IBattleUIActions
    {
        void OnAttackPressed();
        void OnItemPressed();
        void OnExitPressed();
    }
}
