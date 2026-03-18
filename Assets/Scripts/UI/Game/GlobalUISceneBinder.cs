using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UDA2.UI.Game
{
    [DisallowMultipleComponent]
    public sealed class GlobalUISceneBinder : MonoBehaviour
    {
        [Header("Global Visibility")]
        [SerializeField] private CanvasGroup globalVisibilityGroup;

        private Canvas[] rootCanvases;
        private GraphicRaycaster[] rootRaycasters;

        [Header("Visibility Targets")]
        [SerializeField] private GameObject timeRoot;
        [SerializeField] private GameObject timeOverlayRoot;
        [SerializeField] private GameObject profileRoot;
        [SerializeField] private GameObject menuButtonRoot;
        [SerializeField] private GameObject homeButtonRoot;
        [SerializeField] private GameObject backButtonRoot;
        [SerializeField] private GameObject questListRoot;

        [Header("Inspect Eye")]
        [SerializeField] private MonoBehaviour locationInspectController;

        [Header("Defaults (if UISceneConfig not found)")]
        [SerializeField] private bool defaultShowTime = false;
        [SerializeField] private bool defaultShowProfile = false;
        [SerializeField] private bool defaultShowMenuButton = false;
        [SerializeField] private bool defaultShowHomeButton = false;
        [SerializeField] private bool defaultShowBackButton = false;
        [SerializeField] private bool defaultShowQuestList = false;
        [SerializeField] private bool defaultShowInspectEye = false;

        private void Awake()
        {
            if (globalVisibilityGroup == null)
                globalVisibilityGroup = GetComponent<CanvasGroup>();

            if (globalVisibilityGroup == null)
                globalVisibilityGroup = gameObject.AddComponent<CanvasGroup>();

            CacheRootUiComponents();

            ResolveLocationInspectController();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ApplyForScene(SceneManager.GetActiveScene());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyForScene(scene);
        }

        private void ApplyForScene(Scene scene)
        {
            ResolveLocationInspectController();

            var config = FindSceneConfig(scene);

            bool uiVisible = config != null;
            ApplyGlobalVisibility(uiVisible);

            bool showTime = config != null ? config.ShowTime : defaultShowTime;
            bool showProfile = config != null ? config.ShowProfile : defaultShowProfile;
            bool showMenuButton = config != null ? config.ShowMenuButton : defaultShowMenuButton;
            bool showHomeButton = config != null ? config.ShowHomeButton : defaultShowHomeButton;
            bool showBack = config != null ? config.ShowBackButton : defaultShowBackButton;
            bool showQuest = config != null ? config.ShowQuestList : defaultShowQuestList;
            bool showInspect = config != null ? config.ShowInspectEye : defaultShowInspectEye;

            SetVisible(timeRoot, showTime);
            SetVisible(timeOverlayRoot, showTime);
            SetVisible(profileRoot, showProfile);
            SetVisible(menuButtonRoot, showMenuButton);
            SetVisible(homeButtonRoot, showHomeButton);
            SetVisible(backButtonRoot, showBack);
            SetVisible(questListRoot, showQuest);

            if (locationInspectController == null)
                return;

            var hotspotObjects = config != null ? config.ResolveHotspotObjects() : System.Array.Empty<GameObject>();
            var hotspotsRoot = config != null ? config.HotspotsRoot : null;
            bool hasHotspots = (hotspotObjects != null && hotspotObjects.Length > 0) || hotspotsRoot != null;

            InvokeIfExists(locationInspectController, "SetHotspotBinding", hotspotObjects, hotspotsRoot);
            InvokeIfExists(locationInspectController, "SetToggleVisible", showInspect && hasHotspots);
            if (!showInspect || !hasHotspots)
                InvokeIfExists(locationInspectController, "SetEnabled", false);
        }

        private void ResolveLocationInspectController()
        {
            if (HasRequiredInspectMethods(locationInspectController))
                return;

            locationInspectController = FindBestInspectController();
        }

        private MonoBehaviour FindBestInspectController()
        {
            var all = GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            if (all == null || all.Length == 0)
                return null;

            for (int i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null)
                    continue;

                if (HasRequiredInspectMethods(candidate))
                    return candidate;
            }

            return null;
        }

        private static bool HasRequiredInspectMethods(MonoBehaviour target)
        {
            if (target == null)
                return false;

            var type = target.GetType();
            return HasMethod(type, "SetHotspotBinding", 2)
                && HasMethod(type, "SetToggleVisible", 1)
                && HasMethod(type, "SetEnabled", 1);
        }

        private static bool HasMethod(System.Type type, string methodName, int parameterCount)
        {
            var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (!string.Equals(method.Name, methodName, System.StringComparison.Ordinal))
                    continue;

                if (method.GetParameters().Length == parameterCount)
                    return true;
            }

            return false;
        }

        private static UISceneConfig FindSceneConfig(Scene scene)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_2022_2_OR_NEWER
            var all = Object.FindObjectsByType<UISceneConfig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            var all = Object.FindObjectsOfType<UISceneConfig>(true);
#pragma warning restore CS0618
#endif
            if (all == null || all.Length == 0)
                return null;

            for (int i = 0; i < all.Length; i++)
            {
                var cfg = all[i];
                if (cfg == null)
                    continue;

                if (cfg.gameObject.scene == scene)
                    return cfg;
            }

            return null;
        }

        private static void SetVisible(GameObject target, bool visible)
        {
            if (target == null)
                return;

            target.SetActive(visible);
        }

        private void ApplyGlobalVisibility(bool visible)
        {
            if (globalVisibilityGroup != null)
            {
                globalVisibilityGroup.alpha = visible ? 1f : 0f;
                globalVisibilityGroup.interactable = visible;
                globalVisibilityGroup.blocksRaycasts = visible;
            }

            if (rootCanvases == null || rootCanvases.Length == 0)
                CacheRootUiComponents();

            if (rootCanvases != null)
            {
                for (int i = 0; i < rootCanvases.Length; i++)
                {
                    var c = rootCanvases[i];
                    if (c == null)
                        continue;

                    c.enabled = visible;
                }
            }

            if (rootRaycasters != null)
            {
                for (int i = 0; i < rootRaycasters.Length; i++)
                {
                    var r = rootRaycasters[i];
                    if (r == null)
                        continue;

                    r.enabled = visible;
                }
            }

        }

        private void CacheRootUiComponents()
        {
            var root = transform.root != null ? transform.root : transform;
            rootCanvases = root.GetComponentsInChildren<Canvas>(includeInactive: true);
            rootRaycasters = root.GetComponentsInChildren<GraphicRaycaster>(includeInactive: true);
        }

        private void InvokeIfExists(MonoBehaviour target, string methodName, params object[] args)
        {
            if (target == null || string.IsNullOrEmpty(methodName))
                return;

            var type = target.GetType();
            var methods = type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (!string.Equals(method.Name, methodName, System.StringComparison.Ordinal))
                    continue;

                var parameters = method.GetParameters();
                if ((args == null ? 0 : args.Length) != parameters.Length)
                    continue;

                try
                {
                    method.Invoke(target, args);
                }
                catch (System.Exception)
                {
                }
                return;
            }
        }
    }
}
