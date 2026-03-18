using TMPro;
using UnityEngine;
using UDA2.Core;

namespace UDA2.City
{
    [DisallowMultipleComponent]
    public sealed class LocationWindowContentMeta : MonoBehaviour
    {
        [Header("Title")]
        [Tooltip("Fallback plain text title.")]
        [SerializeField] private string titleText;

        [Tooltip("Optional localization key for title. If set, this key is used first.")]
        [SerializeField] private string titleLocalizationKey;

        [Tooltip("Optional direct text target inside content (if content also shows title internally).")]
        [SerializeField] private TMP_Text contentTitleText;

        [Header("Frame Visual")]
        [Tooltip("Optional sprite for LocationWindowFrame background.")]
        [SerializeField] private Sprite frameBackgroundSprite;

        public string TitleLocalizationKey => titleLocalizationKey;
        public string TitleFallbackText => titleText;
        public Sprite FrameBackgroundSprite => frameBackgroundSprite;

        public string ResolveTitle()
        {
            if (!string.IsNullOrWhiteSpace(titleLocalizationKey))
            {
                var localized = LocalizationManager.Get(titleLocalizationKey);
                if (!string.IsNullOrWhiteSpace(localized))
                    return localized;
            }

            return titleText;
        }

        public void ApplyTitleToContentIfAssigned()
        {
            if (contentTitleText == null)
                return;

            if (!string.IsNullOrWhiteSpace(titleLocalizationKey))
            {
                var localized = contentTitleText.GetComponent<LocalizedGlobalComponent>();
                if (localized != null)
                {
                    localized.Key = titleLocalizationKey;
                    localized.ClearArgs();
                    return;
                }
            }

            contentTitleText.text = ResolveTitle();
        }
    }
}
