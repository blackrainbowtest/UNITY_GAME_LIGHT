using UnityEngine;
using UnityEngine.SceneManagement;
using UDA2.SceneFlow;
using UDA2.SaveSystem;

public class GameMenuController : MonoBehaviour
{
    private const int AUTO_SAVE_SLOT = 0;

    public void Resume()
    {
        gameObject.SetActive(false);
    }

    [SerializeField] private GameObject saveLoadMenuPrefab;
    private GameObject saveLoadMenuInstance;

    [SerializeField] private GameObject settingsMenuPrefab;
    private GameObject settingsMenuInstance;

    public void SaveGame()
    {
        UDA2.UI.SaveLoad.SaveLoadModalController.Show(UDA2.UI.SaveLoad.SaveLoadMode.Save);
        gameObject.SetActive(false);
    }

    public void OpenSettings()
    {
        // Open the settings menu
        if (settingsMenuInstance == null && settingsMenuPrefab != null)
        {
            settingsMenuInstance = Instantiate(settingsMenuPrefab, transform.parent);
        }
        if (settingsMenuInstance != null)
        {
            // Всегда переподписываемся на событие
            var closeHandler = settingsMenuInstance.GetComponent<IMenuCloseHandler>();
            if (closeHandler != null)
            {
                closeHandler.OnMenuClosed -= OnSubMenuClosed;
                closeHandler.OnMenuClosed += OnSubMenuClosed;
            }
            settingsMenuInstance.SetActive(true);
            // Deactivate this menu while sub-menu is open
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Settings menu prefab is not assigned or failed to instantiate.");
        }
    }
    // Called when a sub-menu (save/load or settings) is closed
    private void OnSubMenuClosed()
    {
        // Отписываемся от события, чтобы избежать повторных вызовов
        if (settingsMenuInstance != null)
        {
            var closeHandler = settingsMenuInstance.GetComponent<IMenuCloseHandler>();
            if (closeHandler != null)
                closeHandler.OnMenuClosed -= OnSubMenuClosed;
            Destroy(settingsMenuInstance);
            settingsMenuInstance = null;
        }
        gameObject.SetActive(true);
    }

    public void GoToMainMenu()
    {
        if (GameState.Instance.CurrentSave == null)
            Debug.LogError("GameState.Instance.CurrentSave is null! Сохранение не выполнено.");
        if (SceneFlowManager.Instance == null)
            Debug.LogError("SceneFlowManager.Instance is null! Переход в главное меню невозможен.");

        var sceneName = SceneManager.GetActiveScene().name;
        if (SceneCategoryResolver.IsSaveAllowed(sceneName))
        {
            SaveSlotsManager.SaveToSlot(AUTO_SAVE_SLOT, GameState.Instance.CurrentSave);
        }
        else
        {
            Debug.Log($"[GameMenu] Autosave skipped in scene '{sceneName}'");
        }

        // Загрузка главного меню через SceneFlowManager
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.LoadScene("MainMenuScene");
    }

    public void ExitGame()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (SceneCategoryResolver.IsSaveAllowed(sceneName))
        {
            SaveSlotsManager.SaveToSlot(AUTO_SAVE_SLOT, GameState.Instance.CurrentSave);
        }
        else
        {
            Debug.Log($"[GameMenu] Autosave skipped in scene '{sceneName}'");
        }
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
