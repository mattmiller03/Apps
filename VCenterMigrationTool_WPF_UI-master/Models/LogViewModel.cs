using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public class LogViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
        private string _selectedLogLevel = "All";
        private string _logText = "";
        private readonly ICommand _clearLogCommand;
        private readonly ICommand _saveLogCommand;

        public LogViewModel()
        {
            _clearLogCommand = new RelayCommand(ClearLog);
            _saveLogCommand = new RelayCommand(SaveLog);
        }

        public ObservableCollection<LogEntry> LogEntries
        {
            get { return _logEntries; }
            set
            {
                _logEntries = value;
                OnPropertyChanged();
                UpdateLogText(); // Update the LogText when the entries change
            }
        }

        public string SelectedLogLevel
        {
            get { return _selectedLogLevel; }
            set
            {
                _selectedLogLevel = value;
                OnPropertyChanged();
                UpdateLogText(); // Update the LogText when the filter changes
            }
        }

        public string LogText
        {
            get { return _logText; }
            set
            {
                _logText = value;
                OnPropertyChanged();
            }
        }

        public ICommand ClearLogCommand => _clearLogCommand;
        public ICommand SaveLogCommand => _saveLogCommand;

        public void WriteLog(string message, string level = "INFO")
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            };

            LogEntries.Add(logEntry);
            UpdateLogText();
        }

        private void ClearLog()
        {
            LogEntries.Clear();
            UpdateLogText();
        }

        private void SaveLog()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log",
                DefaultExt = "txt",
                FileName = $"VCenterMigration_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, LogText);
                WriteLog($"💾 Log saved to: {dialog.FileName}", "INFO");
            }
        }

        private void UpdateLogText()
        {
            // Apply the log level filter
            string filteredText = string.Join(Environment.NewLine, LogEntries
                .Where(entry => SelectedLogLevel == "All" || entry.Level == SelectedLogLevel)
                .Select(entry => entry.ToString()));  // Use the LogEntry.ToString() method

            LogText = filteredText;
            OnPropertyChanged(nameof(LogText));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
