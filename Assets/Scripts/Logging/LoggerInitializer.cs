using UnityEngine;
using UDA2.Logging;

public class LoggerInitializer : MonoBehaviour
{
    private int errorCount = 0;
    private int warningCount = 0;
	private bool _subscribed;

    void Awake()
    {
        UDA2.Logging.Logger.LogInfo($"Game started. Version: {UDA2.Logging.Logger.BuildId}, Device: {SystemInfo.deviceModel}, OS: {SystemInfo.operatingSystem}, Platform: {Application.platform}, Screen: {Screen.width}x{Screen.height}", LogChannel.Default);
    }

    private void OnEnable()
    {
        if (_subscribed)
            return;
        Application.logMessageReceived += OnLogMessageReceived;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (!_subscribed)
            return;
        Application.logMessageReceived -= OnLogMessageReceived;
        _subscribed = false;
    }

    private void OnDestroy()
    {
        // OnApplicationQuit is not guaranteed in the Editor in all exit paths.
        OnDisable();
    }

    void OnApplicationQuit()
    {
        UDA2.Logging.Logger.LogInfo($"Game exited. Total errors: {errorCount}, total warnings: {warningCount}", LogChannel.Default);
        UDA2.Logging.Logger.Shutdown();
        OnDisable();
    }

    private void OnLogMessageReceived(string condition, string stackTrace, UnityEngine.LogType type)
    {
        if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception)
            errorCount++;
        else if (type == UnityEngine.LogType.Warning)
            warningCount++;
    }
	// Example method to simulate settings change and log it
    public void OnSettingsChanged(object settings)
    {
        UDA2.Logging.UDAlog.Info("Settings changed: " + JsonUtility.ToJson(settings), LogChannel.UI);
    }
}
