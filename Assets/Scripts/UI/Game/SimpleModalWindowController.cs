using System;
using UnityEngine;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    [DisallowMultipleComponent]
    public sealed class SimpleModalWindowController : MonoBehaviour, global::IMenuCloseHandler
    {
        [Header("Wiring")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;

        [Header("Behavior")]
        [Tooltip("If true, destroys root object when window closes. Otherwise only deactivates it.")]
        [SerializeField] private bool destroyOnClose = true;

        [Tooltip("If true, stretches this window root to parent on enable.")]
        [SerializeField] private bool stretchRootToParent = true;

        public event Action OnMenuClosed;

        private GameObject ownerRoot;

        public void SetOwnerRoot(GameObject root)
        {
            ownerRoot = root;
        }

        private void Awake()
        {
            if (closeButton == null)
                closeButton = FindButtonByNameHint("close", "x", "cross", "cancel", "back");

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (backdropButton != null)
                backdropButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (backdropButton != null)
                backdropButton.onClick.RemoveListener(Close);
        }

        private void OnEnable()
        {
            if (stretchRootToParent)
                StretchToParent(transform as RectTransform);
        }

        public void Close()
        {
            OnMenuClosed?.Invoke();

            var target = ownerRoot != null ? ownerRoot : gameObject;
            if (destroyOnClose)
                Destroy(target);
            else
                target.SetActive(false);
        }

        private Button FindButtonByNameHint(params string[] hints)
        {
            var all = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (b == null)
                    continue;

                var name = b.name;
                for (int h = 0; h < hints.Length; h++)
                {
                    if (name.IndexOf(hints[h], StringComparison.OrdinalIgnoreCase) >= 0)
                        return b;
                }
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
            }
        }
    }
}
