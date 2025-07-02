using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public partial class MigrationViewModel : ObservableObject
    {
        private readonly PowerShellManager _powerShellManager;
        private readonly ILogService _logService;

        [ObservableProperty]
        private string _migrationStatus = "Ready for migration tasks";

        [ObservableProperty]
        private ObservableCollection<MigrationTask> _migrationTasks = new();

        [ObservableProperty]
        private int _overallProgress;

        [ObservableProperty]
        private bool _isMigrationInProgress;

        // Migration Task Selection Properties
        [ObservableProperty]
        private bool _migrateFolders;

        [ObservableProperty]
        private bool _migrateResourcePools;

        [ObservableProperty]
        private bool _migrateVDS;

        [ObservableProperty]
        private bool _preserveNetworkConfig;

        [ObservableProperty]
        private bool _migrateStoragePolicies;

        // New properties for granular selection
        [ObservableProperty]
        private ObservableCollection<HostInfo> _availableHosts = new();

        [ObservableProperty]
        private ObservableCollection<VMInfo> _availableVMs = new();

        [ObservableProperty]
        private bool _isSelectingHosts;

        [ObservableProperty]
        private bool _isSelectingVMs;

        [RelayCommand]
        private async Task StartMigrationAsync()
        {
            try
            {
                IsMigrationInProgress = true;
                MigrationStatus = $"Migration started at {DateTime.Now:T}";
                OverallProgress = 0;

                // Get selected hosts and VMs
                var selectedHosts = GetSelectedHosts();
                var selectedVMs = GetSelectedVMs();

                // Clear previous migration tasks
                _migrationTasks.Clear();

                // Create migration tasks for selected hosts
                foreach (var host in selectedHosts)
                {
                    var hostTask = new MigrationTask
                    {
                        ObjectName = host.Name,
                        ObjectType = "Host",
                        Status = Utilities.MigrationStatus.Queued, // Use the enum from MigrationTask
                        StartTime = DateTime.Now
                    };
                    _migrationTasks.Add(hostTask);
                }

                // Create migration tasks for selected VMs
                foreach (var vm in selectedVMs)
                {
                    var vmTask = new MigrationTask
                    {
                        ObjectName = vm.Name,
                        ObjectType = "Virtual Machine",
                        Status = Utilities.MigrationStatus.Queued, // Use the enum from MigrationTask
                        StartTime = DateTime.Now
                    };
                    _migrationTasks.Add(vmTask);
                }

                // Simulate migration process
                for (int i = 0; i < _migrationTasks.Count; i++)
                {
                    var task = _migrationTasks[i];
                    task.Status = Utilities.MigrationStatus.InProgress;

                    try
                    {
                        // Simulate actual migration logic
                        await SimulateMigrationTask(task);
                    }
                    catch (Exception ex)
                    {
                        task.Status = Utilities.MigrationStatus.Failed;
                        task.Details = ex.Message;
                        _logService.LogMessage($"Migration of {task.ObjectName} failed: {ex.Message}", "ERROR");
                    }
                }

                MigrationStatus = $"Migration completed at {DateTime.Now:T}";
                _logService.LogMessage("Migration completed successfully", "INFO");
            }
            catch (Exception ex)
            {
                MigrationStatus = "Migration failed";
                _logService.LogMessage($"Migration error: {ex.Message}", "ERROR");
            }
            finally
            {
                IsMigrationInProgress = false;
            }
        }

        private async Task SimulateMigrationTask(MigrationTask task)
        {
            for (int progress = 0; progress <= 100; progress += 10)
            {
                await Task.Delay(500);
                task.UpdateProgress(progress, $"Processing {task.ObjectName}");
                OverallProgress = (_migrationTasks.IndexOf(task) * 100 + progress) / _migrationTasks.Count;
            }

            task.Status = Utilities.MigrationStatus.Completed;
            task.EndTime = DateTime.Now;
        }

        [RelayCommand]
        private async Task DiscoverHostsAsync()
        {
            try
            {
                AvailableHosts.Clear();
                AvailableVMs.Clear();

                // Get all datacenters
                var datacenters = await _powerShellManager.GetDatacentersAsync();

                foreach (var datacenter in datacenters)
                {
                    // Get clusters for each datacenter
                    var clusters = await _powerShellManager.GetClustersAsync(datacenter);

                    foreach (var cluster in clusters)
                    {
                        // Get hosts for each cluster
                        var hosts = await _powerShellManager.GetHostsAsync(cluster);

                        foreach (var host in hosts)
                        {
                            // Enhance host info with additional details
                            host.Cluster = cluster.Name;
                            host.DataCenter = datacenter.Name;
                            host.IsSelected = false;

                            // Discover VMs for each host
                            var vms = await _powerShellManager.GetVMsAsync(host);

                            host.VirtualMachines.Clear(); // Ensure clean list
                            foreach (var vm in vms)
                            {
                                // Enhance VM info with additional details
                                vm.HostName = host.Name;
                                vm.Cluster = cluster.Name;
                                vm.DataCenter = datacenter.Name;
                                vm.IsSelected = false;

                                // Add VM to host's virtual machines and global VM list
                                host.VirtualMachines.Add(vm);
                                AvailableVMs.Add(vm);
                            }

                            AvailableHosts.Add(host);
                        }
                    }
                }

                IsSelectingHosts = true;
                MigrationStatus = $"Discovered {AvailableHosts.Count} hosts";
                _logService.LogMessage($"Hosts discovered successfully: {AvailableHosts.Count} hosts", "INFO");
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Host discovery failed: {ex.Message}", "ERROR");
                MigrationStatus = "Host discovery failed";
            }
        }

        [RelayCommand]
        private Task DiscoverVMsAsync()
        {
            try
            {
                if (AvailableVMs.Count == 0)
                {
                    // Trigger host discovery if no VMs are found
                    return DiscoverHostsAsync();
                }

                IsSelectingVMs = true;
                MigrationStatus = $"Discovered {AvailableVMs.Count} VMs";
                _logService.LogMessage($"VMs discovered successfully: {AvailableVMs.Count} VMs", "INFO");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"VM discovery failed: {ex.Message}", "ERROR");
                MigrationStatus = "VM discovery failed";
                return Task.CompletedTask;
            }
        }

        // Method to get selected hosts and VMs
        private List<HostInfo> GetSelectedHosts()
        {
            return AvailableHosts.Where(h => h.IsSelected).ToList();
        }

        private List<VMInfo> GetSelectedVMs()
        {
            return AvailableVMs.Where(vm => vm.IsSelected).ToList();
        }

        [RelayCommand]
        private void ValidateSelectedTasks()
        {
            try
            {
                var selectedHosts = GetSelectedHosts();
                var selectedVMs = GetSelectedVMs();

                // Perform validation logic
                _logService.LogMessage($"Validating {selectedHosts.Count} hosts and {selectedVMs.Count} VMs", "INFO");

                // Validate migration configuration
                var validationErrors = new List<string>();

                if (selectedHosts.Count == 0 && selectedVMs.Count == 0)
                {
                    validationErrors.Add("No hosts or VMs selected for migration");
                }

                if (!MigrateFolders && !MigrateResourcePools && !MigrateVDS)
                {
                    validationErrors.Add("No infrastructure tasks selected");
                }

                if (validationErrors.Any())
                {
                    MigrationStatus = "Validation failed";
                    _logService.LogMessage("Migration validation failed", "WARNING");
                    // TODO: Show validation errors to user
                }
                else
                {
                    MigrationStatus = $"Validated {selectedHosts.Count} hosts and {selectedVMs.Count} VMs";
                }
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Validation failed: {ex.Message}", "ERROR");
                MigrationStatus = "Validation failed";
            }
        }

        [RelayCommand]
        private void PauseMigration()
        {
            MigrationStatus = "Migration paused";
            _logService.LogMessage("Migration paused", "WARNING");
        }

        [RelayCommand]
        private void StopMigration()
        {
            MigrationStatus = "Migration stopped";
            OverallProgress = 0;
            IsMigrationInProgress = false;
            _logService.LogMessage("Migration stopped", "WARNING");
        }

        [RelayCommand]
        private void ExportReport()
        {
            try
            {
                // TODO: Implement actual export logic
                MigrationStatus = "Migration report exported";
                _logService.LogMessage("Migration report exported", "INFO");
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Report export failed: {ex.Message}", "ERROR");
            }
        }
    }
}
