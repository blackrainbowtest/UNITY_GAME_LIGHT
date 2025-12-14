using UnityEngine;
using UnityEngine.SceneManagement;

namespace UDA2.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        public int saveSlot = 1;
        public string mainSceneName = "MainMenuScene";

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // Загрузка настроек
            SettingsContext.Current = SettingsManager.Load();

            // Загрузка сейва или создание нового
            var loaded = SaveManager.Load(saveSlot);
            GameContext.Current = loaded ?? new GameState();

            // Переход в первую игровую сцену
            SceneManager.LoadScene(mainSceneName);
        }
    }

    // Контекст для хранения текущих экземпляров
    public static class GameContext
    {
        public static GameState Current;
    }

    public static class SettingsContext
    {
        public static SettingsState Current;
    }
}
