using System;

namespace VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces
{
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);

        /// <summary>
        /// Gets raised when any message is logged.
        /// First argument is the message text, second is the level ("INFO","WARN","ERROR").
        /// </summary>
        event Action<string, string> MessageWritten;
    }
}