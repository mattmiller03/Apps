namespace VCenterMigrationTool_WPF_UI.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; }
        public string Level { get; }
        public string Message { get; }

        public LogEntry(DateTime timestamp, string level, string message)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message;
        }
    }
}
