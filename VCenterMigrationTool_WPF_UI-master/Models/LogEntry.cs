using System;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI
{
    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARNING,
        ERROR,
        FATAL
    }

    public class LogEntry
    {
        public LogEntry(DateTime timestamp, LogLevel level, string message)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public DateTime Timestamp { get; }
        public LogLevel Level { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}";
        }
    }
}
