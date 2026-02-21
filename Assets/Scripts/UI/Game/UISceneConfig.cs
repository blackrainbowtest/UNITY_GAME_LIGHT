using UnityEngine;

namespace UDA2.UI.Game
{
    [DisallowMultipleComponent]
    public sealed class UISceneConfig : MonoBehaviour
    {
        [Header("UI Visibility")]
        [SerializeField] private bool showTime = true;
        [SerializeField] private bool showProfile = true;
        [SerializeField] private bool showMenuButton = true;
        [SerializeField] private bool showBackButton = true;
        [SerializeField] private bool showQuestList = true;
        [SerializeField] private bool showInspectEye = true;

        [Header("Location Inspect")]
        [Tooltip("Optional explicit scene objects containing hotspot components.")]
        [SerializeField] private GameObject[] hotspotObjects;
        [Tooltip("Optional root for auto-finding hotspots in this scene.")]
        [SerializeField] private Transform hotspotsRoot;

        public bool ShowTime => showTime;
        public bool ShowProfile => showProfile;
        public bool ShowMenuButton => showMenuButton;
        public bool ShowBackButton => showBackButton;
        public bool ShowQuestList => showQuestList;
        public bool ShowInspectEye => showInspectEye;

        public Transform HotspotsRoot => hotspotsRoot;

        public GameObject[] ResolveHotspotObjects()
        {
            if (hotspotObjects != null && hotspotObjects.Length > 0)
                return FilterNulls(hotspotObjects);

            return System.Array.Empty<GameObject>();
        }

        private static GameObject[] FilterNulls(GameObject[] source)
        {
            if (source == null || source.Length == 0)
                return System.Array.Empty<GameObject>();

            int count = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                    count++;
            }

            if (count == source.Length)
                return source;

            if (count == 0)
                return System.Array.Empty<GameObject>();

            var result = new GameObject[count];
            int write = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                    continue;

                result[write++] = source[i];
            }

            return result;
        }
    }
}
