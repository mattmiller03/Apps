using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public class LogServiceAdapter : ILogService
    {
        private readonly LogViewModel _logViewModel;

        public LogServiceAdapter(LogViewModel logViewModel)
        {
            _logViewModel = logViewModel;
        }

        public void LogMessage(string message, string level)
        {
            LogLevel logLevel = LogLevel.INFO;

            switch (level.ToUpper())
            {
                case "ERROR":
                    logLevel = LogLevel.ERROR;
                    break;
                case "WARNING":
                    logLevel = LogLevel.WARNING;
                    break;
                case "DEBUG":
                    logLevel = LogLevel.DEBUG;
                    break;
                default:
                    logLevel = LogLevel.INFO;
                    break;
            }

            _logViewModel.WriteLog(message, logLevel);
        }
    }
}