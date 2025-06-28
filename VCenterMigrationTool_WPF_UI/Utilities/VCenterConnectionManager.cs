using System;
using System.Management.Automation;
using System.Security;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    /// <summary>
    /// Manages vCenter connections.
    /// </summary>
    public class VCenterConnectionManager
    {
        private readonly PowerShellRunspaceManager _runspaceManager;

        public bool IsSourceConnected { get; private set; }
        public bool IsDestinationConnected { get; private set; }
        public bool IsPowerCLIAvailable { get; private set; }

        public event Action<string, string>? LogMessage;

        public VCenterConnectionManager(PowerShellRunspaceManager runspaceManager)
        {
            _runspaceManager = runspaceManager;
        }

        private void WriteLog(string message, string level = "INFO") => LogMessage?.Invoke(message, level);

        /// <summary>
        /// Initializes PowerCLI availability and configures settings.
        /// </summary>
        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    WriteLog("🔧 Initializing PowerShell environment...", "INFO");

                    // Check if VMware PowerCLI is available
                    var result = _runspaceManager.InvokeScript("Get-Module -Name VMware.PowerCLI -ListAvailable", new System.Collections.Generic.Dictionary<string, object>());
                    IsPowerCLIAvailable = result.Count > 0;

                    if (IsPowerCLIAvailable)
                    {
                        WriteLog("✅ PowerCLI detected, configuring settings...", "INFO");

                        _runspaceManager.InvokeScript("Import-Module VMware.PowerCLI -Force", new System.Collections.Generic.Dictionary<string, object>());
                        _runspaceManager.InvokeScript("Set-PowerCLIConfiguration -InvalidCertificateAction Ignore -Confirm:$false", new System.Collections.Generic.Dictionary<string, object>());
                        _runspaceManager.InvokeScript("Set-PowerCLIConfiguration -Scope User -ParticipateInCEIP $false -Confirm:$false", new System.Collections.Generic.Dictionary<string, object>());
                        _runspaceManager.InvokeScript("Set-PowerCLIConfiguration -DefaultVIServerMode Multiple -Confirm:$false", new System.Collections.Generic.Dictionary<string, object>());

                        WriteLog("✅ PowerCLI configured successfully", "INFO");
                    }
                    else
                    {
                        WriteLog("⚠️ PowerCLI not available - will run in simulation mode", "WARNING");
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"⚠️ PowerCLI check failed: {ex.Message} - will run in simulation mode", "WARNING");
                    IsPowerCLIAvailable = false;
                }
            });
        }

        /// <summary>
        /// Connects to source vCenter.
        /// </summary>
        public async Task<bool> ConnectToSourceVCenterAsync(string server, string username, string password)
        {
            return await ConnectAsync(server, username, password, isSource: true);
        }

        /// <summary>
        /// Connects to destination vCenter.
        /// </summary>
        public async Task<bool> ConnectToDestinationVCenterAsync(string server, string username, string password)
        {
            return await ConnectAsync(server, username, password, isSource: false);
        }

        private async Task<bool> ConnectAsync(string server, string username, string password, bool isSource)
        {
            try
            {
                WriteLog($"🔌 Attempting to connect to {(isSource ? "source" : "destination")} vCenter: {server}", "INFO");

                if (IsPowerCLIAvailable)
                {
                    var command = $@"
                        try {{
                            $global:{(isSource ? "SourceVIServer" : "DestVIServer")} = Connect-VIServer -Server '{server}' -User '{username}' -Password '{password}' -Force -ErrorAction Stop
                            Write-Output ""Connected to {server} successfully""
                            Write-Output ""Version: $($global:{(isSource ? "SourceVIServer" : "DestVIServer")}.Version)""
                            Write-Output ""Build: $($global:{(isSource ? "SourceVIServer" : "DestVIServer")}.Build)""
                        }} catch {{
                            Write-Error ""Connection failed: $($_.Exception.Message)""
                            throw
                        }}
                    ";

                    var result = await _runspaceManager.InvokeScriptAsync("-Command", new System.Collections.Generic.Dictionary<string, object> { { "Command", command } });

                    var output = string.Join(Environment.NewLine, result);
                    if (output.Contains("Connected to"))
                    {
                        if (isSource) IsSourceConnected = true; else IsDestinationConnected = true;

                        WriteLog($"✅ Successfully connected to {(isSource ? "source" : "destination")} vCenter: {server}", "INFO");

                        foreach (var line in output.Split('\n'))
                        {
                            if (line.Contains("Version:") || line.Contains("Build:"))
                                WriteLog($"ℹ️ {line.Trim()}", "INFO");
                        }
                    }
                    else
                    {
                        if (isSource) IsSourceConnected = false; else IsDestinationConnected = false;
                        WriteLog($"❌ Failed to connect to {(isSource ? "source" : "destination")} vCenter: {server}", "ERROR");
                    }
                }
                else
                {
                    WriteLog($"🎭 Simulating connection to {(isSource ? "source" : "destination")} vCenter: {server}", "INFO");
                    WriteLog(isSource ? "ℹ️ Version: 7.0.3 Build 19193900 (Simulated)" : "ℹ️ Version: 8.0.2 Build 22617221 (Simulated)", "INFO");
                    if (isSource) IsSourceConnected = true; else IsDestinationConnected = true;
                    await Task.Delay(1000);
                }

                return isSource ? IsSourceConnected : IsDestinationConnected;
            }
            catch (Exception ex)
            {
                WriteLog($"❌ {(isSource ? "Source" : "Destination")} vCenter connection error: {ex.Message}", "ERROR");
                if (isSource) IsSourceConnected = false; else IsDestinationConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Disconnects from all vCenter servers.
        /// </summary>
        /// 

        private async Task AutoLoadLastConnectionProfile()
        {
            //implementation here
        }
        private void LoadConnectionSettings(ConnectionSettings settings)
        {
            //implementation here
        }
        public async Task<string> GetVCenterVersionAsync(string target)
        {
            // target = "source" or "destination"
            string serverVar = target == "source" ? "$global:SourceVIServer" : "$global:DestVIServer";
            string script = $@"
            if ({serverVar} -and {serverVar}.IsConnected) {{
                ""$({serverVar}.Version) Build $({serverVar}.Build)""
            }} else {{
                'Not Connected'
            }}
            ";
            var results = await _runspaceManager.InvokeScriptAsync(
                "-Command",
                new Dictionary<string, object> { { "Command", script } }
            );
            return string.Join("\n", results.Select(r => r.ToString()));
        }
        public void DisconnectAll()
        {
            try
            {
                WriteLog("🔌 Disconnecting from all vCenter servers...", "INFO");

                if (IsPowerCLIAvailable)
                {
                    _runspaceManager.InvokeScript("if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) { Disconnect-VIServer -Server $global:SourceVIServer -Confirm:$false -Force }", new System.Collections.Generic.Dictionary<string, object>());
                    _runspaceManager.InvokeScript("if ($global:DestVIServer -and $global:DestVIServer.IsConnected) { Disconnect-VIServer -Server $global:DestVIServer -Confirm:$false -Force }", new System.Collections.Generic.Dictionary<string, object>());
                }

                WriteLog("✅ Disconnected from all vCenter servers", "INFO");
            }
            catch (Exception ex)
            {
                WriteLog($"⚠️ Error during disconnect: {ex.Message}", "WARNING");
            }

            IsSourceConnected = false;
            IsDestinationConnected = false;
        }
    }
}