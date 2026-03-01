using System;
using UnityEngine;

namespace UDA2.UI.Game
{
    /// <summary>
    /// Orchestrates Profile UI refresh and provides a single place to add profile-related systems logic.
    /// Today: refresh level/exp UI.
    /// Future: handle equipment slot click + add/remove equip flows.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProfileSystemsController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private ProfileTabView profileTabView;
        [SerializeField] private ProfileExperienceView profileExperienceView;
        [SerializeField] private ProfileStatusView profileStatusView;

        [Header("Behavior")]
        [SerializeField] private bool refreshOnEnable = true;

        public event Action<EquipmentSlotId> SlotClicked;

        private void Awake()
        {
            if (profileTabView == null)
                profileTabView = GetComponentInChildren<ProfileTabView>(includeInactive: true);

            if (profileExperienceView == null)
                profileExperienceView = GetComponentInChildren<ProfileExperienceView>(includeInactive: true);

            if (profileStatusView == null)
                profileStatusView = GetComponentInChildren<ProfileStatusView>(includeInactive: true);
        }

        private void OnEnable()
        {
            if (profileTabView != null)
                profileTabView.SlotClicked += OnSlotClicked;

            if (refreshOnEnable)
                Refresh();
        }

        private void OnDisable()
        {
            if (profileTabView != null)
                profileTabView.SlotClicked -= OnSlotClicked;
        }

        public void Refresh()
        {
            // Equipment + character sprite
            profileTabView?.Refresh();

            // Level + exp numbers + exp bar fill
            profileExperienceView?.RefreshFromCurrentSave();

            // HP/MP/SP/LP bars
            profileStatusView?.RefreshFromCurrentSave();
        }

        private void OnSlotClicked(EquipmentSlotId slotId)
        {
            SlotClicked?.Invoke(slotId);
        }
    }
}
