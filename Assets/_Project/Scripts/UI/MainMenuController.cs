using UnityEngine;

namespace UDA2.UI
{
    public class MainMenuController : MonoBehaviour
    {
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
            // Логика будет добавлена позже
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
