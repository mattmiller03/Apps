using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Security;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public sealed class PowerShellManager : IDisposable
    {
        private readonly Runspace _runspace;
        private readonly string _scriptsPath;

        public bool IsSourceConnected { get; private set; }
        public bool IsDestinationConnected { get; private set; }
        public bool IsPowerCLIAvailable { get; private set; }

        // Event for logging
        public event Action<string, string>? LogMessage;

        public PowerShellManager()
        {
            _runspace = RunspaceFactory.CreateRunspace();
            _runspace.Open();

            // Resolve scripts directory relative to executable
            _scriptsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Scripts");
        }

        private void WriteLog(string message, string level = "INFO") =>
            LogMessage?.Invoke(message, level);

        /// <summary>
        /// Executes a PowerShell script file with parameters.
        /// </summary>
        public async Task<string> ExecuteScriptAsync(
        string scriptName,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
        {
            var scriptPath = Path.Combine(_scriptsPath, scriptName);
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Script not found: {scriptPath}");

            // Build parameter string
            var paramArgs = string.Join(" ", parameters.Select(p =>
                $"-{p.Name} \"{p.Value}\""));

            var command = $@"
                try {{
                    & '{scriptPath.Replace("'", "''")}' {paramArgs}
                }}
                catch {{
                    Write-Error $_.Exception.Message
                    throw
                }}
            ";

            return await ExecuteCommandAsync(command, cancellationToken);
        }

        // Example: Backup Resource Pools
        public async Task BackupResourcePoolsAsync(
            string backupPath,
            CancellationToken cancellationToken = default)
        {
            WriteLog("Starting Resource Pool backup...", "INFO");
            await ExecuteScriptAsync(
                "ResourcePool-Export.ps1",
                cancellationToken,
                ("BackupPath", backupPath));
        }

        // Example: Migrate VM
        public async Task MigrateVMAsync(
            string vmName,
            string sourceVCenter,
            string destVCenter,
            CancellationToken cancellationToken = default)
        {
            WriteLog($"Migrating VM {vmName}...", "INFO");
            await ExecuteScriptAsync(
                "CrossVcenterVMMigration.ps1",
                cancellationToken,
                ("VMName", vmName),
                ("SourceVCenter", sourceVCenter),
                ("DestVCenter", destVCenter));
        }

        // Helper: Execute raw PowerShell command
        private async Task<string> ExecuteCommandAsync(
            string command,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var ps = PowerShell.Create();
                ps.Runspace = _runspace;
                ps.AddScript(command);

                var results = ps.Invoke();
                var output = string.Join(Environment.NewLine, results.Select(r => r.ToString()));

                if (ps.Streams.Error.Count > 0)
                    throw new InvalidOperationException(
                        string.Join(Environment.NewLine, ps.Streams.Error.Select(e => e.ToString())));

                return output;
            }, cancellationToken);
        }

        public void Dispose()
        {
            _runspace?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
