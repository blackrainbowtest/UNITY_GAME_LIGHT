using UnityEngine;

namespace UDA2.UI
{
    public class MainMenuController : MonoBehaviour
    {
        public SettingsController settingsController;
        public GameObject settingsPanel;
        public GameObject settingsWindow;

        private void OnEnable()
        {
            // Force update all localized texts in the menu when it becomes active
            LocalizedTextSetter.UpdateAllInHierarchy(gameObject);
        }

        private void Awake()
        {
            UDA2.Core.SettingsContext.Current = UDA2.Core.SettingsManager.Load();
        }

        private void Start()
        {
            // Force update all localized texts at scene start
            LocalizedTextSetter.UpdateAllInHierarchy(gameObject);
        }

        public void OnNewGamePressed()
        {
            // Логика будет добавлена позже
        }

        public void OnLoadGamePressed()
        {
            // Логика будет добавлена позже
        }

        public void OnSettingsPressed()
        {
            if (settingsController != null)
            {
                settingsController.Open();
                return;
            }

            if (settingsWindow != null)
                settingsWindow.SetActive(true);

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                return;
            }

            Debug.LogWarning("MainMenuController: settingsController/settingsPanel/settingsWindow не назначены.", this);
        }

        public void OnExitPressed()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
