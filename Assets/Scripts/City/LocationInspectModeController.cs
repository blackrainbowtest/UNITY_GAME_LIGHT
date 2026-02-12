using UnityEngine;
using UnityEngine.UI;

namespace UDA2.City
{
    [DisallowMultipleComponent]
    public sealed class LocationInspectModeController : MonoBehaviour
    {
        [Header("Toggle")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private Image toggleIcon;
        [SerializeField] private Sprite iconOff;
        [SerializeField] private Sprite iconOn;
        [SerializeField] private bool enabledOnStart;

        [Header("Targets")]
        [Tooltip("If empty, hotspots will be auto-found under hotspotsRoot.")]
        [SerializeField] private LocationPrefabHotspot[] hotspots;
        [SerializeField] private Transform hotspotsRoot;

        private bool _enabledInspect;

        private void Awake()
        {
            _enabledInspect = enabledOnStart;

            if (toggleButton != null)
                toggleButton.onClick.AddListener(Toggle);

            RefreshHotspotsCacheIfNeeded();
            Apply();
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(Toggle);
        }

        public void SetEnabled(bool value)
        {
            _enabledInspect = value;
            Apply();
        }

        public void Toggle()
        {
            _enabledInspect = !_enabledInspect;
            Apply();
        }

        private void RefreshHotspotsCacheIfNeeded()
        {
            if (hotspots != null && hotspots.Length > 0)
                return;

            if (hotspotsRoot == null)
                hotspotsRoot = transform;

            hotspots = hotspotsRoot.GetComponentsInChildren<LocationPrefabHotspot>(includeInactive: true);
        }

        private void Apply()
        {
            if (toggleIcon != null)
                toggleIcon.sprite = _enabledInspect ? iconOn : iconOff;

            if (hotspots == null)
                return;

            for (int i = 0; i < hotspots.Length; i++)
            {
                var hotspot = hotspots[i];
                if (hotspot == null)
                    continue;

                hotspot.SetInspectMode(_enabledInspect);
            }
        }
    }
}