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
        private ObservableCollection<VMHost> _availableVMHosts = new();

        [ObservableProperty]
        private ObservableCollection<VMInfo> _availableVMs = new();

        [ObservableProperty]
        private bool _isSelectingVMHosts;

        [ObservableProperty]
        private bool _isSelectingVMs;

       public MigrationViewModel(PowerShellManager powerShellManager, ILogService logService)
           {
               _powerShellManager = powerShellManager
                   ?? throw new ArgumentNullException(nameof(powerShellManager));
               _logService        = logService
                  ?? throw new ArgumentNullException(nameof(logService));
           }

        [RelayCommand]
        private async Task StartMigrationAsync()
        {
            try
            {
                IsMigrationInProgress = true;
                MigrationStatus = $"Migration started at {DateTime.Now:T}";
                OverallProgress = 0;

                // Get selected VMHosts and VMs
                var selectedVMHosts = GetSelectedVMHosts();
                var selectedVMs = GetSelectedVMs();

                // Clear previous migration tasks
                _migrationTasks.Clear();

                // Create migration tasks for selected VMHosts
                foreach (var VMHost in selectedVMHosts)
                {
                    var VMHostTask = new MigrationTask
                    {
                        ObjectName = VMHost.Name,
                        ObjectType = "VMHost",
                        Status = Utilities.MigrationStatus.Queued, // Use the enum from MigrationTask
                        StartTime = DateTime.Now
                    };
                    _migrationTasks.Add(VMHostTask);
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
        private async Task DiscoverVMHostsAsync()
        {
            try
            {
                AvailableVMHosts.Clear();
                AvailableVMs.Clear();

                // Get all datacenters
                var datacenters = await _powerShellManager.GetDatacentersAsync();

                foreach (var datacenter in datacenters)
                {
                    // Get clusters for each datacenter
                    var clusters = await _powerShellManager.GetClustersAsync(datacenter);

                    foreach (var cluster in clusters)
                    {
                        // Get VMHosts for each cluster
                        var VMHosts = await _powerShellManager.GetVMHostsAsync(cluster);

                        foreach (var VMHost in VMHosts)
                        {
                            // Enhance VMHost info with additional details
                            VMHost.Cluster = cluster.Name;
                            VMHost.DataCenter = datacenter.Name;
                            VMHost.IsSelected = false;

                            // Discover VMs for each VMHost
                            var vms = await _powerShellManager.GetVMsAsync(VMHost);

                            VMHost.VirtualMachines.Clear(); // Ensure clean list
                            foreach (var vm in vms)
                            {
                                // Enhance VM info with additional details
                                vm.VMHostName = VMHost.Name;
                                vm.Cluster = cluster.Name;
                                vm.DataCenter = datacenter.Name;
                                vm.IsSelected = false;

                                // Add VM to VMHost's virtual machines and global VM list
                                VMHost.VirtualMachines.Add(vm);
                                AvailableVMs.Add(vm);
                            }

                            AvailableVMHosts.Add(VMHost);
                        }
                    }
                }

                IsSelectingVMHosts = true;
                MigrationStatus = $"Discovered {AvailableVMHosts.Count} VMHosts";
                _logService.LogMessage($"VMHosts discovered successfully: {AvailableVMHosts.Count} VMHosts", "INFO");
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"VMHost discovery failed: {ex.Message}", "ERROR");
                MigrationStatus = "VMHost discovery failed";
            }
        }

        [RelayCommand]
        private Task DiscoverVMsAsync()
        {
            try
            {
                if (AvailableVMs.Count == 0)
                {
                    // Trigger VMHost discovery if no VMs are found
                    return DiscoverVMHostsAsync();
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

        // Method to get selected VMHosts and VMs
        private List<VMHost> GetSelectedVMHosts()
        {
            return AvailableVMHosts.Where(h => h.IsSelected).ToList();
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
                var selectedVMHosts = GetSelectedVMHosts();
                var selectedVMs = GetSelectedVMs();

                // Perform validation logic
                _logService.LogMessage($"Validating {selectedVMHosts.Count} VMHosts and {selectedVMs.Count} VMs", "INFO");

                // Validate migration configuration
                var validationErrors = new List<string>();

                if (selectedVMHosts.Count == 0 && selectedVMs.Count == 0)
                {
                    validationErrors.Add("No VMHosts or VMs selected for migration");
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
                    MigrationStatus = $"Validated {selectedVMHosts.Count} VMHosts and {selectedVMs.Count} VMs";
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
