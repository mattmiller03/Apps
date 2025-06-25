// Models/MigrationProgress.cs
using System;
using System.ComponentModel;

namespace VCenterMigrationTool.Models
{
    public class MigrationProgress : INotifyPropertyChanged
    {
        private string _vmName;
        private MigrationStatus _status;
        private int _progressPercentage;
        private string _currentStep;
        private DateTime _startTime;
        private DateTime? _endTime;
        private string _errorMessage;
        private long _dataTransferred;
        private long _totalDataSize;

        public string VmName
        {
            get => _vmName;
            set
            {
                _vmName = value;
                OnPropertyChanged(nameof(VmName));
            }
        }

        public MigrationStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public int ProgressPercentage
        {
            get => _progressPercentage;
            set
            {
                _progressPercentage = value;
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }

        public string CurrentStep
        {
            get => _currentStep;
            set
            {
                _currentStep = value;
                OnPropertyChanged(nameof(CurrentStep));
            }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged(nameof(StartTime));
                OnPropertyChanged(nameof(ElapsedTime));
            }
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged(nameof(EndTime));
                OnPropertyChanged(nameof(ElapsedTime));
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        public long DataTransferred
        {
            get => _dataTransferred;
            set
            {
                _dataTransferred = value;
                OnPropertyChanged(nameof(DataTransferred));
                OnPropertyChanged(nameof(DataTransferredFormatted));
                OnPropertyChanged(nameof(TransferSpeed));
            }
        }

        public long TotalDataSize
        {
            get => _totalDataSize;
            set
            {
                _totalDataSize = value;
                OnPropertyChanged(nameof(TotalDataSize));
                OnPropertyChanged(nameof(TotalDataSizeFormatted));
            }
        }

        public string StatusText => Status.ToString().Replace("_", " ");

        public TimeSpan ElapsedTime
        {
            get
            {
                var endTime = EndTime ?? DateTime.Now;
                return endTime - StartTime;
            }
        }

        public string DataTransferredFormatted => FormatBytes(DataTransferred);
        public string TotalDataSizeFormatted => FormatBytes(TotalDataSize);

        public string TransferSpeed
        {
            get
            {
                var elapsed = ElapsedTime.TotalSeconds;
                if (elapsed > 0 && DataTransferred > 0)
                {
                    var bytesPerSecond = DataTransferred / elapsed;
                    return $"{FormatBytes((long)bytesPerSecond)}/s";
                }
                return "0 B/s";
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum MigrationStatus
    {
        Pending,
        Initializing,
        Copying_Data,
        Configuring_VM,
        Finalizing,
        Completed,
        Failed,
        Cancelled
    }
}