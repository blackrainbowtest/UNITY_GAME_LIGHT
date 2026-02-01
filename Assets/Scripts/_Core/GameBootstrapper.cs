//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets\Scripts\_Core\GameBootstrapper.cs                                                         */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/23 01:35:09 by UDA                                                                    */
/*   Updated: 2026/01/23 01:35:09 by UDA                                                                    */
/*                                                                                                          */
/* ******************************************************************************************************** */

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

            // Загрузка сейва (SaveData) или создание нового
            var loadedSave = SaveSlotsManager.LoadFromSlot(saveSlot);
            global::GameState.Instance.CurrentSave = SaveDataMigration.Apply(loadedSave);

            // Если нет сейва — создаём новый с актуальной версией
            if (global::GameState.Instance.CurrentSave == null)
            {
                global::GameState.Instance.CurrentSave = SaveData.CreateDefault(Application.version);
            }

            // Переход в первую игровую сцену
            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.LoadScene(mainSceneName);
            else
                SceneManager.LoadScene(mainSceneName);
        }

		private void OnDestroy()
		{
			if (instance == this)
				instance = null;
		}
    }
}
