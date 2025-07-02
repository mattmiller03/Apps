using System.ComponentModel;
using System.Runtime.CompilerServices;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public interface IInventoryItem
    {
        string Name { get; }
        string Id { get; }
    }

    public abstract class InventoryItemBase : IInventoryItem, INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _id = string.Empty;

        public InventoryItemBase(string name, string id)
        {
            _name = name;
            _id = id;
        }

        public string Name
        {
            get { return _name; }
            protected set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Id
        {
            get { return _id; }
            protected set
            {
                if (_id != value)
                {
                    _id = value;
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

    public enum HostConnectionState
    {
        Connected,
        Disconnected,
        NotResponding
    }

    public class HostInfo : InventoryItemBase
    {
        private HostConnectionState _connectionState;
        private bool _isSelected;
        private string _cluster = string.Empty;
        private string _dataCenter = string.Empty;
        private List<VMInfo> _virtualMachines = new();

        public HostInfo(string name, string id, HostConnectionState connectionState) : base(name, id)
        {
            _connectionState = connectionState;
        }

        public HostConnectionState ConnectionState
        {
            get { return _connectionState; }
            protected set
            {
                if (_connectionState != value)
                {
                    _connectionState = value;
                    OnPropertyChanged();
                }
            }
        }
        // New properties for MigrationViewModel
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Cluster
        {
            get { return _cluster; }
            set
            {
                if (_cluster != value)
                {
                    _cluster = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DataCenter
        {
            get { return _dataCenter; }
            set
            {
                if (_dataCenter != value)
                {
                    _dataCenter = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<VMInfo> VirtualMachines
        {
            get { return _virtualMachines; }
            set
            {
                if (_virtualMachines != value)
                {
                    _virtualMachines = value;
                    OnPropertyChanged();
                }
            }
        }

        // Computed property for status
        public string Status => ConnectionState.ToString();
    }

    public enum VMPowerState
    {
        PoweredOn,
        PoweredOff,
        Suspended
    }

    public class VMInfo : InventoryItemBase
    {
        private VMPowerState _powerState;
        private bool _isSelected;
        private string _hostName = string.Empty;
        private string _cluster = string.Empty;
        private string _dataCenter = string.Empty;
        private string _configuredSize = string.Empty;

        public VMInfo(string name, string id, VMPowerState powerState) : base(name, id)
        {
            _powerState = powerState;
        }

        public VMPowerState PowerState
        {
            get { return _powerState; }
            protected set
            {
                if (_powerState != value)
                {
                    _powerState = value;
                    OnPropertyChanged();
                }
            }
        }
        // New properties for MigrationViewModel
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string HostName
        {
            get { return _hostName; }
            set
            {
                if (_hostName != value)
                {
                    _hostName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Cluster
        {
            get { return _cluster; }
            set
            {
                if (_cluster != value)
                {
                    _cluster = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DataCenter
        {
            get { return _dataCenter; }
            set
            {
                if (_dataCenter != value)
                {
                    _dataCenter = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConfiguredSize
        {
            get { return _configuredSize; }
            set
            {
                if (_configuredSize != value)
                {
                    _configuredSize = value;
                    OnPropertyChanged();
                }
            }
        }
    }


    public class DatacenterInfo : InventoryItemBase
    {
        public DatacenterInfo(string name, string id) : base(name, id) { }

    }

    public class ClusterInfo : InventoryItemBase
    {
        public ClusterInfo(string name, string id) : base(name, id) { }
    }
}
