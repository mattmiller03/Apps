using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VCenterMigrationTool_WPF_UI
{
    public enum ValidationResultStatus
    {
        Passed,
        Failed,
        Warning,
        Information
    }

    public class ValidationResult : INotifyPropertyChanged
    {
        private string _testName = string.Empty;
        private ValidationResultStatus _result;
        private string _details = string.Empty;
        private string _recommendation = string.Empty;

        public string TestName
        {
            get { return _testName; }
            set
            {
                if (_testName != value)
                {
                    _testName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ValidationResultStatus Result
        {
            get { return _result; }
            set
            {
                if (_result != value)
                {
                    _result = value;
                    OnPropertyChanged();
                }
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

        public string Recommendation
        {
            get { return _recommendation; }
            set
            {
                if (_recommendation != value)
                {
                    _recommendation = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
