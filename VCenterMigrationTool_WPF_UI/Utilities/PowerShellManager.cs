// Utilities/PowerShellManager.cs
using System;
using System.Collections.Generic;
using System.IO;                 // NEW
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;           // for SecureString→PSCredential helper
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI.Utilities;

public sealed class PowerShellManager : IDisposable
{
    #region ── fields / ctor ────────────────────────────────────────────
    private Runspace? _runspace;
    private bool _vmHostModuleLoaded;          // NEW
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly char[] InvalidFileNameChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];
    public bool IsSourceConnected { get; private set; }
    public bool IsDestinationConnected { get; private set; }
    public bool IsPowerCLIAvailable { get; private set; }
    public void DisconnectAll() { /* TODO: real implementation */ }
    // (message , level)
    public event Action<string, string>? LogMessage;

    private void WriteLog(string msg, string level = "INFO") => LogMessage?.Invoke(msg, level);

    #endregion

    #region ── INITIALISATION  ─────────────────────────────────────────
    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                WriteLog("🔧 Initializing PowerShell environment…", "INFO");

                var iss = InitialSessionState.CreateDefault();
                _runspace = RunspaceFactory.CreateRunspace(iss);
                _runspace.Open();
                WriteLog("✅ PowerShell run-space opened", "INFO");

                // check PowerCLI
                WriteLog("🔍 Checking for VMware PowerCLI…", "INFO");
                var result = ExecuteCommand("Get-Module -Name VMware.PowerCLI -ListAvailable");
                IsPowerCLIAvailable = !string.IsNullOrEmpty(result);
                if (IsPowerCLIAvailable)
                {
                    WriteLog("✅ PowerCLI detected – importing / configuring", "INFO");

                    ExecuteCommand("Import-Module VMware.PowerCLI -Force");
                    ExecuteCommand("Set-PowerCLIConfiguration -InvalidCertificateAction Ignore -Confirm:$false");
                    ExecuteCommand("Set-PowerCLIConfiguration -Scope User -ParticipateInCEIP $false -Confirm:$false");
                    ExecuteCommand("Set-PowerCLIConfiguration -DefaultVIServerMode Multiple -Confirm:$false");

                    // load host-config module if present
                    EnsureVmHostModuleImported();
                }
                else
                {
                    WriteLog("⚠️ PowerCLI not available – running in simulation mode", "WARNING");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"❌ Failed to init PS environment: {ex.Message}", "ERROR");
                throw;
            }
        });
    }
    #endregion

    #region ── MODULE LOADER (NEW) ─────────────────────────────────────
    private void EnsureVmHostModuleImported()
    {
        if (_vmHostModuleLoaded) return;

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var moduleDir = Path.Combine(baseDir, "Scripts", "VMHostConfig");
        var psm1Path = Path.Combine(moduleDir, "VMHostConfig.psm1");

        if (!File.Exists(psm1Path))
        {
            WriteLog($"⚠️ VMHostConfig module not found at {psm1Path}", "WARNING");
            return;                 // silently ignore – simulation will be used
        }

        ExecuteCommand($"Import-Module '{psm1Path.Replace("'", "''")}' -Force");
        WriteLog($"📦 Imported VMHostConfig module ({psm1Path})", "INFO");
        _vmHostModuleLoaded = true;
    }
    #endregion

    #region ── helper: build PSCredential from plain pwd (NEW) ─────────
    private static PSCredential ToPsCredential(string user, string plainPwd)
    {
        var ss = new SecureString();
        foreach (var ch in plainPwd) ss.AppendChar(ch);
        ss.MakeReadOnly();
        return new PSCredential(user, ss);
    }
    #endregion

    // ────────────────────────────────────────────────────────────────
    //  (All previously-existing connection / inventory / ESXi backup
    //   methods are kept 100 % unchanged – omitted here for brevity.)
    // ────────────────────────────────────────────────────────────────



    #region ── UPDATED *BACKUP* METHODS  ───────────────────────────────
    // USERS & GROUPS
    public async Task BackupUsersAndGroupsAsync(
        string backupPath,
        CancellationToken cancellationToken = default,
        Action<string>? progressCallback = null)
    {
        WriteLog("👥 Starting Users & Groups backup…", "INFO");
        progressCallback?.Invoke("Initializing Users and Groups backup…");

        // import module if possible
        EnsureVmHostModuleImported();

        string psCmd =
            IsPowerCLIAvailable && _vmHostModuleLoaded
            ? $"Backup-UsersAndGroups -ExportPath '{backupPath.Replace("'", "''")}'"
            : @"
                Write-Output 'PROGRESS:Creating simulated users/groups backup'
                $users  = @(@{ Name='administrator@vsphere.local' })
                $groups = @(@{ Name='Administrators' })
                $data = @{ Users=$users; Groups=$groups } | ConvertTo-Json -Depth 5
                $file = Join-Path '" + backupPath.Replace("'", "''") + @"' 'SSO_UsersAndGroups.json'
                $data | Out-File $file -Encoding utf8
                Write-Output ('Saved: ' + (Split-Path $file -Leaf))
              ";

        var result = await ExecuteCommandAsync(psCmd, cancellationToken);

        foreach (var line in result.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var t = line.Trim();
            if (t.StartsWith("PROGRESS:"))
                progressCallback?.Invoke(t[9..]);
            else
                WriteLog($"👥 {t}", "INFO");
        }
    }

    // ROLES
    public async Task BackupRolesAsync(string backupPath,
                                       CancellationToken cancellationToken = default,
                                       Action<string>? progressCallback = null)
    {
        WriteLog("🔐 Starting Roles backup…", "INFO");
        progressCallback?.Invoke("Initializing roles backup…");

        EnsureVmHostModuleImported();

        string psCmd =
            IsPowerCLIAvailable && _vmHostModuleLoaded
            ? $"Backup-Roles -ExportPath '{backupPath.Replace("'", "''")}'"
            : $"Write-Output 'PROGRESS:Simulated roles backup'; " +
              $"@{{Name='Administrator';Priv='*'}} | ConvertTo-Json | " +
              $"Out-File (Join-Path '{backupPath}' 'Administrator.json'); " +
              $"Write-Output 'Saved: Administrator.json'";

        var result = await ExecuteCommandAsync(psCmd, cancellationToken);
        foreach (var l in result.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var t = l.Trim();
            if (t.StartsWith("PROGRESS:")) progressCallback?.Invoke(t[9..]);
            else WriteLog($"🔐 {t}", "INFO");
        }
    }

    // PERMISSIONS
    public async Task BackupPermissionsAsync(string backupPath,
                                             CancellationToken cancellationToken = default,
                                             Action<string>? progressCallback = null)
    {
        WriteLog("🛡️  Starting Permissions backup…", "INFO");
        progressCallback?.Invoke("Initializing permissions backup…");

        EnsureVmHostModuleImported();

        string psCmd =
            IsPowerCLIAvailable && _vmHostModuleLoaded
            ? $"Backup-Permissions -ExportPath '{backupPath.Replace("'", "''")}'"
            : $"Write-Output 'PROGRESS:Simulated permissions backup'; " +
              $"@{{Principal='administrator@vsphere.local';Role='Admin'}} | " +
              $"ConvertTo-Json -Depth 5 | Out-File (Join-Path '{backupPath}' 'GlobalPermissions.json'); " +
              $"Write-Output 'Saved: GlobalPermissions.json'";

        var result = await ExecuteCommandAsync(psCmd, cancellationToken);
        foreach (var l in result.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var t = l.Trim();
            if (t.StartsWith("PROGRESS:")) progressCallback?.Invoke(t[9..]);
            else WriteLog($"🛡️ {t}", "INFO");
        }
    }
    #endregion



    #region ── ExecuteCommand / async / Dispose (unchanged) ────────────
    private Task<string> ExecuteCommandAsync(string command,
                                             CancellationToken token = default)
        => Task.Run(() => { token.ThrowIfCancellationRequested(); return ExecuteCommand(command); }, token);

    private string ExecuteCommand(string command)
    {
        if (_runspace is null || _runspace.RunspaceStateInfo.State != RunspaceState.Opened)
            throw new InvalidOperationException("Run-space not open");

        try
        {
            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.AddScript(command);
            var results = ps.Invoke();

            if (ps.Streams.Error.Any())
                throw new InvalidOperationException(ps.Streams.Error[0].ToString());

            return string.Join(Environment.NewLine, results);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"PowerShell failed: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        DisconnectAll();
        _runspace?.Dispose();
        GC.SuppressFinalize(this);
    }
    #endregion
}
