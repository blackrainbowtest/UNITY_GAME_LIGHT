using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BattleTooltipBackdropCloseCatcher : MonoBehaviour, IPointerDownHandler
{
    private BattleActionTooltipModalController modal;

    public void Bind(BattleActionTooltipModalController owner)
    {
        modal = owner;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (modal != null)
            modal.Hide();

        // Consume so it doesn't click through.
        if (eventData != null)
            eventData.Use();
    }
}
