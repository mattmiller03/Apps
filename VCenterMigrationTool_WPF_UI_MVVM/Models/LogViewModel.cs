using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input; // Use the toolkit's RelayCommand
using Microsoft.Win32;
using VCenterMigrationTool_WPF_UI;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;// For LogEntry and LogLevel

namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public class LogViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
        private string _selectedLogLevel = "All";
        private string _logText = "";

        public LogViewModel()
        {
            ClearLogCommand = new RelayCommand(ClearLog);
            SaveLogCommand = new RelayCommand(SaveLog);
        }

        public ObservableCollection<LogEntry> LogEntries
        {
            get => _logEntries;
            set
            {
                _logEntries = value;
                OnPropertyChanged();
                UpdateLogText();
            }
        }

        public string SelectedLogLevel
        {
            get => _selectedLogLevel;
            set
            {
                _selectedLogLevel = value;
                OnPropertyChanged();
                UpdateLogText();
            }
        }

        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                OnPropertyChanged();
            }
        }

        public ICommand ClearLogCommand { get; }
        public ICommand SaveLogCommand { get; }

        public void WriteLog(string message, LogLevel level = LogLevel.INFO)
        {
            var logEntry = new LogEntry(DateTime.Now, level, message);
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
                WriteLog($"💾 Log saved to: {dialog.FileName}", LogLevel.INFO);
            }
        }

        private void UpdateLogText()
        {
            var filtered = LogEntries
                .Where(entry => SelectedLogLevel == "All" || entry.Level.ToString() == SelectedLogLevel)
                .Select(entry => entry.ToString());
            LogText = string.Join(Environment.NewLine, filtered);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}



