using UnityEngine;
using UDA2.Core;
using UDA2.SceneFlow;

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
        // Open the save/load menu (SaveLoadMenu) in save mode
        if (saveLoadMenuInstance == null && saveLoadMenuPrefab != null)
        {
            saveLoadMenuInstance = Instantiate(saveLoadMenuPrefab, transform.parent);
            // Subscribe to close event if possible
            var closeHandler = saveLoadMenuInstance.GetComponent<IMenuCloseHandler>();
            if (closeHandler != null)
                closeHandler.OnMenuClosed += OnSubMenuClosed;
        }
        if (saveLoadMenuInstance != null)
        {
            saveLoadMenuInstance.SetActive(true);
            var menu = saveLoadMenuInstance.GetComponent<ISaveLoadMenuMode>();
            if (menu != null)
                menu.ShowSaveMode();
            // Deactivate this menu while sub-menu is open
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("SaveLoadMenu prefab is not assigned or failed to instantiate.");
        }
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
        if (GameContext.Current == null)
            Debug.LogError("GameContext.Current is null! Сохранение не выполнено.");
        if (SceneFlowManager.Instance == null)
            Debug.LogError("SceneFlowManager.Instance is null! Переход в главное меню невозможен.");

        SaveManager.Save(GameContext.Current, AUTO_SAVE_SLOT);
        // Загрузка главного меню через SceneFlowManager
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.LoadScene("MainMenuScene");
    }

    public void ExitGame()
    {
        SaveManager.Save(GameContext.Current, AUTO_SAVE_SLOT);
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
