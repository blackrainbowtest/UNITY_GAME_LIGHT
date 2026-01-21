using UnityEngine;
using UnityEngine.SceneManagement;
using UDA2.SceneFlow;

namespace UDA2.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        private static GameBootstrapper instance;

        public int saveSlot = 1;
        public string mainSceneName = "MainMenuScene";

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            DontDestroyOnLoad(gameObject);

            // Загрузка настроек
            SettingsContext.Current = SettingsManager.Load();
            SettingsContext.ApplyAll();


            // Загрузка сейва или создание нового
            var loaded = SaveManager.Load(saveSlot);
            GameContext.Current = loaded ?? new GameState();

            // Если нет сейва — создаём новый с актуальной версией
            if (global::GameState.Instance.CurrentSave == null)
            {
                string versionPath = System.IO.Path.Combine(Application.dataPath, "..", "version.txt");
                string version = System.IO.File.Exists(versionPath)
                    ? System.IO.File.ReadAllText(versionPath).Trim()
                    : "0.0.1";
                global::GameState.Instance.CurrentSave = SaveData.CreateDefault(version);
            }

            // Переход в первую игровую сцену
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.LoadScene(mainSceneName);
            else
                SceneManager.LoadScene(mainSceneName);
        }
    }

    // Контекст для хранения текущих экземпляров
    public static class GameContext
    {
        public static GameState Current;
    }

}
