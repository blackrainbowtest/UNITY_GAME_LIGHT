using UnityEngine;
namespace UDA2.Logging
{
    public static class UDAlog
    {
        public static void Info(string message, LogChannel channel = LogChannel.Default, UnityEngine.Object unityContext = null,
            string callerFilePath = "", string callerMemberName = "", int callerLineNumber = 0)
        {
            Logger.LogInfo(message, channel, unityContext, callerFilePath, callerMemberName, callerLineNumber);
        }

        public static void Warning(string message, LogChannel channel = LogChannel.Default, UnityEngine.Object unityContext = null,
            string callerFilePath = "", string callerMemberName = "", int callerLineNumber = 0)
        {
            Logger.LogWarning(message, channel, unityContext, callerFilePath, callerMemberName, callerLineNumber);
        }

        public static void Error(string message, LogChannel channel = LogChannel.Default, UnityEngine.Object unityContext = null,
            string callerFilePath = "", string callerMemberName = "", int callerLineNumber = 0)
        {
            Logger.LogError(message, channel, unityContext, callerFilePath, callerMemberName, callerLineNumber);
        }
    }
}