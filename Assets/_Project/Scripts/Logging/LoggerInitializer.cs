using UnityEngine;
using UDA2.Logging;

public class LoggerInitializer : MonoBehaviour
    // Example method to simulate settings change and log it
    public void OnSettingsChanged(object settings)
    {
        UDA2.Logging.UDAlog.Info("Settings changed: " + JsonUtility.ToJson(settings), LogChannel.UI);
    }
{
    private int errorCount = 0;
    private int warningCount = 0;

    void Awake()
    {
        UDA2.Logging.Logger.LogInfo($"Game started. Version: {UDA2.Logging.Logger.BuildId}, Device: {SystemInfo.deviceModel}, OS: {SystemInfo.operatingSystem}, Platform: {Application.platform}, Screen: {Screen.width}x{Screen.height}", LogChannel.Default);
        Application.logMessageReceived += OnLogMessageReceived;
    }

    void OnApplicationQuit()
    {
        UDA2.Logging.Logger.LogInfo($"Game exited. Total errors: {errorCount}, total warnings: {warningCount}", LogChannel.Default);
        UDA2.Logging.Logger.Shutdown();
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    private void OnLogMessageReceived(string condition, string stackTrace, UnityEngine.LogType type)
    {
        if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception)
            errorCount++;
        else if (type == UnityEngine.LogType.Warning)
            warningCount++;
    }
}
