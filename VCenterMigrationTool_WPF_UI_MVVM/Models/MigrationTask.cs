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
        private DateTime? _endTime;
        private string _details = string.Empty;

        public string ObjectName
        {
            get => _objectName;
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
            get => _objectType;
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
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Duration));
                }
            }
        }

        public double Progress
        {
            get => _progress;
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
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Duration));
                }
            }
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Duration));
                }
            }
        }

        public string Details
        {
            get => _details;
            set
            {
                if (_details != value)
                {
                    _details = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (EndTime.HasValue && StartTime != DateTime.MinValue)
                {
                    return EndTime.Value - StartTime;
                }
                else if (Status == MigrationStatus.InProgress && StartTime != DateTime.MinValue)
                {
                    return DateTime.Now - StartTime;
                }
                return TimeSpan.Zero;
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
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
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
