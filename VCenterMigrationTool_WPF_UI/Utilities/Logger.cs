using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public class Logger
    {
        public event Action<string, string> MessageLogged;

        public void Info(string message) => Log(message, "INFO");
        public void Warn(string message) => Log(message, "WARN");
        public void Error(string message) => Log(message, "ERROR");

        private void Log(string message, string level)
        {
            var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
            Debug.WriteLine(formattedMessage);
            MessageLogged?.Invoke(message, level);
        }
    }
}
