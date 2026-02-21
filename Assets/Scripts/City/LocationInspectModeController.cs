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
        [SerializeField] private CityMapBuildingHotspot[] cityHotspots;
        [SerializeField] private Transform hotspotsRoot;

        private bool _enabledInspect;
        private bool _hotspotsOverriddenExternally;

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

        public void SetToggleVisible(bool visible)
        {
            if (toggleButton != null)
                toggleButton.gameObject.SetActive(visible);
        }

        public void SetHotspots(LocationPrefabHotspot[] sceneHotspots)
        {
            hotspots = sceneHotspots ?? System.Array.Empty<LocationPrefabHotspot>();
            cityHotspots = System.Array.Empty<CityMapBuildingHotspot>();
            _hotspotsOverriddenExternally = true;
            Apply();
        }

        public void SetHotspotBinding(GameObject[] sceneHotspotObjects, Transform sceneHotspotsRoot)
        {
            _hotspotsOverriddenExternally = true;

            if (sceneHotspotObjects != null && sceneHotspotObjects.Length > 0)
            {
                hotspots = ExtractHotspotsFromObjects(sceneHotspotObjects);
                cityHotspots = ExtractCityHotspotsFromObjects(sceneHotspotObjects);
            }
            else if (sceneHotspotsRoot != null)
            {
                hotspots = sceneHotspotsRoot.GetComponentsInChildren<LocationPrefabHotspot>(includeInactive: true);
                cityHotspots = sceneHotspotsRoot.GetComponentsInChildren<CityMapBuildingHotspot>(includeInactive: true);
            }
            else
            {
                hotspots = System.Array.Empty<LocationPrefabHotspot>();
                cityHotspots = System.Array.Empty<CityMapBuildingHotspot>();
            }
            Apply();
        }

        private void RefreshHotspotsCacheIfNeeded()
        {
            if (_hotspotsOverriddenExternally)
                return;

            if (hotspots != null && hotspots.Length > 0)
                return;

            if (hotspotsRoot == null)
                hotspotsRoot = transform;

            hotspots = hotspotsRoot.GetComponentsInChildren<LocationPrefabHotspot>(includeInactive: true);
            cityHotspots = hotspotsRoot.GetComponentsInChildren<CityMapBuildingHotspot>(includeInactive: true);
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

            if (cityHotspots != null)
            {
                for (int i = 0; i < cityHotspots.Length; i++)
                {
                    var cityHotspot = cityHotspots[i];
                    if (cityHotspot == null)
                        continue;

                    cityHotspot.SetHighlight(_enabledInspect);
                }
            }
        }

        private static LocationPrefabHotspot[] ExtractHotspotsFromObjects(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0)
                return System.Array.Empty<LocationPrefabHotspot>();

            int count = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null)
                    continue;

                if (obj.GetComponent<LocationPrefabHotspot>() != null)
                    count++;
            }

            if (count == 0)
                return System.Array.Empty<LocationPrefabHotspot>();

            var result = new LocationPrefabHotspot[count];
            int write = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null)
                    continue;

                var hotspot = obj.GetComponent<LocationPrefabHotspot>();
                if (hotspot == null)
                    continue;

                result[write++] = hotspot;
            }

            return result;
        }

        private static CityMapBuildingHotspot[] ExtractCityHotspotsFromObjects(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0)
                return System.Array.Empty<CityMapBuildingHotspot>();

            int count = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null)
                    continue;

                if (obj.GetComponent<CityMapBuildingHotspot>() != null)
                    count++;
            }

            if (count == 0)
                return System.Array.Empty<CityMapBuildingHotspot>();

            var result = new CityMapBuildingHotspot[count];
            int write = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null)
                    continue;

                var hotspot = obj.GetComponent<CityMapBuildingHotspot>();
                if (hotspot == null)
                    continue;

                result[write++] = hotspot;
            }

            return result;
        }

    }
}