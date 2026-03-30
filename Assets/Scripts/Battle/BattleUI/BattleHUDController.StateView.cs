using System.Collections.Generic;
using Game.Battle.UI;
using Game.Battle.Statuses;

public partial class BattleHUDController
{
    public void UpdateState(BattleHUDState state)
    {
        UpdateState(state, showDeltas: true);
    }

    public void UpdateStatuses(IReadOnlyList<StatusInstance> player, IReadOnlyList<StatusInstance> enemy)
    {
        if (statusPanel == null)
            return;

        statusPanel.Render(player, enemy);
    }

    public BattleStatusCatalog GetStatusCatalog()
    {
        return statusPanel != null ? statusPanel.StatusCatalog : null;
    }

    public void UpdateState(BattleHUDState state, bool showDeltas)
    {
        if (state == null) return;

        var hasLast = lastState != null;
        var canShowDeltas = showDeltas && hasLast;

        UpdateBar(playerHpBar, state.PlayerHp, state.PlayerHpMax, hasLast ? state.PlayerHp - lastState.PlayerHp : 0, canShowDeltas);
        UpdateBar(playerMpBar, state.PlayerMp, state.PlayerMpMax, hasLast ? state.PlayerMp - lastState.PlayerMp : 0, canShowDeltas);
        UpdateBar(playerSpBar, state.PlayerSp, state.PlayerSpMax, hasLast ? state.PlayerSp - lastState.PlayerSp : 0, canShowDeltas);
        UpdateBar(playerLpBar, state.PlayerLp, state.PlayerLpMax, hasLast ? state.PlayerLp - lastState.PlayerLp : 0, canShowDeltas);

        UpdateBar(enemyHpBar, state.EnemyHp, state.EnemyHpMax, hasLast ? state.EnemyHp - lastState.EnemyHp : 0, canShowDeltas);
        UpdateBar(enemyMpBar, state.EnemyMp, state.EnemyMpMax, hasLast ? state.EnemyMp - lastState.EnemyMp : 0, canShowDeltas);
        UpdateBar(enemySpBar, state.EnemySp, state.EnemySpMax, hasLast ? state.EnemySp - lastState.EnemySp : 0, canShowDeltas);
        UpdateBar(enemyLpBar, state.EnemyLp, state.EnemyLpMax, hasLast ? state.EnemyLp - lastState.EnemyLp : 0, canShowDeltas);

        // Store a copy so later deltas work even if caller reuses the same object.
        lastState = Clone(state);
    }

    private static void UpdateBar(StatBarView bar, int current, int max, int delta, bool showDelta)
    {
        if (bar == null)
            return;

        // Important: if max == 0, we must still update the UI.
        // Otherwise the bar keeps previous fill/value and looks like a random/default number.
        if (max <= 0)
        {
            bar.SetNormalized(0f);
            bar.SetValue(0, 0);
            return;
        }

        bar.SetNormalized((float)current / max);
        bar.SetValue(current, max);

        if (showDelta)
            bar.ShowDelta(delta);
    }

    private static BattleHUDState Clone(BattleHUDState s)
    {
        return new BattleHUDState
        {
            PlayerHp = s.PlayerHp,
            PlayerHpMax = s.PlayerHpMax,
            PlayerMp = s.PlayerMp,
            PlayerMpMax = s.PlayerMpMax,
            PlayerSp = s.PlayerSp,
            PlayerSpMax = s.PlayerSpMax,
            PlayerLp = s.PlayerLp,
            PlayerLpMax = s.PlayerLpMax,

            EnemyHp = s.EnemyHp,
            EnemyHpMax = s.EnemyHpMax,
            EnemyMp = s.EnemyMp,
            EnemyMpMax = s.EnemyMpMax,
            EnemySp = s.EnemySp,
            EnemySpMax = s.EnemySpMax,
            EnemyLp = s.EnemyLp,
            EnemyLpMax = s.EnemyLpMax
        };
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
