using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public partial class LogViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<LogEntry> _logEntries = new();

        [RelayCommand]
        private void ClearLogs()
        {
            LogEntries.Clear();
        }

        public void AddLogEntry(string message, string level)
        {
            var entry = new LogEntry(DateTime.Now, level, message);
            App.Current.Dispatcher.Invoke(() =>
            {
                LogEntries.Add(entry);
            });
        }
    }
}
