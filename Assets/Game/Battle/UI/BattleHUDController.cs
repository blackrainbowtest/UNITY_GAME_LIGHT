using UnityEngine;
using UnityEngine.UI;
using Game.Battle.UI;

public class BattleHUDController : MonoBehaviour, IBattleHUDView
{
    [Header("Action Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button exitButton;

    [Header("HP Bars")]
    [SerializeField] private StatBarView playerHpBar;
    // [SerializeField] private StatBarView playerMpBar;
    // [SerializeField] private StatBarView playerSpBar;
    // [SerializeField] private StatBarView playerLpBar;
    [SerializeField] private StatBarView enemyHpBar;
    // [SerializeField] private StatBarView enemyMpBar;
    // [SerializeField] private StatBarView enemySpBar;
    // [SerializeField] private StatBarView enemyLpBar;

    private IBattleUIActions actions;

    public void SetActions(IBattleUIActions actions)
    {
        this.actions = actions;

        attackButton.onClick.AddListener(() => actions.OnAttackPressed());
        itemButton.onClick.AddListener(() => actions.OnItemPressed());
        exitButton.onClick.AddListener(() => actions.OnExitPressed());
    }

    public void UpdateState(BattleHUDState state)
    {
        Debug.Log("[HUD] UpdateState called");
        if (state == null) return;

        Debug.Log($"[HUD] Player HP: {state.PlayerHp}/{state.PlayerHpMax}, Enemy HP: {state.EnemyHp}/{state.EnemyHpMax}");

        if (playerHpBar != null && state.PlayerHpMax > 0)
            playerHpBar.SetNormalized((float)state.PlayerHp / state.PlayerHpMax);
        // if (playerMpBar != null && state.PlayerMpMax > 0)
        //     playerMpBar.SetNormalized((float)state.PlayerMp / state.PlayerMpMax);
        // if (playerSpBar != null && state.PlayerSpMax > 0)
        //     playerSpBar.SetNormalized((float)state.PlayerSp / state.PlayerSpMax);
        // if (playerLpBar != null && state.PlayerLpMax > 0)
        //     playerLpBar.SetNormalized((float)state.PlayerLp / state.PlayerLpMax);

        if (enemyHpBar != null && state.EnemyHpMax > 0)
            enemyHpBar.SetNormalized((float)state.EnemyHp / state.EnemyHpMax);
        // if (enemyMpBar != null && state.EnemyMpMax > 0)
        //     enemyMpBar.SetNormalized((float)state.EnemyMp / state.EnemyMpMax);
        // if (enemySpBar != null && state.EnemySpMax > 0)
        //     enemySpBar.SetNormalized((float)state.EnemySp / state.EnemySpMax);
        // if (enemyLpBar != null && state.EnemyLpMax > 0)
        //     enemyLpBar.SetNormalized((float)state.EnemyLp / state.EnemyLpMax);
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
