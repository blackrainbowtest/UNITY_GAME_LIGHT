using UnityEngine;
using UnityEngine.EventSystems;

namespace UDA2.UI.Game
{
    /// <summary>
    /// Attached to the tooltip backdrop at runtime.  Closes the tooltip on the NEXT pointer-down
    /// after the tooltip appeared — not on the pointer-up that fired from the long-press gesture.
    ///
    /// Why IPointerDownHandler instead of Button.onClick:
    ///   When the long press completes the EventSystem still has the item-slot recorded as the
    ///   press target.  The subsequent pointer-up therefore goes to the item slot, not the
    ///   backdrop, so Button.onClick on the backdrop would never fire from that release.
    ///   However, using IPointerDownHandler lets us close the tooltip on a brand-new deliberate
    ///   tap — matching exactly what BattleTooltipBackdropCloseCatcher does for ability tooltips.
    /// </summary>
    public sealed class ItemTooltipBackdropCloseCatcher : MonoBehaviour, IPointerDownHandler
    {
        private ItemTooltipModalController _modal;

        public void Bind(ItemTooltipModalController modal)
        {
            _modal = modal;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_modal != null)
                _modal.OnBackdropPointerDown(eventData);
        }
    }
}
