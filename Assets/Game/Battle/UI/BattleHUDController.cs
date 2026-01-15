using UnityEngine;
using UnityEngine.UI;
using Game.Battle.UI;

public class BattleHUDController : MonoBehaviour, IBattleHUDView
{
    [Header("Action Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button exitButton;

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
        // v0.1 — пусто
        // HP-бары подключим позже
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
