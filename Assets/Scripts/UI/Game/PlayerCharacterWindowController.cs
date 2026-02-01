using System;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    public sealed class PlayerCharacterWindowController : MonoBehaviour, global::IMenuCloseHandler
    {
        [Serializable]
        public sealed class TabEntry
        {
            public PlayerCharacterTabId tabId;
            public Button tabButton;
            public GameObject tabViewPrefab;
        }

        [Header("Wiring")]
        [SerializeField] private Button closeButton;
        [Tooltip("Optional: if assigned, clicking backdrop will close the window.")]
        [SerializeField] private Button backdropButton;
        [SerializeField] private Transform contentRoot;

        [Header("Tabs")]
        [SerializeField] private PlayerCharacterTabId defaultTab = PlayerCharacterTabId.Profile;
        [SerializeField] private TabEntry[] tabs;

        [Header("Behavior")]
        [Tooltip("If true, this window destroys itself on close. If false, it just deactivates.")]
        [SerializeField] private bool destroyOnClose = true;

        [Tooltip("If true, stretches this window's root RectTransform to match its parent on enable.")]
        [SerializeField] private bool stretchRootToParent = true;

        public event Action OnMenuClosed;

        private PlayerCharacterTabId? _activeTab;
        private GameObject _activeTabView;
        private GameObject _ownerRoot;

        public void SetOwnerRoot(GameObject ownerRoot)
        {
            _ownerRoot = ownerRoot;
        }

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (backdropButton != null)
                backdropButton.onClick.AddListener(Close);

            if (tabs != null)
            {
                for (int i = 0; i < tabs.Length; i++)
                {
                    var entry = tabs[i];
                    if (entry == null || entry.tabButton == null)
                        continue;

                    var id = entry.tabId;
                    entry.tabButton.onClick.AddListener(() => SelectTab(id));
                }
            }
        }

        private void OnEnable()
        {
            if (stretchRootToParent)
                StretchToParent(transform as RectTransform);

            // Make sure there is always a visible tab when the window is opened.
            SelectTab(_activeTab ?? defaultTab);
        }

        public void Close()
        {
            OnMenuClosed?.Invoke();

            if (_activeTabView != null)
            {
                Destroy(_activeTabView);
                _activeTabView = null;
            }

            var target = _ownerRoot != null ? _ownerRoot : gameObject;
            if (destroyOnClose)
                Destroy(target);
            else
                target.SetActive(false);
        }

        public void SelectTab(PlayerCharacterTabId tabId)
        {
            if (contentRoot == null)
            {
                Debug.LogWarning("[PlayerCharacterWindow] ContentRoot is not assigned.");
                return;
            }

            var entry = FindEntry(tabId);
            if (entry == null)
            {
                Debug.LogWarning($"[PlayerCharacterWindow] No tab entry for {tabId}");
                return;
            }

            _activeTab = tabId;

            if (_activeTabView != null)
            {
                Destroy(_activeTabView);
                _activeTabView = null;
            }

            if (entry.tabViewPrefab != null)
            {
                _activeTabView = Instantiate(entry.tabViewPrefab, contentRoot);
                StretchToParent(_activeTabView.transform as RectTransform);
            }
            else
            {
                Debug.LogWarning($"[PlayerCharacterWindow] Tab '{tabId}' has no view prefab.");
            }

            UpdateTabButtonStates();
        }

        private void UpdateTabButtonStates()
        {
            if (tabs == null)
                return;

            for (int i = 0; i < tabs.Length; i++)
            {
                var entry = tabs[i];
                if (entry == null || entry.tabButton == null)
                    continue;

                // Simple visual feedback: disable the selected tab button.
                entry.tabButton.interactable = !_activeTab.HasValue || entry.tabId != _activeTab.Value;
            }
        }

        private TabEntry FindEntry(PlayerCharacterTabId tabId)
        {
            if (tabs == null)
                return null;

            for (int i = 0; i < tabs.Length; i++)
            {
                var entry = tabs[i];
                if (entry != null && entry.tabId == tabId)
                    return entry;
            }

            return null;
        }

        private static void StretchToParent(RectTransform rt)
        {
            if (rt == null)
                return;

            if (rt.parent is RectTransform)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.localScale = Vector3.one;
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
