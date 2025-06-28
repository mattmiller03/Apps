using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class MigrationTask : INotifyPropertyChanged
    {
        private string _objectName = string.Empty;
        private string _objectType = string.Empty;
        private MigrationStatus _status;
        private double _progress;
        private DateTime _startTime;
        private string _details = string.Empty;

        public string ObjectName
        {
            get { return _objectName; }
            set
            {
                if (_objectName != value)
                {
                    _objectName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ObjectType
        {
            get { return _objectType; }
            set
            {
                if (_objectType != value)
                {
                    _objectType = value;
                    OnPropertyChanged();
                }
            }
        }

        public MigrationStatus Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Duration)); // Recalculate duration when status changes
                }
            }
        }

        public double Progress
        {
            get { return _progress; }
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime StartTime
        {
            get { return _startTime; }
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Duration)); // Recalculate duration when start time changes
                }
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (Status == MigrationStatus.InProgress || Status == MigrationStatus.Completed || Status == MigrationStatus.Failed || Status == MigrationStatus.Cancelled)
                {
                    return DateTime.Now - StartTime;
                }
                return TimeSpan.Zero; // Or some other appropriate default
            }
        }

        public string Details
        {
            get { return _details; }
            set
            {
                if (_details != value)
                {
                    _details = value;
                    OnPropertyChanged();
                }
            }
        }

        public void UpdateProgress(double progress, string details)
        {
            Progress = progress;
            Details = details;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum MigrationStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        Queued
    }

    public enum MigrationType
    {
        Host,
        VM,
        Cluster
    }
}
