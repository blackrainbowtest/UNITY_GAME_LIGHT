using UnityEngine;
using UnityEngine.UI;
using UDA2.Core;

namespace UDA2.City
{
    public sealed class CityInspectModeController : MonoBehaviour
    {
        [Header("Toggle")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private Image toggleIcon;
        [SerializeField] private Sprite iconOff;
        [SerializeField] private Sprite iconOn;

        [Header("Targets")]
        [Tooltip("If empty, hotspots will be auto-found under buildingsRoot.")]
        [SerializeField] private CityMapBuildingHotspot[] hotspots;
        [SerializeField] private Transform buildingsRoot;

        private bool enabledInspect;

        private void Awake()
        {
            // Restore persisted setting
            if (SettingsContext.Current == null)
                SettingsContext.Current = SettingsManager.Load();

            enabledInspect = SettingsContext.GetCityInspectModeEnabled();

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

        private void RefreshHotspotsCacheIfNeeded()
        {
            if (hotspots != null && hotspots.Length > 0)
                return;

            if (buildingsRoot == null)
                buildingsRoot = transform;

            hotspots = buildingsRoot.GetComponentsInChildren<CityMapBuildingHotspot>(includeInactive: true);
        }

        public void SetEnabled(bool value)
        {
            enabledInspect = value;
            Persist();
            Apply();
        }

        public void Toggle()
        {
            enabledInspect = !enabledInspect;
            Persist();
            Apply();
        }

        private void Persist()
        {
            if (SettingsContext.Current == null)
                SettingsContext.Current = new SettingsState();

            SettingsContext.SetCityInspectModeEnabled(enabledInspect);
            SettingsManager.Save(SettingsContext.Current);
        }

        private void Apply()
        {
            if (toggleIcon != null)
                toggleIcon.sprite = enabledInspect ? iconOn : iconOff;

            if (hotspots == null)
                return;

            for (int i = 0; i < hotspots.Length; i++)
            {
                var h = hotspots[i];
                if (h == null) continue;
                h.SetHighlight(enabledInspect);
            }
        }
    }
}
