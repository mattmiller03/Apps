using System;

namespace VCenterMigrationTool_WPF_UI.Infrastructure
{
    public sealed class Logger
    {
        public event Action<string, string>? MessageWritten;
        public void Info(string m) => MessageWritten?.Invoke(m, "INFO");
        public void Warn(string m) => MessageWritten?.Invoke(m, "WARN");
        public void Error(string m) => MessageWritten?.Invoke(m, "ERR ");
    }
}

