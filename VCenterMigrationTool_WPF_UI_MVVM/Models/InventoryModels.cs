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
