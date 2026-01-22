using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Reflection;

namespace UDA2.Logging
{
    public enum LogChannel
    {
        Default,
        Gameplay,
        AI,
        UI,
        Network,
        Audio,
        Custom
    }

    public class Logger
    {
        private const int MaxEntries = 5000;
        private static readonly string LogFilePath = Path.Combine(Application.persistentDataPath, "uda2.log");
        private static readonly List<LogEntry> _entries = new List<LogEntry>(MaxEntries);
        public static LogType MinLogLevel = LogType.Info;
        public static LogType MinUnityConsoleLevel = LogType.Warning;
        private static bool _outputToUnityConsole = true;
        private static bool _isDisposing = false;
        public static readonly string SessionId = Guid.NewGuid().ToString();
        // BuildId — версия игры из Application.version
        public static readonly string BuildId = Application.version;

        public static void LogInfo(string message, LogChannel channel = LogChannel.Default, UnityEngine.Object unityContext = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            Log(LogType.Info, channel, message, unityContext, callerFilePath, callerMemberName, callerLineNumber);
        }

        public static void LogWarning(string message, LogChannel channel = LogChannel.Default, UnityEngine.Object unityContext = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            Log(LogType.Warning, channel, message, unityContext, callerFilePath, callerMemberName, callerLineNumber);
        }

        public static void LogError(string message, LogChannel channel = LogChannel.Default, UnityEngine.Object unityContext = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            Log(LogType.Error, channel, message, unityContext, callerFilePath, callerMemberName, callerLineNumber);
        }

        private static int Severity(LogType type)
        {
            // Lower number here means more severe.
            switch (type)
            {
                case LogType.Error:
                    return 0;
                case LogType.Warning:
                    return 1;
                case LogType.Info:
                default:
                    return 2;
            }
        }

        private static void Log(LogType type, LogChannel channel, string message, UnityEngine.Object unityContext, string callerFilePath, string callerMemberName, int callerLineNumber)
        {
            if (Severity(type) > Severity(MinLogLevel))
                return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Type = type,
                Channel = channel,
                Message = message,
                CallerFilePath = callerFilePath,
                CallerMemberName = callerMemberName,
                CallerLineNumber = callerLineNumber,
                UnityContext = unityContext,
                SessionId = SessionId,
                BuildId = BuildId,
                StackTrace = type == LogType.Error ? Environment.StackTrace : null
            };
            if (_entries.Count >= MaxEntries)
                _entries.RemoveAt(0);
            _entries.Add(entry);
            OutputToUnityConsoleWithCallerInfo(entry);
            if (type == LogType.Error)
                WriteToFile(entry); // ошибки пишем сразу
        }

        private static void OutputToUnityConsoleWithCallerInfo(LogEntry entry)
        {
            if (!_outputToUnityConsole || _isDisposing)
                return;

            if (Severity(entry.Type) > Severity(MinUnityConsoleLevel))
                return;

            string line = BuildLogLine(entry);
            string color = GetColorForType(entry.Type);
            string formattedMessage = $"<color={color}>{line}</color>";

            switch (entry.Type)
            {
                case LogType.Warning:
                    Debug.LogWarning(formattedMessage, entry.UnityContext);
                    break;
                case LogType.Error:
                    Debug.LogError(formattedMessage, entry.UnityContext);
                    break;
                default:
                    Debug.Log(formattedMessage, entry.UnityContext);
                    break;
            }
        }

        private static string BuildLogLine(LogEntry entry)
        {
            string channel = entry.Channel != LogChannel.Default ? $"[{entry.Channel}]" : "";
            string stack = entry.StackTrace != null ? $"\nStackTrace: {entry.StackTrace}" : "";
            return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Type}]{channel} {entry.Message} (at {Path.GetFileName(entry.CallerFilePath)}:{entry.CallerLineNumber} in {entry.CallerMemberName}) [Session:{entry.SessionId} Build:{entry.BuildId}]{stack}";
        }

        private static string GetColorForType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning: return "#FFD700"; // gold
                case LogType.Error: return "#FF5555"; // red
                default: return "#C0C0C0"; // silver
            }
        }

        private static void WriteToFile(LogEntry entry)
        {
            try
            {
                File.AppendAllText(LogFilePath, BuildLogLine(entry) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write log to file: {ex.Message}");
            }
        }

        public static void FlushToFile()
        {
            try
            {
                var lines = new List<string>();
                lines.Add($"# SessionId: {SessionId} BuildId: {BuildId} Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                foreach (var entry in _entries)
                    lines.Add(BuildLogLine(entry));
                File.WriteAllLines(LogFilePath, lines);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to flush logs: {ex.Message}");
            }
        }

        public static void ClearLogs()
        {
            _entries.Clear();
            try
            {
                File.Delete(LogFilePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clear log file: {ex.Message}");
            }
        }

        public static void Shutdown()
        {
            _isDisposing = true;
            FlushToFile();
        }
    }
}
