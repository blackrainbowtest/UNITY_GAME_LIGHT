using System.Collections.Generic;
using UnityEngine;

namespace UDA2.UI.Shelter
{
    [DisallowMultipleComponent]
    public sealed class ModalContainerUiVisibilityController : MonoBehaviour
    {
        [Header("Modal Source")]
        [Tooltip("Container where modal prefabs are instantiated. If empty, this object is used.")]
        [SerializeField] private Transform modalContainer;

        [Header("UI To Hide")]
        [Tooltip("Scene UI roots that should be hidden while at least one modal is open.")]
        [SerializeField] private List<GameObject> hideWhileModalOpen = new List<GameObject>();

        [Tooltip("Treat inactive modal children as open too.")]
        [SerializeField] private bool countInactiveModalChildrenAsOpen;

        private readonly Dictionary<GameObject, bool> originalActiveState = new Dictionary<GameObject, bool>();

        private bool lastOpenState;
        private int lastChildCount = -1;
        private bool initialized;

        private void Awake()
        {
            EnsureContainer();
            RefreshState(force: true);
            initialized = true;
        }

        private void OnEnable()
        {
            EnsureContainer();
            RefreshState(force: true);
        }

        private void Update()
        {
            if (!initialized)
                return;

            RefreshState(force: false);
        }

        private void OnDisable()
        {
            RestoreUi();
            lastChildCount = -1;
            lastOpenState = false;
        }

        private void EnsureContainer()
        {
            if (modalContainer == null)
                modalContainer = transform;
        }

        private void RefreshState(bool force)
        {
            EnsureContainer();

            int childCount = modalContainer != null ? modalContainer.childCount : 0;
            bool hasOpenModal = HasOpenModal();

            if (!force && childCount == lastChildCount && hasOpenModal == lastOpenState)
                return;

            lastChildCount = childCount;

            if (hasOpenModal)
                HideUi();
            else
                RestoreUi();

            lastOpenState = hasOpenModal;
        }

        private bool HasOpenModal()
        {
            if (modalContainer == null || modalContainer.childCount == 0)
                return false;

            if (countInactiveModalChildrenAsOpen)
                return true;

            int count = modalContainer.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = modalContainer.GetChild(i);
                if (child != null && child.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }

        private void HideUi()
        {
            int count = hideWhileModalOpen.Count;
            for (int i = 0; i < count; i++)
            {
                GameObject target = hideWhileModalOpen[i];
                if (target == null)
                    continue;

                if (!originalActiveState.ContainsKey(target))
                    originalActiveState[target] = target.activeSelf;

                target.SetActive(false);
            }
        }

        private void RestoreUi()
        {
            int count = hideWhileModalOpen.Count;
            for (int i = 0; i < count; i++)
            {
                GameObject target = hideWhileModalOpen[i];
                if (target == null)
                    continue;

                bool previousState;
                if (originalActiveState.TryGetValue(target, out previousState))
                    target.SetActive(previousState);
                else
                    target.SetActive(true);
            }

            originalActiveState.Clear();
        }
    }
}
