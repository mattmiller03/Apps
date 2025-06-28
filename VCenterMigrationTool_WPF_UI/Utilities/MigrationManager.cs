using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    /// <summary>
    /// Handles migration operations.
    /// </summary>
    public class MigrationManager
    {
        private readonly PowerShellRunspaceManager _runspaceManager;

        public event Action<string, string>? LogMessage;

        private void WriteLog(string message, string level = "INFO") => LogMessage?.Invoke(message, level);

        public MigrationManager(PowerShellRunspaceManager runspaceManager)
        {
            _runspaceManager = runspaceManager;
        }

        public async Task MigrateHostAsync(
            string sourceVCenter,
            string targetVCenter,
            string vmHostName,
            PSCredential esxiHostCredential,
            PSCredential? sourceCredential = null,
            PSCredential? targetCredential = null,
            string? targetDatacenterName = null,
            string? targetClusterName = null,
            string? backupPath = null,
            string? uplinkPortgroupName = null,
            CancellationToken cancellationToken = default,
            Action<string>? progressCallback = null)
            {
            WriteLog($"🚀 Starting migration of host {vmHostName}", "INFO");
            progressCallback?.Invoke("Initializing migration...");

            var parameters = new Dictionary<string, object>()
            {
                { "Action", "Migrate" },
                { "SourceVCenter", sourceVCenter },
                { "TargetVCenter", targetVCenter },
                { "VMHostName", vmHostName },
                { "ESXiHostCredential", esxiHostCredential }
            };

            if (sourceCredential != null)
                parameters.Add("SourceCredential", sourceCredential);
            if (targetCredential != null)
                parameters.Add("TargetCredential", targetCredential);
            if (!string.IsNullOrEmpty(targetDatacenterName))
                parameters.Add("TargetDatacenterName", targetDatacenterName);
            if (!string.IsNullOrEmpty(targetClusterName))
                parameters.Add("TargetClusterName", targetClusterName);
            if (!string.IsNullOrEmpty(backupPath))
                parameters.Add("BackupPath", backupPath);
            if (!string.IsNullOrEmpty(uplinkPortgroupName))
                parameters.Add("UplinkPortgroupName", uplinkPortgroupName);

            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "VMHostConfigV2.ps1");
            if (!File.Exists(scriptPath)) throw new FileNotFoundException($"PowerShell script not found: {scriptPath}");

            using PowerShell ps = PowerShell.Create();
            ps.Runspace = _runspaceManager.Runspace;
            ps.AddCommand(scriptPath);

            foreach (var param in parameters)
                ps.AddParameter(param.Key, param.Value);

            ps.Streams.Progress.DataAdded += (sender, e) =>
            {
                var progressRecord = ps.Streams.Progress[e.Index];
                progressCallback?.Invoke($"Progress: {progressRecord.PercentComplete}% - {progressRecord.StatusDescription}");
            };

            ps.Streams.Error.DataAdded += (sender, e) =>
            {
                var errorRecord = ps.Streams.Error[e.Index];
                progressCallback?.Invoke($"Error: {errorRecord.Exception.Message}");
            };

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), ps.EndInvoke);
            cancellationToken.ThrowIfCancellationRequested();

            WriteLog($"✅ Migration completed for host {vmHostName}", "SUCCESS");
            progressCallback?.Invoke("Migration completed successfully.");
        }
        public async Task<List<MigrationTask>> PrepareMigrationAsync(MigrationType type, List<string> selectedObjects)
        {
            var migrationTasks = new List<MigrationTask>();

            try
            {
                WriteLog($"🚀 Preparing {type} migration...", "INFO");

                switch (type)
                {
                    case MigrationType.Host:
                        migrationTasks = await PrepareHostMigrationAsync(selectedObjects);
                        break;
                    case MigrationType.VM:
                        migrationTasks = await PrepareVMMigrationAsync(selectedObjects);
                        break;
                    case MigrationType.Cluster:
                        migrationTasks = await PrepareClusterMigrationAsync(selectedObjects);
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"❌ Migration preparation failed: {ex.Message}", "ERROR");
                throw;
            }

            return migrationTasks;
        }

        private async Task<List<MigrationTask>> PrepareHostMigrationAsync(List<string> hosts)
        {
            var tasks = new List<MigrationTask>();

            foreach (var host in hosts)
            {
                tasks.Add(new MigrationTask
                {
                    ObjectName = host,
                    ObjectType = "Host",
                    Status = MigrationStatus.Queued,
                    Progress = 0,
                    StartTime = DateTime.Now,
                    Details = "Preparing for migration"
                });
            }

            return tasks;
        }

        private async Task<List<MigrationTask>> PrepareVMMigrationAsync(List<string> vms)
        {
            var tasks = new List<MigrationTask>();

            foreach (var vm in vms)
            {
                tasks.Add(new MigrationTask
                {
                    ObjectName = vm,
                    ObjectType = "VM",
                    Status = MigrationStatus.Queued,
                    Progress = 0,
                    StartTime = DateTime.Now,
                    Details = "Preparing for migration"
                });
            }

            return tasks;
        }

        private async Task SimulateMigrationAsync(MigrationTask task, CancellationToken cancellationToken)
        {
            //implementation here
        }
        private void UpdateMigrationProgress(int completedTasks, int totalTasks)
        {
            //implementation here
        }

        private async Task<List<MigrationTask>> PrepareClusterMigrationAsync(List<string> clusters)
        {
            var tasks = new List<MigrationTask>();

            foreach (var cluster in clusters)
            {
                tasks.Add(new MigrationTask
                {
                    ObjectName = cluster,
                    ObjectType = "Cluster",
                    Status = MigrationStatus.Queued,
                    Progress = 0,
                    StartTime = DateTime.Now,
                    Details = "Preparing for migration"
                });
            }

            return tasks;
        }

    }
}