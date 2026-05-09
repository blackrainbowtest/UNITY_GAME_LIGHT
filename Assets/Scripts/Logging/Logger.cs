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
        // BUG-001 fix: Queue<T> вместо List<T> — O(1) Dequeue вместо O(n) RemoveAt(0)
        private static readonly Queue<LogEntry> _entries = new Queue<LogEntry>(MaxEntries);
        public static LogType MinLogLevel = LogType.Info;
        public static LogType MinUnityConsoleLevel = LogType.Warning;
        private static bool _outputToUnityConsole = true;
        private static bool _isDisposing = false;
        public static readonly string SessionId = Guid.NewGuid().ToString();
        // BuildId — версия игры из Application.version
        public static readonly string BuildId = Application.version;

        // BUG-010 fix: буферизованная запись вместо File.AppendAllText на каждый лог
        private static readonly List<string> _pendingWrites = new List<string>(64);
        private static readonly object _writeLock = new object();
        private static float _lastFlushTime;
        private const float FlushIntervalSeconds = 5f;

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

            int contextId = -1;
            string contextName = null;
            if (unityContext != null)
            {
                contextId = unityContext.GetInstanceID();
                contextName = unityContext.name;
            }

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Type = type,
                Channel = channel,
                Message = message,
                CallerFilePath = callerFilePath,
                CallerMemberName = callerMemberName,
                CallerLineNumber = callerLineNumber,
				UnityContextInstanceId = contextId,
				UnityContextName = contextName,
                SessionId = SessionId,
                BuildId = BuildId,
                StackTrace = type == LogType.Error ? Environment.StackTrace : null
            };
            // BUG-001 fix: O(1) Dequeue вместо O(n) RemoveAt(0)
            if (_entries.Count >= MaxEntries)
                _entries.Dequeue();
            _entries.Enqueue(entry);
			OutputToUnityConsoleWithCallerInfo(entry, unityContext);
                if (type == LogType.Error || type == LogType.Warning || IsInfoFileWriteEnabled())
                WriteToFile(entry);
        }

            private static bool IsInfoFileWriteEnabled()
            {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
        #else
                return false;
        #endif
            }

		private static void OutputToUnityConsoleWithCallerInfo(LogEntry entry, UnityEngine.Object unityContext)
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
					Debug.LogWarning(formattedMessage, unityContext);
                    break;
                case LogType.Error:
					Debug.LogError(formattedMessage, unityContext);
                    break;
                default:
					Debug.Log(formattedMessage, unityContext);
                    break;
            }
        }

        private static string BuildLogLine(LogEntry entry)
        {
            string channel = entry.Channel != LogChannel.Default ? $"[{entry.Channel}]" : "";
            string stack = entry.StackTrace != null ? $"\nStackTrace: {entry.StackTrace}" : "";
			string ctx = entry.UnityContextName != null ? $" [Ctx:{entry.UnityContextName}#{entry.UnityContextInstanceId}]" : "";
			return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Type}]{channel} {entry.Message} (at {Path.GetFileName(entry.CallerFilePath)}:{entry.CallerLineNumber} in {entry.CallerMemberName}) [Session:{entry.SessionId} Build:{entry.BuildId}]{ctx}{stack}";
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

        // BUG-010 fix: буферизованная запись — накапливаем строки и сбрасываем пачкой
        private static void WriteToFile(LogEntry entry)
        {
            lock (_writeLock)
            {
                _pendingWrites.Add(BuildLogLine(entry));
            }

            // Сбрасываем на диск не чаще чем раз в FlushIntervalSeconds
            float now = Time.realtimeSinceStartup;
            if (now - _lastFlushTime >= FlushIntervalSeconds)
            {
                FlushPendingWrites();
            }
        }

        /// <summary>
        /// Сбрасывает накопленные строки логов на диск одной операцией записи.
        /// </summary>
        public static void FlushPendingWrites()
        {
            string[] linesToWrite;
            lock (_writeLock)
            {
                if (_pendingWrites.Count == 0)
                    return;
                linesToWrite = _pendingWrites.ToArray();
                _pendingWrites.Clear();
            }

            try
            {
                File.AppendAllText(LogFilePath,
                    string.Join(Environment.NewLine, linesToWrite) + Environment.NewLine);
                _lastFlushTime = Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write log to file: {ex.Message}");
            }
        }

        public static void FlushToFile()
        {
            // Сначала сбрасываем pending writes
            FlushPendingWrites();

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
            lock (_writeLock)
            {
                _pendingWrites.Clear();
            }
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
            FlushPendingWrites();
            FlushToFile();
        }
    }
}
