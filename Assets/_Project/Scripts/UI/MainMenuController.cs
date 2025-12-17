using UnityEngine;

namespace UDA2.UI
{
    public class MainMenuController : MonoBehaviour
    {
        public SettingsController settingsController;
        public GameObject settingsPanel;
        public GameObject settingsWindow;
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
