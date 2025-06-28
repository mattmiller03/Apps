using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    /// <summary>
    /// Manages inventory retrieval from vCenter.
    /// </summary>
    public class InventoryManager
    {
        private readonly PowerShellRunspaceManager _runspaceManager;

        public event Action<string, string>? LogMessage;

        private void WriteLog(string message, string level = "INFO") => LogMessage?.Invoke(message, level);

        public InventoryManager(PowerShellRunspaceManager runspaceManager)
        {
            _runspaceManager = runspaceManager;
        }

        public async Task<List<DatacenterInfo>> GetDatacentersAsync()
        {
            try
            {
                var command = @"
                    Get-Datacenter | ForEach-Object {
                        ""$($_.Name)|$($_.Id)""
                    }
                ";

                var results = await _runspaceManager.InvokeScriptAsync("-Command", new System.Collections.Generic.Dictionary<string, object> { { "Command", command } });
                var datacenters = new List<DatacenterInfo>();

                foreach (var line in results.Select(r => r.ToString()).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2)
                        datacenters.Add(new DatacenterInfo { Name = parts[0], Id = parts[1] });
                }

                WriteLog($"✅ Retrieved {datacenters.Count} datacenters", "INFO");
                return datacenters;
            }
            catch (Exception ex)
            {
                WriteLog($"❌ Error retrieving datacenters: {ex.Message}", "ERROR");
                return new List<DatacenterInfo>();
            }
        }

        public async Task<List<ClusterInfo>> GetClustersAsync(DatacenterInfo datacenter)
        {
            try
            {
                var command = $@"
                    Get-Cluster -Location (Get-Datacenter -Name '{datacenter.Name}') | ForEach-Object {{
                        ""$($_.Name)|$($_.Id)""
                    }}
                ";

                var results = await _runspaceManager.InvokeScriptAsync("-Command", new System.Collections.Generic.Dictionary<string, object> { { "Command", command } });
                var clusters = new List<ClusterInfo>();

                foreach (var line in results.Select(r => r.ToString()).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2)
                        clusters.Add(new ClusterInfo { Name = parts[0], Id = parts[1] });
                }

                WriteLog($"✅ Retrieved {clusters.Count} clusters for datacenter {datacenter.Name}", "INFO");
                return clusters;
            }
            catch (Exception ex)
            {
                WriteLog($"❌ Error retrieving clusters: {ex.Message}", "ERROR");
                return new List<ClusterInfo>();
            }
        }

        public async Task<List<HostInfo>> GetHostsAsync(ClusterInfo cluster)
        {
            try
            {
                var command = $@"
                    Get-VMHost -Location (Get-Cluster -Name '{cluster.Name}') | ForEach-Object {{
                        ""$($_.Name)|$($_.Id)|$($_.ConnectionState)""
                    }}
                ";

                var results = await _runspaceManager.InvokeScriptAsync("-Command", new System.Collections.Generic.Dictionary<string, object> { { "Command", command } });
                var hosts = new List<HostInfo>();

                foreach (var line in results.Select(r => r.ToString()).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var parts = line.Split('|');
                    if (parts.Length == 3)
                        hosts.Add(new HostInfo { Name = parts[0], Id = parts[1], ConnectionState = parts[2] });
                }

                WriteLog($"✅ Retrieved {hosts.Count} hosts for cluster {cluster.Name}", "INFO");
                return hosts;
            }
            catch (Exception ex)
            {
                WriteLog($"❌ Error retrieving hosts: {ex.Message}", "ERROR");
                return new List<HostInfo>();
            }
        }

        public async Task<List<VMInfo>> GetVMsAsync(HostInfo host)
        {
            try
            {
                var command = $@"
                    Get-VM -Location (Get-VMHost -Name '{host.Name}') | ForEach-Object {{
                        ""$($_.Name)|$($_.Id)|$($_.PowerState)""
                    }}
                ";

                var results = await _runspaceManager.InvokeScriptAsync("-Command", new System.Collections.Generic.Dictionary<string, object> { { "Command", command } });
                var vms = new List<VMInfo>();

                foreach (var line in results.Select(r => r.ToString()).Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var parts = line.Split('|');
                    if (parts.Length == 3)
                        vms.Add(new VMInfo { Name = parts[0], Id = parts[1], PowerState = parts[2] });
                }

                WriteLog($"✅ Retrieved {vms.Count} VMs for host {host.Name}", "INFO");
                return vms;
            }
            catch (Exception ex)
            {
                WriteLog($"❌ Error retrieving VMs: {ex.Message}", "ERROR");
                return new List<VMInfo>();
            }
        }
    }
}