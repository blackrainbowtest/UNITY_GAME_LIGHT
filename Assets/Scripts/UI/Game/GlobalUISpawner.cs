using UnityEngine;

namespace UDA2.UI.Game
{
    [DisallowMultipleComponent]
    public sealed class GlobalUISpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject globalUiPrefab;

        [Header("Spawn")]
        [Tooltip("Optional explicit parent for spawned UI. If null, this spawner's transform is used.")]
        [SerializeField] private Transform spawnParent;
        [SerializeField] private bool spawnOnAwake = true;

        [Header("Lifetime")]
        [Tooltip("If true, only one UI instance will exist and it will persist across scene loads.")]
        [SerializeField] private bool persistentSingleton = true;

        private static GameObject persistentInstance;
        private GameObject localInstance;

        private void Awake()
        {
            if (!spawnOnAwake)
                return;

            SpawnIfNeeded();
        }

        public GameObject SpawnIfNeeded()
        {
            if (globalUiPrefab == null)
                return null;

            if (persistentSingleton)
            {
                if (persistentInstance != null)
                    return persistentInstance;

                persistentInstance = CreateInstance(asRoot: true);
                if (persistentInstance != null)
                    DontDestroyOnLoad(persistentInstance);

                return persistentInstance;
            }

            if (localInstance != null)
                return localInstance;

            localInstance = CreateInstance(asRoot: false);
            return localInstance;
        }

        private GameObject CreateInstance(bool asRoot)
        {
            GameObject instance;
            if (asRoot)
            {
                instance = Instantiate(globalUiPrefab);
            }
            else
            {
                var parent = spawnParent != null ? spawnParent : transform;
                instance = Instantiate(globalUiPrefab, parent, worldPositionStays: false);
            }

            instance.name = globalUiPrefab.name;
            return instance;
        }
    }
}
