using System;
using System.Diagnostics;
using VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public class Logger : ILogger
    {
        public event Action<string, string> MessageWritten;

        public void Info(string message) => Log(message, "INFO");
        public void Warn(string message) => Log(message, "WARN");
        public void Error(string message) => Log(message, "ERROR");

        private void Log(string message, string level)
        {
            var formatted = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
            Debug.WriteLine(formatted);
            MessageWritten?.Invoke(message, level);
        }
    }
}
