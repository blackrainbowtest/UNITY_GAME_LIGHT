using UnityEngine;
using UDA2.Platform;

namespace UDA2.UI
{
    public class MainMenuController : MonoBehaviour
    {
        // Устаревшие поля удалены, используем только settingsMenuPrefab

        private void OnEnable()
        {
            // Force update all localized texts in the menu when it becomes active
            LocalizedGlobalComponent.UpdateAllInHierarchy(gameObject);
        }

        private void Awake()
        {
        }

        private void Start()
        {
            // Force update all localized texts at scene start
            LocalizedGlobalComponent.UpdateAllInHierarchy(gameObject);
        }

        public void OnNewGamePressed()
        {
            // New Game must always start from a fresh save (full HP/SP/etc), regardless of what GameBootstrapper loaded.
            SaveSlotsManager.DeleteAutosave();
            global::GameState.Instance.CurrentSave = SaveData.CreateDefault(Application.version, "IntroScene");
            UDA2.SceneFlow.SceneFlowManager.Instance.LoadScene("IntroScene");
        }

        public void OnLoadGamePressed()
        {
            UDA2.UI.SaveLoad.SaveLoadModalController.Show(UDA2.UI.SaveLoad.SaveLoadMode.Load);
        }

        [SerializeField] private GameObject settingsMenuPrefab;
        private GameObject settingsMenuInstance;

        public void OnSettingsPressed()
        {
            if (settingsMenuInstance == null && settingsMenuPrefab != null)
            {
                settingsMenuInstance = Instantiate(settingsMenuPrefab, transform.parent);
                var closeHandler = settingsMenuInstance.GetComponent<IMenuCloseHandler>();
                if (closeHandler != null)
                    closeHandler.OnMenuClosed += OnSettingsMenuClosed;
            }
            if (settingsMenuInstance != null)
            {
                settingsMenuInstance.SetActive(true);
            }
            else
            {
                Debug.LogWarning("MainMenuController: settingsMenuPrefab не назначен или не удалось создать окно настроек.", this);
            }
        }

        private void OnSettingsMenuClosed()
        {
            var closeHandler = settingsMenuInstance.GetComponent<IMenuCloseHandler>();
            if (closeHandler != null)
                closeHandler.OnMenuClosed -= OnSettingsMenuClosed;
            Destroy(settingsMenuInstance);
            settingsMenuInstance = null;
        }

        public void OnExitPressed()
        {
            AppExit.Quit(removeFromRecents: true);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
