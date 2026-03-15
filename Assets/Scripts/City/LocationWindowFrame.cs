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

        [Tooltip("Optional background image in frame. If assigned, can be overridden by content meta.")]
        [SerializeField] private Image backgroundImage;

        [Tooltip("If true, frame background alpha is forced visible when sprite is assigned and restored when sprite is cleared.")]
        [SerializeField] private bool autoAdjustBackgroundAlpha = true;

        [SerializeField, Range(0f, 1f)] private float backgroundVisibleAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float backgroundHiddenAlpha = 0f;

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
                if (localized == null)
                    localized = headerTitleText.GetComponentInParent<LocalizedGlobalComponent>();
                if (localized == null)
                    localized = headerTitleText.GetComponentInChildren<LocalizedGlobalComponent>(includeInactive: true);

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

                Debug.LogWarning($"[LocationWindowFrame] Header title localization key '{localizationKey}' was provided, but no LocalizedGlobalComponent was found near '{headerTitleText.name}'. Applied fallback text.", this);
            }

            SetHeaderTitle(fallbackTitle);
        }

        public void SetBackgroundSprite(Sprite sprite)
        {
            if (backgroundImage == null)
                return;

            backgroundImage.sprite = sprite;

            if (autoAdjustBackgroundAlpha)
            {
                var c = backgroundImage.color;
                c.a = sprite != null
                    ? Mathf.Clamp01(backgroundVisibleAlpha)
                    : Mathf.Clamp01(backgroundHiddenAlpha);
                backgroundImage.color = c;
            }

            backgroundImage.enabled = sprite != null || !Mathf.Approximately(backgroundHiddenAlpha, 0f);
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
