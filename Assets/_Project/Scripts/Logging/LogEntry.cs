using System;

namespace UDA2.Logging
{
    public enum LogType
    {
        Info,
        Warning,
        Error
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogType Type { get; set; }
        public string Message { get; set; }
        public string CallerFilePath { get; set; }
        public string CallerMemberName { get; set; }
        public int CallerLineNumber { get; set; }
        public LogChannel Channel { get; set; }
        public UnityEngine.Object UnityContext { get; set; }
        public string SessionId { get; set; }
        public string BuildId { get; set; }
        public string StackTrace { get; set; }
    }
}
