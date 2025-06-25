// Models/MigrationSettings.cs
using System.ComponentModel;

namespace VCenterMigrationTool.Models
{
    public class MigrationSettings : INotifyPropertyChanged
    {
        private int _maxConcurrentMigrations = 2;
        private int _timeoutMinutes = 60;
        private bool _preserveNetworkSettings = true;
        private bool _powerOnAfterMigration = false;
        private bool _createSnapshot = true;
        private string _snapshotName = "Pre-Migration Snapshot";
        private bool _validateAfterMigration = true;
        private bool _removeSourceVmAfterMigration = false;
        private int _retryAttempts = 3;
        private int _retryDelayMinutes = 5;

        public int MaxConcurrentMigrations
        {
            get => _maxConcurrentMigrations;
            set
            {
                _maxConcurrentMigrations = value;
                OnPropertyChanged(nameof(MaxConcurrentMigrations));
            }
        }

        public int TimeoutMinutes
        {
            get => _timeoutMinutes;
            set
            {
                _timeoutMinutes = value;
                OnPropertyChanged(nameof(TimeoutMinutes));
            }
        }

        public bool PreserveNetworkSettings
        {
            get => _preserveNetworkSettings;
            set
            {
                _preserveNetworkSettings = value;
                OnPropertyChanged(nameof(PreserveNetworkSettings));
            }
        }

        public bool PowerOnAfterMigration
        {
            get => _powerOnAfterMigration;
            set
            {
                _powerOnAfterMigration = value;
                OnPropertyChanged(nameof(PowerOnAfterMigration));
            }
        }

        public bool CreateSnapshot
        {
            get => _createSnapshot;
            set
            {
                _createSnapshot = value;
                OnPropertyChanged(nameof(CreateSnapshot));
            }
        }

        public string SnapshotName
        {
            get => _snapshotName;
            set
            {
                _snapshotName = value;
                OnPropertyChanged(nameof(SnapshotName));
            }
        }

        public bool ValidateAfterMigration
        {
            get => _validateAfterMigration;
            set
            {
                _validateAfterMigration = value;
                OnPropertyChanged(nameof(ValidateAfterMigration));
            }
        }

        public bool RemoveSourceVmAfterMigration
        {
            get => _removeSourceVmAfterMigration;
            set
            {
                _removeSourceVmAfterMigration = value;
                OnPropertyChanged(nameof(RemoveSourceVmAfterMigration));
            }
        }

        public int RetryAttempts
        {
            get => _retryAttempts;
            set
            {
                _retryAttempts = value;
                OnPropertyChanged(nameof(RetryAttempts));
            }
        }

        public int RetryDelayMinutes
        {
            get => _retryDelayMinutes;
            set
            {
                _retryDelayMinutes = value;
                OnPropertyChanged(nameof(RetryDelayMinutes));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}