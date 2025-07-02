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
            _logViewModel.AddLogEntry(message, level);
        }
    }
}