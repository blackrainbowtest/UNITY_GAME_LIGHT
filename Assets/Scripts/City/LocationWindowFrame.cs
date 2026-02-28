using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UDA2.Core;

namespace UDA2.City
{
    [DisallowMultipleComponent]
    public sealed class LocationWindowFrame : MonoBehaviour
    {
        [Header("Behavior")]
        [Tooltip("Temporarily hide global scene UI while this frame is open.")]
        [SerializeField] private bool hideGlobalUiWhileOpen = true;

        [Tooltip("Optional content root inside frame. If empty, frame root transform is used.")]
        [SerializeField] private Transform contentRoot;

        [Tooltip("Optional header title text in frame.")]
        [SerializeField] private TMP_Text headerTitleText;

        [Tooltip("Optional close button owned by frame. If assigned, hotspot auto-wires close action.")]
        [SerializeField] private Button closeButton;

        private bool ownsGlobalUiHide;

        public Transform ContentRoot => contentRoot != null ? contentRoot : transform;
        public Button CloseButton => closeButton;

        private void OnEnable()
        {
            if (!hideGlobalUiWhileOpen || ownsGlobalUiHide)
                return;

            LocationGlobalUiVisibility.RequestHide(this);
            ownsGlobalUiHide = true;
        }

        private void OnDisable()
        {
            ReleaseGlobalUiIfOwned();
        }

        private void OnDestroy()
        {
            ReleaseGlobalUiIfOwned();
        }

        public void SetHeaderTitle(string title)
        {
            if (headerTitleText == null)
                return;

            headerTitleText.text = title ?? string.Empty;
        }

        public void SetHeaderTitle(string localizationKey, string fallbackTitle)
        {
            if (headerTitleText == null)
                return;

            if (!string.IsNullOrWhiteSpace(localizationKey))
            {
                var localized = headerTitleText.GetComponent<LocalizedGlobalComponent>();
                if (localized != null)
                {
                    localized.Key = localizationKey;
                    localized.ClearArgs();
                    return;
                }

                var resolved = LocalizationManager.Get(localizationKey);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    headerTitleText.text = resolved;
                    return;
                }
            }

            SetHeaderTitle(fallbackTitle);
        }

        private void ReleaseGlobalUiIfOwned()
        {
            if (!ownsGlobalUiHide)
                return;

            ownsGlobalUiHide = false;
            LocationGlobalUiVisibility.ReleaseHide(this);
        }
    }
}
