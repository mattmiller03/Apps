using Markdig.Parsers;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;
using VCenterMigrationTool_WPF_UI.Models; // ← ADD THIS LINE

namespace VCenterMigrationTool_WPF_UI.Utilities;

public class PowerShellManager : IDisposable
{
    private Runspace? _runspace;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly char[] InvalidFileNameChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    public bool IsSourceConnected { get; private set; }
    public bool IsDestinationConnected { get; private set; }
    public bool IsPowerCLIAvailable { get; private set; }


    // Event for logging
    public event Action<string, string>? LogMessage;

    private void WriteLog(string message, string level = "INFO")
    {
        LogMessage?.Invoke(message, level);
    }

    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                WriteLog("🔧 Initializing PowerShell environment...", "INFO");

                var initialSessionState = InitialSessionState.CreateDefault();
                _runspace = RunspaceFactory.CreateRunspace(initialSessionState);
                _runspace.Open();

                WriteLog("✅ PowerShell runspace opened successfully", "INFO");

                // Check if VMware PowerCLI is available
                try
                {
                    WriteLog("🔍 Checking for VMware PowerCLI...", "INFO");
                    var result = ExecuteCommand("Get-Module -Name VMware.PowerCLI -ListAvailable");
                    IsPowerCLIAvailable = !string.IsNullOrEmpty(result);

                    if (IsPowerCLIAvailable)
                    {
                        WriteLog("✅ PowerCLI detected, configuring settings...", "INFO");

                        // Simulate a slight delay to show loading
                        System.Threading.Thread.Sleep(1000);

                        ExecuteCommand("Import-Module VMware.PowerCLI -Force");
                        ExecuteCommand("Set-PowerCLIConfiguration -InvalidCertificateAction Ignore -Confirm:$false");
                        ExecuteCommand("Set-PowerCLIConfiguration -Scope User -ParticipateInCEIP $false -Confirm:$false");
                        ExecuteCommand("Set-PowerCLIConfiguration -DefaultVIServerMode Multiple -Confirm:$false");

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
            }
            catch (Exception ex)
            {
                WriteLog($"❌ Failed to initialize PowerShell environment: {ex.Message}", "ERROR");
                throw new InvalidOperationException($"Failed to initialize PowerShell environment: {ex.Message}", ex);
            }
        });
    }

    public async Task<bool> ConnectToSourceVCenterAsync(string server, string username, string password)
    {
        try
        {
            WriteLog($"🔌 Attempting to connect to source vCenter: {server}", "INFO");

            if (IsPowerCLIAvailable)
            {
                var command = $@"
                    try {{
                        $global:SourceVIServer = Connect-VIServer -Server '{server}' -User '{username}' -Password '{password}' -Force -ErrorAction Stop
                        Write-Output ""Connected to {server} successfully""
                        Write-Output ""Version: $($global:SourceVIServer.Version)""
                        Write-Output ""Build: $($global:SourceVIServer.Build)""
                    }} catch {{
                        Write-Error ""Connection failed: $($_.Exception.Message)""
                        throw
                    }}
                ";

                var result = await ExecuteCommandAsync(command);

                if (result.Contains("Connected to"))
                {
                    IsSourceConnected = true;
                    WriteLog($"✅ Successfully connected to source vCenter: {server}", "INFO");

                    // Extract and log version info
                    var lines = result.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("Version:") || line.Contains("Build:"))
                        {
                            WriteLog($"ℹ️ {line.Trim()}", "INFO");
                        }
                    }
                }
                else
                {
                    IsSourceConnected = false;
                    WriteLog($"❌ Failed to connect to source vCenter: {server}", "ERROR");
                }
            }
            else
            {
                // Simulate connection
                WriteLog($"🎭 Simulating connection to source vCenter: {server}", "INFO");
                WriteLog("ℹ️ Version: 7.0.3 Build 19193900 (Simulated)", "INFO");
                IsSourceConnected = true;

                // Simulate a small delay
                await Task.Delay(1000);
            }

            return IsSourceConnected;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Source vCenter connection error: {ex.Message}", "ERROR");
            IsSourceConnected = false;
            return false;
        }
    }

    public async Task<bool> ConnectToDestinationVCenterAsync(string server, string username, string password)
    {
        try
        {
            WriteLog($"🔌 Attempting to connect to destination vCenter: {server}", "INFO");

            if (IsPowerCLIAvailable)
            {
                var command = $@"
                    try {{
                        $global:DestVIServer = Connect-VIServer -Server '{server}' -User '{username}' -Password '{password}' -Force -ErrorAction Stop
                        Write-Output ""Connected to {server} successfully""
                        Write-Output ""Version: $($global:DestVIServer.Version)""
                        Write-Output ""Build: $($global:DestVIServer.Build)""
                    }} catch {{
                        Write-Error ""Connection failed: $($_.Exception.Message)""
                        throw
                    }}
                ";

                var result = await ExecuteCommandAsync(command);

                if (result.Contains("Connected to"))
                {
                    IsDestinationConnected = true;
                    WriteLog($"✅ Successfully connected to destination vCenter: {server}", "INFO");

                    // Extract and log version info
                    var lines = result.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("Version:") || line.Contains("Build:"))
                        {
                            WriteLog($"ℹ️ {line.Trim()}", "INFO");
                        }
                    }
                }
                else
                {
                    IsDestinationConnected = false;
                    WriteLog($"❌ Failed to connect to destination vCenter: {server}", "ERROR");
                }
            }
            else
            {
                // Simulate connection
                WriteLog($"🎭 Simulating connection to destination vCenter: {server}", "INFO");
                WriteLog("ℹ️ Version: 8.0.2 Build 22617221 (Simulated)", "INFO");
                IsDestinationConnected = true;

                // Simulate a small delay
                await Task.Delay(1000);
            }

            return IsDestinationConnected;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Destination vCenter connection error: {ex.Message}", "ERROR");
            IsDestinationConnected = false;
            return false;
        }
    }

    public async Task<string> GetVCenterVersionAsync(string target)
    {
        try
        {
            if (IsPowerCLIAvailable)
            {
                var serverVar = target == "source" ? "$global:SourceVIServer" : "$global:DestVIServer";
                var command = $@"
                    if ({serverVar} -and {serverVar}.IsConnected) {{ 
                        ""$({serverVar}.Version) Build $({serverVar}.Build)""
                    }} else {{ 
                        'Not Connected' 
                    }}
                ";

                return await ExecuteCommandAsync(command);
            }
            else
            {
                return target == "source" ? "7.0.3 Build 19193900 (Simulated)" : "8.0.2 Build 22617221 (Simulated)";
            }
        }
        catch
        {
            return "Unknown";
        }
    }

    public async Task<string> GetInventoryAsync()
    {
        try
        {
            WriteLog("📋 Retrieving vCenter inventory...", "INFO");

            if (IsPowerCLIAvailable && IsSourceConnected)
            {
                var command = @"
                    try {
                        $inventory = @()
                        Get-Datacenter -Server $global:SourceVIServer | ForEach-Object {
                            $inventory += ""DC:$($_.Name)""
                            
                            Get-Cluster -Location $_ -Server $global:SourceVIServer -ErrorAction SilentlyContinue | ForEach-Object {
                                $inventory += ""CLUSTER:$($_.Name)""
                                
                                Get-VMHost -Location $_ -Server $global:SourceVIServer -ErrorAction SilentlyContinue | ForEach-Object {
                                    $inventory += ""HOST:$($_.Name) ($($_.ConnectionState))""
                                    
                                    Get-VM -Location $_ -Server $global:SourceVIServer -ErrorAction SilentlyContinue | ForEach-Object {
                                        $inventory += ""VM:$($_.Name) ($($_.PowerState))""
                                    }
                                }
                            }
                        }
                        $inventory -join ""`n""
                    } catch {
                        Write-Error ""Failed to get inventory: $($_.Exception.Message)""
                        throw
                    }
                ";

                var result = await ExecuteCommandAsync(command);
                WriteLog("✅ Inventory retrieved successfully", "INFO");
                return result;
            }
            else
            {
                WriteLog("🎭 Generating simulated inventory", "INFO");
                return @"DC:Datacenter1
CLUSTER:Production-Cluster
HOST:esxi01.domain.local (Connected)
VM:WebServer01 (PoweredOn)
VM:Database01 (PoweredOn)
HOST:esxi02.domain.local (Connected)
VM:FileServer01 (PoweredOn)
VM:TestVM01 (PoweredOff)
CLUSTER:Development-Cluster
HOST:esxi03.domain.local (Connected)
VM:DevVM01 (PoweredOn)
VM:DevVM02 (PoweredOff)";
            }
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Error retrieving inventory: {ex.Message}", "ERROR");
            return $"Error: {ex.Message}";
        }
    }

    #region All Backup Methods with Enhanced VDS Support

    public async Task BackupVDSAsync(string backupPath, CancellationToken cancellationToken = default, Action<string>? progressCallback = null)
    {
        WriteLog("📡 Starting optimized VDS backup...", "INFO");
        progressCallback?.Invoke("Initializing VDS backup process...");

        var command = $@"
        try {{
            $vdsPath = Join-Path '{backupPath}' 'VDS'
            New-Item -Path $vdsPath -ItemType Directory -Force | Out-Null
            Write-Output ""PROGRESS:Created VDS backup directory""
            
            if (Get-Module -Name VMware.PowerCLI -ListAvailable -ErrorAction SilentlyContinue) {{
                if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) {{
                    Write-Output ""PROGRESS:Discovering VDS switches...""
                    $vdSwitches = Get-VDSwitch -Server $global:SourceVIServer -ErrorAction SilentlyContinue
                    Write-Output ""PROGRESS:Found $($vdSwitches.Count) VDS switches to backup""
                    
                    $switchIndex = 0
                    foreach ($vdsSwitch in $vdSwitches) {{
                        $switchIndex++
                        Write-Output ""PROGRESS:Processing VDS $switchIndex of $($vdSwitches.Count): $($vdsSwitch.Name)""
                        
                        # Basic VDS configuration (optimized for speed)
                        Write-Output ""PROGRESS:Collecting basic configuration for $($vdsSwitch.Name)...""
                        $vdsConfig = @{{
                            Name = $vdsSwitch.Name
                            NumUplinkPorts = $vdsSwitch.NumUplinkPorts
                            Mtu = $vdsSwitch.Mtu
                            Version = $vdsSwitch.Version
                            Notes = $vdsSwitch.Notes
                            MaxPorts = $vdsSwitch.MaxPorts
                            NumPorts = $vdsSwitch.NumPorts
                            Vendor = $vdsSwitch.Vendor
                            PortGroups = @()
                            VMKernelAdapters = @()
                            PhysicalNicUplinks = @()
                        }}
                        
                        # Get port groups (simplified for speed)
                        Write-Output ""PROGRESS:Collecting port groups for $($vdsSwitch.Name)...""
                        try {{
                            $portGroups = Get-VDPortgroup -VDSwitch $vdsSwitch -ErrorAction SilentlyContinue
                            Write-Output ""PROGRESS:Found $($portGroups.Count) port groups on $($vdsSwitch.Name)""
                            
                            foreach ($pg in $portGroups) {{
                                Write-Output ""PROGRESS:Processing port group: $($pg.Name)""
                                $pgConfig = @{{
                                    Name = $pg.Name
                                    VlanId = if ($pg.VlanConfiguration) {{ $pg.VlanConfiguration.VlanId }} else {{ 0 }}
                                    PortBinding = $pg.PortBinding.ToString()
                                    NumPorts = $pg.NumPorts
                                    Notes = $pg.Notes
                                    IsUplink = $pg.IsUplink
                                }}
                                $vdsConfig.PortGroups += $pgConfig
                            }}
                        }} catch {{
                            Write-Output ""PROGRESS:Warning: Could not retrieve port groups for $($vdsSwitch.Name)""
                        }}
                        
                        # Get VMkernel adapters (simplified)
                        Write-Output ""PROGRESS:Collecting VMkernel adapters for $($vdsSwitch.Name)...""
                        try {{
                            $vmkAdapters = Get-VMHostNetworkAdapter -VMKernel -Server $global:SourceVIServer -ErrorAction SilentlyContinue | Where-Object {{ $_.PortGroupName -in $vdsConfig.PortGroups.Name }} | Select-Object -First 10
                            Write-Output ""PROGRESS:Found $($vmkAdapters.Count) VMkernel adapters on $($vdsSwitch.Name)""
                            
                            foreach ($vmk in $vmkAdapters) {{
                                Write-Output ""PROGRESS:Processing VMkernel adapter: $($vmk.Name)""
                                $vmkConfig = @{{
                                    Name = $vmk.Name
                                    VMHostName = $vmk.VMHost.Name
                                    PortGroupName = $vmk.PortGroupName
                                    IP = $vmk.IP
                                    SubnetMask = $vmk.SubnetMask
                                    VMotionEnabled = $vmk.VMotionEnabled
                                    ManagementTrafficEnabled = $vmk.ManagementTrafficEnabled
                                }}
                                $vdsConfig.VMKernelAdapters += $vmkConfig
                            }}
                        }} catch {{
                            Write-Output ""PROGRESS:Warning: Could not retrieve VMkernel adapters for $($vdsSwitch.Name)""
                        }}
                        
                        # Get physical NIC uplinks (limited for speed)
                        Write-Output ""PROGRESS:Collecting physical NIC uplinks for $($vdsSwitch.Name)...""
                        try {{
                            $vmHosts = Get-VMHost -Server $global:SourceVIServer -ErrorAction SilentlyContinue | Select-Object -First 3
                            foreach ($vmHostItem in $vmHosts) {{
                                Write-Output ""PROGRESS:Processing host: $($vmHostItem.Name)""
                                $physicalNics = Get-VMHostNetworkAdapter -Physical -VMHost $vmHostItem -ErrorAction SilentlyContinue | Select-Object -First 2
                                foreach ($pnic in $physicalNics) {{
                                    $pnicConfig = @{{
                                        VMHostName = $vmHostItem.Name
                                        PhysicalNicName = $pnic.Name
                                        Mac = $pnic.Mac
                                        LinkSpeedMb = $pnic.LinkSpeedMb
                                    }}
                                    $vdsConfig.PhysicalNicUplinks += $pnicConfig
                                }}
                            }}
                        }} catch {{
                            Write-Output ""PROGRESS:Warning: Could not retrieve physical NIC uplinks for $($vdsSwitch.Name)""
                        }}
                        
                        # Save configuration
                        Write-Output ""PROGRESS:Saving configuration for $($vdsSwitch.Name)...""
                        $safeFileName = $vdsSwitch.Name -replace '[\\/:*?""""<>|]', '_'
                        $configFile = Join-Path $vdsPath ""$safeFileName.json""
                        $vdsConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                        Write-Output ""PROGRESS:Completed VDS $switchIndex of $($vdSwitches.Count): $($vdsSwitch.Name)""
                    }}
                    
                    Write-Output ""PROGRESS:VDS backup completed successfully""
                    Write-Output ""VDS backup completed - $($vdSwitches.Count) switches backed up""
                }} else {{
                    Write-Output ""PROGRESS:Creating simulated VDS backup (no vCenter connection)""
                    # Fast simulated VDS
                    $vdsConfig = @{{
                        Name = 'vDS-Production'
                        NumUplinkPorts = 4
                        Mtu = 1500
                        Version = '7.0.0'
                        PortGroups = @(
                            @{{ Name = 'Management Network'; VlanId = 100; PortBinding = 'Static'; NumPorts = 128 }},
                            @{{ Name = 'vMotion Network'; VlanId = 200; PortBinding = 'Static'; NumPorts = 64 }},
                            @{{ Name = 'VM Network'; VlanId = 0; PortBinding = 'Static'; NumPorts = 256 }}
                        )
                        VMKernelAdapters = @(
                            @{{ Name = 'vmk0'; VMHostName = 'esxi01.domain.local'; PortGroupName = 'Management Network'; IP = '192.168.100.10' }},
                            @{{ Name = 'vmk1'; VMHostName = 'esxi01.domain.local'; PortGroupName = 'vMotion Network'; IP = '192.168.200.10' }}
                        )
                        PhysicalNicUplinks = @(
                            @{{ VMHostName = 'esxi01.domain.local'; PhysicalNicName = 'vmnic0'; Mac = '00:50:56:78:90:12'; LinkSpeedMb = 10000 }},
                            @{{ VMHostName = 'esxi01.domain.local'; PhysicalNicName = 'vmnic1'; Mac = '00:50:56:78:90:13'; LinkSpeedMb = 10000 }}
                        )
                    }}
                    
                    $configFile = Join-Path $vdsPath ""vDS-Production.json""
                    $vdsConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                    Write-Output ""PROGRESS:Saved simulated VDS configuration""
                    Write-Output ""VDS backup completed - 1 simulated switch backed up""
                }}
            }} else {{
                Write-Output ""PROGRESS:Creating simulated VDS backup (PowerCLI not available)""
                # Same fast simulated configuration
                $vdsConfig = @{{
                    Name = 'vDS-Production'
                    NumUplinkPorts = 4
                    Mtu = 1500
                    Version = '7.0.0'
                    PortGroups = @(
                        @{{ Name = 'Management Network'; VlanId = 100 }},
                        @{{ Name = 'vMotion Network'; VlanId = 200 }},
                        @{{ Name = 'VM Network'; VlanId = 0 }}
                    )
                }}
                
                $configFile = Join-Path $vdsPath ""vDS-Production.json""
                $vdsConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                Write-Output ""PROGRESS:Saved simulated VDS configuration""
                Write-Output ""VDS backup completed - 1 simulated switch backed up""
            }}
        }} catch {{
            Write-Error ""VDS backup failed: $($_.Exception.Message)""
            throw
        }}
    ";

        try
        {
            var result = await ExecuteCommandAsync(command, cancellationToken);
            var lines = result.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmedLine = line.Trim();

                // Check for progress updates
                if (trimmedLine.StartsWith("PROGRESS:"))
                {
                    var progressText = trimmedLine[9..]; // Remove "PROGRESS:" prefix
                    progressCallback?.Invoke(progressText);
                }
                else
                {
                    WriteLog($"📡 {trimmedLine}", "INFO");
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 VDS backup was cancelled", "WARNING");
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ VDS backup failed: {ex.Message}", "ERROR");
            throw;
        }
    }

    public async Task BackupUsersAndGroupsAsync(string backupPath, CancellationToken cancellationToken = default, Action<string>? progressCallback = null)
    {
        WriteLog("👥 Starting Users and Groups backup...", "INFO");
        progressCallback?.Invoke("Initializing Users and Groups backup...");

        var command = $@"
            try {{
                $usersPath = Join-Path '{backupPath}' 'UsersAndGroups'
                New-Item -Path $usersPath -ItemType Directory -Force | Out-Null
                Write-Output ""Created Users and Groups backup directory""
                
                Write-Output ""PROGRESS:Simulating users and groups backup (SSO cmdlets require special permissions)""
                
                $simulatedUsers = @(
                    @{{
                        Name = 'administrator@vsphere.local'
                        Description = 'Built-in administrator'
                        EmailAddress = 'admin@company.com'
                        FirstName = 'System'
                        LastName = 'Administrator'
                        Disabled = $false
                        Locked = $false
                    }}
                )
                
                $simulatedGroups = @(
                    @{{
                        Name = 'Administrators'
                        Description = 'System administrators'
                        Domain = 'vsphere.local'
                    }}
                )
                
                Write-Output ""PROGRESS:Saving SSO users configuration...""
                $configFile = Join-Path $usersPath ""SSOUsers.json""
                $simulatedUsers | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                Write-Output ""Saved: SSOUsers.json""
                
                Write-Output ""PROGRESS:Saving SSO groups configuration...""
                $configFile = Join-Path $usersPath ""SSOGroups.json""
                $simulatedGroups | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                Write-Output ""Saved: SSOGroups.json""
                
                Write-Output ""Users and Groups backup completed""
            }} catch {{
                Write-Error ""Users and Groups backup failed: $($_.Exception.Message)""
                throw
            }}
        ";

        try
        {
            var result = await ExecuteCommandAsync(command, cancellationToken);
            var lines = result.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("PROGRESS:"))
                {
                    var progressText = trimmedLine[9..];
                    progressCallback?.Invoke(progressText);
                }
                else
                {
                    WriteLog($"👥 {trimmedLine}", "INFO");
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 Users and Groups backup was cancelled", "WARNING");
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Users and Groups backup failed: {ex.Message}", "ERROR");
            throw;
        }
    }

    public async Task BackupRolesAsync(string backupPath, CancellationToken cancellationToken = default, Action<string>? progressCallback = null)
    {
        WriteLog("🔐 Starting Roles backup...", "INFO");
        progressCallback?.Invoke("Initializing Roles backup...");

        var command = $@"
            try {{
                $rolesPath = Join-Path '{backupPath}' 'Roles'
                New-Item -Path $rolesPath -ItemType Directory -Force | Out-Null
                Write-Output ""Created Roles backup directory""
                
                if (Get-Module -Name VMware.PowerCLI -ListAvailable -ErrorAction SilentlyContinue) {{
                    if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) {{
                        Write-Output ""PROGRESS:Discovering vCenter roles...""
                        $viRoles = Get-VIRole -Server $global:SourceVIServer -ErrorAction SilentlyContinue
                        Write-Output ""Found $($viRoles.Count) roles""
                        
                        $roleIndex = 0
                        foreach ($viRole in $viRoles) {{
                            $roleIndex++
                            Write-Output ""PROGRESS:Processing role $roleIndex of $($viRoles.Count): $($viRole.Name)""
                            Write-Output ""Backing up role: $($viRole.Name)""
                            
                            $roleConfig = @{{
                                Name = $viRole.Name
                                Description = $viRole.Description
                                PrivilegeList = $viRole.PrivilegeList
                                IsSystem = $viRole.IsSystem
                                Server = $viRole.Server.Name
                            }}
                            
                            $safeFileName = $viRole.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $rolesPath ""$safeFileName.json""
                            $roleConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json""
                        }}
                        
                        Write-Output ""Roles backup completed - $($viRoles.Count) roles backed up""
                    }} else {{
                        Write-Output ""PROGRESS:Creating simulated roles backup (no vCenter connection)""
                        $simulatedRoles = @(
                            @{{
                                Name = 'Administrator'
                                Description = 'Full administrative privileges'
                                PrivilegeList = @('System.Anonymous', 'System.View', 'System.Read')
                                IsSystem = $true
                            }},
                            @{{
                                Name = 'ReadOnly'
                                Description = 'Read-only access'
                                PrivilegeList = @('System.Anonymous', 'System.View', 'System.Read')
                                IsSystem = $true
                            }}
                        )
                        
                        foreach ($simulatedRole in $simulatedRoles) {{
                            $configFile = Join-Path $rolesPath ""$($simulatedRole.Name).json""
                            $simulatedRole | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $($simulatedRole.Name).json (simulated)""
                        }}
                        
                        Write-Output ""Roles backup completed - $($simulatedRoles.Count) simulated roles backed up""
                    }}
                }} else {{
                    Write-Output ""PROGRESS:Creating simulated roles backup (PowerCLI not available)""
                    $simulatedRoles = @(
                        @{{
                            Name = 'Administrator'
                            Description = 'Full administrative privileges'
                            PrivilegeList = @('System.Anonymous', 'System.View', 'System.Read')
                            IsSystem = $true
                        }},
                        @{{
                            Name = 'ReadOnly'
                            Description = 'Read-only access'
                            PrivilegeList = @('System.Anonymous', 'System.View', 'System.Read')
                            IsSystem = $true
                        }}
                    )
                    
                    foreach ($simulatedRole in $simulatedRoles) {{
                        $configFile = Join-Path $rolesPath ""$($simulatedRole.Name).json""
                        $simulatedRole | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                        Write-Output ""Saved: $($simulatedRole.Name).json (simulated)""
                    }}
                    
                    Write-Output ""Roles backup completed - $($simulatedRoles.Count) simulated roles backed up""
                }}
            }} catch {{
                Write-Error ""Roles backup failed: $($_.Exception.Message)""
                throw
            }}
        ";

        try
        {
            var result = await ExecuteCommandAsync(command, cancellationToken);
            var lines = result.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("PROGRESS:"))
                {
                    var progressText = trimmedLine[9..];
                    progressCallback?.Invoke(progressText);
                }
                else
                {
                    WriteLog($"🔐 {trimmedLine}", "INFO");
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 Roles backup was cancelled", "WARNING");
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Roles backup failed: {ex.Message}", "ERROR");
            throw;
        }
    }

    public async Task BackupPermissionsAsync(string backupPath, CancellationToken cancellationToken = default, Action<string>? progressCallback = null)
    {
        WriteLog("🛡️ Starting Permissions backup...", "INFO");
        progressCallback?.Invoke("Initializing Permissions backup...");

        var command = $@"
            try {{
                $permissionsPath = Join-Path '{backupPath}' 'Permissions'
                New-Item -Path $permissionsPath -ItemType Directory -Force | Out-Null
                Write-Output ""Created Permissions backup directory""
                
                if (Get-Module -Name VMware.PowerCLI -ListAvailable -ErrorAction SilentlyContinue) {{
                    if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) {{
                        Write-Output ""PROGRESS:Discovering vCenter permissions...""
                        $viPermissions = Get-VIPermission -Server $global:SourceVIServer -ErrorAction SilentlyContinue
                        $permissionsConfig = @()
                        Write-Output ""Found $($viPermissions.Count) permissions""
                        
                        $permIndex = 0
                        foreach ($viPermission in $viPermissions) {{
                            $permIndex++
                            Write-Output ""PROGRESS:Processing permission $permIndex of $($viPermissions.Count): $($viPermission.Principal)""
                            Write-Output ""Backing up permission for: $($viPermission.Principal)""
                            $permissionsConfig += @{{
                                Principal = $viPermission.Principal
                                Role = $viPermission.Role
                                Entity = $viPermission.Entity.Name
                                EntityType = $viPermission.Entity.GetType().Name
                                IsGroup = $viPermission.IsGroup
                                Propagate = $viPermission.Propagate
                            }}
                        }}
                        
                        Write-Output ""PROGRESS:Saving permissions configuration...""
                        $configFile = Join-Path $permissionsPath ""GlobalPermissions.json""
                        $permissionsConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                        Write-Output ""Saved: GlobalPermissions.json""
                        
                        Write-Output ""Permissions backup completed - $($viPermissions.Count) permissions backed up""
                    }} else {{
                        Write-Output ""PROGRESS:Creating simulated permissions backup (no vCenter connection)""
                        $simulatedPermissions = @(
                            @{{
                                Principal = 'administrator@vsphere.local'
                                Role = 'Admin'
                                Entity = 'vcenter.company.local'
                                EntityType = 'VirtualCenter'
                                IsGroup = $false
                                Propagate = $true
                            }}
                        )
                        
                        $configFile = Join-Path $permissionsPath ""GlobalPermissions.json""
                        $simulatedPermissions | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                        Write-Output ""Saved: GlobalPermissions.json (simulated)""
                        
                        Write-Output ""Permissions backup completed - $($simulatedPermissions.Count) simulated permissions backed up""
                    }}
                }} else {{
                    Write-Output ""PROGRESS:Creating simulated permissions backup (PowerCLI not available)""
                    $simulatedPermissions = @(
                        @{{
                            Principal = 'administrator@vsphere.local'
                            Role = 'Admin'
                            Entity = 'vcenter.company.local'
                            EntityType = 'VirtualCenter'
                            IsGroup = $false
                            Propagate = $true
                        }}
                    )
                    
                    $configFile = Join-Path $permissionsPath ""GlobalPermissions.json""
                    $simulatedPermissions | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                    Write-Output ""Saved: GlobalPermissions.json (simulated)""
                    
                    Write-Output ""Permissions backup completed - $($simulatedPermissions.Count) simulated permissions backed up""
                }}
            }} catch {{
                Write-Error ""Permissions backup failed: $($_.Exception.Message)""
                throw
            }}
        ";

        try
        {
            var result = await ExecuteCommandAsync(command, cancellationToken);
            var lines = result.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("PROGRESS:"))
                {
                    var progressText = trimmedLine[9..];
                    progressCallback?.Invoke(progressText);
                }
                else
                {
                    WriteLog($"🛡️ {trimmedLine}", "INFO");
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 Permissions backup was cancelled", "WARNING");
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Permissions backup failed: {ex.Message}", "ERROR");
            throw;
        }
    }

    public async Task BackupHostConfigurationsAsync(string backupPath, CancellationToken cancellationToken = default, Action<string>? progressCallback = null)
    {
        WriteLog("🖥️ Starting Host configurations backup...", "INFO");
        progressCallback?.Invoke("Initializing Host configurations backup...");

        var command = $@"
            try {{
                $hostsPath = Join-Path '{backupPath}' 'Hosts'
                New-Item -Path $hostsPath -ItemType Directory -Force | Out-Null
                Write-Output ""Created Hosts backup directory""
                
                if (Get-Module -Name VMware.PowerCLI -ListAvailable -ErrorAction SilentlyContinue) {{
                    if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) {{
                        Write-Output ""PROGRESS:Discovering ESXi hosts...""
                        $vmHosts = Get-VMHost -Server $global:SourceVIServer -ErrorAction SilentlyContinue
                        Write-Output ""Found $($vmHosts.Count) ESXi hosts""
                        
                        $hostIndex = 0
                        foreach ($vmHostItem in $vmHosts) {{
                            $hostIndex++
                            Write-Output ""PROGRESS:Processing host $hostIndex of $($vmHosts.Count): $($vmHostItem.Name)""
                            Write-Output ""Backing up host: $($vmHostItem.Name)""
                            
                            $hostConfig = @{{
                                Name = $vmHostItem.Name
                                Version = $vmHostItem.Version
                                Build = $vmHostItem.Build
                                ConnectionState = $vmHostItem.ConnectionState.ToString()
                                PowerState = $vmHostItem.PowerState.ToString()
                                Manufacturer = $vmHostItem.Manufacturer
                                Model = $vmHostItem.Model
                            }}
                            
                            $safeFileName = $vmHostItem.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $hostsPath ""$safeFileName.json""
                            $hostConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json""
                        }}
                        
                        Write-Output ""Host configurations backup completed - $($vmHosts.Count) hosts backed up""
                    }} else {{
                        Write-Output ""PROGRESS:Creating simulated host backup (no vCenter connection)""
                        $simulatedHosts = @('esxi01.domain.local', 'esxi02.domain.local')
                        
                        foreach ($hostName in $simulatedHosts) {{
                            $hostConfig = @{{
                                Name = $hostName
                                Version = '7.0.3'
                                Build = '19193900'
                                ConnectionState = 'Connected'
                                PowerState = 'PoweredOn'
                                Manufacturer = 'Dell Inc.'
                                Model = 'PowerEdge R640'
                            }}
                            
                            $safeFileName = $hostName -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $hostsPath ""$safeFileName.json""
                            $hostConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json (simulated)""
                        }}
                        
                        Write-Output ""Host configurations backup completed - $($simulatedHosts.Count) simulated hosts backed up""
                    }}
                }} else {{
                    Write-Output ""PROGRESS:Creating simulated host backup (PowerCLI not available)""
                    $simulatedHosts = @('esxi01.domain.local', 'esxi02.domain.local')
                    
                    foreach ($hostName in $simulatedHosts) {{
                        $hostConfig = @{{
                            Name = $hostName
                            Version = '7.0.3'
                            Build = '19193900'
                            ConnectionState = 'Connected'
                            PowerState = 'PoweredOn'
                            Manufacturer = 'Dell Inc.'
                            Model = 'PowerEdge R640'
                        }}
                        
                        $safeFileName = $hostName -replace '[\\/:*?""""<>|]', '_'
                        $configFile = Join-Path $hostsPath ""$safeFileName.json""
                        $hostConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                        Write-Output ""Saved: $safeFileName.json (simulated)""
                    }}
                    
                    Write-Output ""Host configurations backup completed - $($simulatedHosts.Count) simulated hosts backed up""
                }}
            }} catch {{
                Write-Error ""Host configurations backup failed: $($_.Exception.Message)""
                throw
            }}
        ";

        try
        {
            var result = await ExecuteCommandAsync(command, cancellationToken);
            var lines = result.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("PROGRESS:"))
                {
                    var progressText = trimmedLine[9..];
                    progressCallback?.Invoke(progressText);
                }
                else
                {
                    WriteLog($"🖥️ {trimmedLine}", "INFO");
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 Host configurations backup was cancelled", "WARNING");
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Host configurations backup failed: {ex.Message}", "ERROR");
            throw;
        }
    }

    public async Task BackupVMConfigurationsAsync(string backupPath, CancellationToken cancellationToken = default, Action<string>? progressCallback = null)
    {
        WriteLog("💻 Starting VM configurations backup...", "INFO");
        progressCallback?.Invoke("Initializing VM configurations backup...");

        var command = $@"
            try {{
                $vmsPath = Join-Path '{backupPath}' 'VMs'
                New-Item -Path $vmsPath -ItemType Directory -Force | Out-Null
                Write-Output ""Created VMs backup directory""
                
                if (Get-Module -Name VMware.PowerCLI -ListAvailable -ErrorAction SilentlyContinue) {{
                    if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) {{
                        Write-Output ""PROGRESS:Discovering virtual machines...""
                        $virtualMachines = Get-VM -Server $global:SourceVIServer -ErrorAction SilentlyContinue
                        Write-Output ""Found $($virtualMachines.Count) virtual machines""
                        
                        $vmIndex = 0
                        foreach ($virtualMachine in $virtualMachines) {{
                            $vmIndex++
                            Write-Output ""PROGRESS:Processing VM $vmIndex of $($virtualMachines.Count): $($virtualMachine.Name)""
                            Write-Output ""Backing up VM: $($virtualMachine.Name)""
                            
                            $vmConfig = @{{
                                Name = $virtualMachine.Name
                                PowerState = $virtualMachine.PowerState.ToString()
                                NumCpu = $virtualMachine.NumCpu
                                CoresPerSocket = $virtualMachine.CoresPerSocket
                                MemoryGB = $virtualMachine.MemoryGB
                                MemoryMB = $virtualMachine.MemoryMB
                                Version = $virtualMachine.Version
                                HardwareVersion = $virtualMachine.HardwareVersion
                                GuestId = $virtualMachine.GuestId
                                Notes = $virtualMachine.Notes
                                Folder = $virtualMachine.Folder.Name
                                ResourcePool = $virtualMachine.ResourcePool.Name
                                VMHost = $virtualMachine.VMHost.Name
                                CreateDate = $virtualMachine.CreateDate
                            }}
                            
                            $safeFileName = $virtualMachine.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $vmsPath ""$safeFileName.json""
                            $vmConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json""
                        }}
                        
                        Write-Output ""VM configurations backup completed - $($virtualMachines.Count) VMs backed up""
                    }} else {{
                        Write-Output ""PROGRESS:Creating simulated VM backup (no vCenter connection)""
                        $simulatedVMs = @(
                            @{{
                                Name = 'WebServer01'
                                PowerState = 'PoweredOn'
                                NumCpu = 2
                                CoresPerSocket = 1
                                MemoryGB = 4
                                MemoryMB = 4096
                                Version = 'v19'
                                HardwareVersion = 'vmx-19'
                                GuestId = 'windows2019srv_64Guest'
                                Notes = 'Production web server'
                                Folder = 'Production'
                                ResourcePool = 'Resources'
                                VMHost = 'esxi01.domain.local'
                                CreateDate = '2023-01-15T10:30:00'
                            }},
                            @{{
                                Name = 'Database01'
                                PowerState = 'PoweredOn'
                                NumCpu = 4
                                CoresPerSocket = 2
                                MemoryGB = 8
                                MemoryMB = 8192
                                Version = 'v19'
                                HardwareVersion = 'vmx-19'
                                GuestId = 'windows2019srv_64Guest'
                                Notes = 'Production database server'
                                Folder = 'Production'
                                ResourcePool = 'Resources'
                                VMHost = 'esxi01.domain.local'
                                CreateDate = '2023-01-20T14:15:00'
                            }}
                        )
                        
                        foreach ($simulatedVM in $simulatedVMs) {{
                            $safeFileName = $simulatedVM.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $vmsPath ""$safeFileName.json""
                            $simulatedVM | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json (simulated)""
                        }}
                        
                        Write-Output ""VM configurations backup completed - $($simulatedVMs.Count) simulated VMs backed up""
                    }}
                }} else {{
                    Write-Output ""PROGRESS:Creating simulated VM backup (PowerCLI not available)""
                    $simulatedVMs = @(
                        @{{
                            Name = 'WebServer01'
                            PowerState = 'PoweredOn'
                            NumCpu = 2
                            CoresPerSocket = 1
                            MemoryGB = 4
                            MemoryMB = 4096
                            Version = 'v19'
                            HardwareVersion = 'vmx-19'
                            GuestId = 'windows2019srv_64Guest'
                            Notes = 'Production web server'
                            Folder = 'Production'
                            ResourcePool = 'Resources'
                            VMHost = 'esxi01.domain.local'
                            CreateDate = '2023-01-15T10:30:00'
                        }},
                        @{{
                            Name = 'Database01'
                            PowerState = 'PoweredOn'
                            NumCpu = 4
                            CoresPerSocket = 2
                            MemoryGB = 8
                            MemoryMB = 8192
                            Version = 'v19'
                            HardwareVersion = 'vmx-19'
                            GuestId = 'windows2019srv_64Guest'
                            Notes = 'Production database server'
                            Folder = 'Production'
                            ResourcePool = 'Resources'
                            VMHost = 'esxi01.domain.local'
                            CreateDate = '2023-01-20T14:15:00'
                        }}
                    )
                    
                    foreach ($simulatedVM in $simulatedVMs) {{
                        $safeFileName = $simulatedVM.Name -replace '[\\/:*?""""<>|]', '_'
                        $configFile = Join-Path $vmsPath ""$safeFileName.json""
                        $simulatedVM | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                        Write-Output ""Saved: $safeFileName.json (simulated)""
                    }}
                    
                    Write-Output ""VM configurations backup completed - $($simulatedVMs.Count) simulated VMs backed up""
                }}
            }} catch {{
                Write-Error ""VM configurations backup failed: $($_.Exception.Message)""
                throw
            }}
        ";

        try
        {
            var result = await ExecuteCommandAsync(command, cancellationToken);
            var lines = result.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("PROGRESS:"))
                {
                    var progressText = trimmedLine[9..];
                    progressCallback?.Invoke(progressText);
                }
                else
                {
                    WriteLog($"💻 {trimmedLine}", "INFO");
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 VM configurations backup was cancelled", "WARNING");
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ VM configurations backup failed: {ex.Message}", "ERROR");
            throw;
        }
    }

    public async Task BackupClusterConfigurationsAsync(string backupPath, CancellationToken cancellationToken = default, Action<string>? progressCallback = null)
    {
        WriteLog("🏢 Starting Cluster configurations backup...", "INFO");
        progressCallback?.Invoke("Initializing Cluster configurations backup...");

        var command = $@"
            try {{
                $clustersPath = Join-Path '{backupPath}' 'Clusters'
                New-Item -Path $clustersPath -ItemType Directory -Force | Out-Null
                Write-Output ""Created Clusters backup directory""
                
                if (Get-Module -Name VMware.PowerCLI -ListAvailable -ErrorAction SilentlyContinue) {{
                    if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) {{
                        Write-Output ""PROGRESS:Discovering clusters...""
                        $vmClusters = Get-Cluster -Server $global:SourceVIServer -ErrorAction SilentlyContinue
                        Write-Output ""Found $($vmClusters.Count) clusters""
                        
                        $clusterIndex = 0
                        foreach ($vmCluster in $vmClusters) {{
                            $clusterIndex++
                            Write-Output ""PROGRESS:Processing cluster $clusterIndex of $($vmClusters.Count): $($vmCluster.Name)""
                            Write-Output ""Backing up cluster: $($vmCluster.Name)""
                            
                            $clusterConfig = @{{
                                Name = $vmCluster.Name
                                DrsEnabled = $vmCluster.DrsEnabled
                                DrsAutomationLevel = $vmCluster.DrsAutomationLevel.ToString()
                                HAEnabled = $vmCluster.HAEnabled
                                HAAdmissionControlEnabled = $vmCluster.HAAdmissionControlEnabled
                                HAFailoverLevel = $vmCluster.HAFailoverLevel
                                HARestartPriority = $vmCluster.HARestartPriority.ToString()
                                HAIsolationResponse = $vmCluster.HAIsolationResponse.ToString()
                                VsanEnabled = $vmCluster.VsanEnabled
                                EVCMode = $vmCluster.EVCMode
                            }}
                            
                            $safeFileName = $vmCluster.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $clustersPath ""$safeFileName.json""
                            $clusterConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json""
                        }}
                        
                        Write-Output ""Cluster configurations backup completed - $($vmClusters.Count) clusters backed up""
                    }} else {{
                        Write-Output ""PROGRESS:Creating simulated cluster backup (no vCenter connection)""
                        $simulatedClusters = @(
                            @{{
                                Name = 'Production-Cluster'
                                DrsEnabled = $true
                                DrsAutomationLevel = 'FullyAutomated'
                                HAEnabled = $true
                                HAAdmissionControlEnabled = $true
                                HAFailoverLevel = 1
                                HARestartPriority = 'Medium'
                                HAIsolationResponse = 'PowerOff'
                                VsanEnabled = $false
                                EVCMode = 'intel-skylake'
                            }},
                            @{{
                                Name = 'Development-Cluster'
                                DrsEnabled = $true
                                DrsAutomationLevel = 'Manual'
                                HAEnabled = $false
                                HAAdmissionControlEnabled = $false
                                HAFailoverLevel = 0
                                HARestartPriority = 'Low'
                                HAIsolationResponse = 'DoNothing'
                                VsanEnabled = $false
                                EVCMode = ''
                            }}
                        )
                        
                        foreach ($simulatedCluster in $simulatedClusters) {{
                            $safeFileName = $simulatedCluster.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $clustersPath ""$safeFileName.json""
                            $simulatedCluster | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json (simulated)""
                        }}
                        
                        Write-Output ""Cluster configurations backup completed - $($simulatedClusters.Count) simulated clusters backed up""
                    }}
                }} else {{
                    Write-Output ""PROGRESS:Creating simulated cluster backup (PowerCLI not available)""
                    $simulatedClusters = @(
                        @{{
                            Name = 'Production-Cluster'
                            DrsEnabled = $true
                            DrsAutomationLevel = 'FullyAutomated'
                            HAEnabled = $true
                            HAAdmissionControlEnabled = $true
                            HAFailoverLevel = 1
                            HARestartPriority = 'Medium'
                            HAIsolationResponse = 'PowerOff'
                            VsanEnabled = $false
                            EVCMode = 'intel-skylake'
                        }},
                        @{{
                            Name = 'Development-Cluster'
                            DrsEnabled = $true
                            DrsAutomationLevel = 'Manual'
                            HAEnabled = $false
                            HAAdmissionControlEnabled = $false
                            HAFailoverLevel = 0
                            HARestartPriority = 'Low'
                            HAIsolationResponse = 'DoNothing'
                            VsanEnabled = $false
                            EVCMode = ''
                        }}
                    )
                    
                    foreach ($simulatedCluster in $simulatedClusters) {{
                        $safeFileName = $simulatedCluster.Name -replace '[\\/:*?""""<>|]', '_'
                        $configFile = Join-Path $clustersPath ""$safeFileName.json""
                        $simulatedCluster | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                        Write-Output ""Saved: $safeFileName.json (simulated)""
                    }}
                    
                    Write-Output ""Cluster configurations backup completed - $($simulatedClusters.Count) simulated clusters backed up""
                }}
            }} catch {{
                Write-Error ""Cluster configurations backup failed: $($_.Exception.Message)""
                throw
            }}
        ";

        try
        {
            var result = await ExecuteCommandAsync(command, cancellationToken);
            var lines = result.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("PROGRESS:"))
                {
                    var progressText = trimmedLine[9..];
                    progressCallback?.Invoke(progressText);
                }
                else
                {
                    WriteLog($"🏢 {trimmedLine}", "INFO");
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 Cluster configurations backup was cancelled", "WARNING");
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Cluster configurations backup failed: {ex.Message}", "ERROR");
            throw;
        }
    }

    public async Task BackupResourcePoolsAsync(string backupPath, CancellationToken cancellationToken = default, Action<string>? progressCallback = null)
    {
        WriteLog("🏊 Starting Resource Pools backup...", "INFO");
        progressCallback?.Invoke("Initializing Resource Pools backup...");

        var command = $@"
            try {{
                $resourcePoolsPath = Join-Path '{backupPath}' 'ResourcePools'
                New-Item -Path $resourcePoolsPath -ItemType Directory -Force | Out-Null
                Write-Output ""Created Resource Pools backup directory""
                
                if (Get-Module -Name VMware.PowerCLI -ListAvailable -ErrorAction SilentlyContinue) {{
                    if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) {{
                        Write-Output ""PROGRESS:Discovering resource pools...""
                        $vmResourcePools = Get-ResourcePool -Server $global:SourceVIServer -ErrorAction SilentlyContinue | Where-Object {{ $_.Name -ne 'Resources' }}
                        Write-Output ""Found $($vmResourcePools.Count) resource pools""
                        
                        $rpIndex = 0
                        foreach ($vmResourcePool in $vmResourcePools) {{
                            $rpIndex++
                            Write-Output ""PROGRESS:Processing resource pool $rpIndex of $($vmResourcePools.Count): $($vmResourcePool.Name)""
                            Write-Output ""Backing up resource pool: $($vmResourcePool.Name)""
                            
                            $rpConfig = @{{
                                Name = $vmResourcePool.Name
                                CpuExpandableReservation = $vmResourcePool.CpuExpandableReservation
                                CpuLimitMhz = $vmResourcePool.CpuLimitMhz
                                CpuReservationMhz = $vmResourcePool.CpuReservationMhz
                                CpuSharesLevel = $vmResourcePool.CpuSharesLevel.ToString()
                                MemExpandableReservation = $vmResourcePool.MemExpandableReservation
                                MemLimitGB = $vmResourcePool.MemLimitGB
                                MemReservationGB = $vmResourcePool.MemReservationGB
                                MemSharesLevel = $vmResourcePool.MemSharesLevel.ToString()
                                Parent = $vmResourcePool.Parent.Name
                            }}
                            
                            $safeFileName = $vmResourcePool.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $resourcePoolsPath ""$safeFileName.json""
                            $rpConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json""
                        }}
                        
                        Write-Output ""Resource pools backup completed - $($vmResourcePools.Count) resource pools backed up""
                    }} else {{
                        Write-Output ""PROGRESS:Creating simulated resource pools backup (no vCenter connection)""
                        $simulatedResourcePools = @(
                            @{{
                                Name = 'Production-RP'
                                CpuExpandableReservation = $true
                                CpuLimitMhz = -1
                                CpuReservationMhz = 2000
                                CpuSharesLevel = 'High'
                                MemExpandableReservation = $true
                                MemLimitGB = -1
                                MemReservationGB = 4
                                MemSharesLevel = 'High'
                                Parent = 'Production-Cluster'
                            }}
                        )
                        
                        foreach ($simulatedResourcePool in $simulatedResourcePools) {{
                            $safeFileName = $simulatedResourcePool.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $resourcePoolsPath ""$safeFileName.json""
                            $simulatedResourcePool | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json (simulated)""
                        }}
                        
                        Write-Output ""Resource pools backup completed - $($simulatedResourcePools.Count) simulated resource pools backed up""
                    }}
                }} else {{
                    Write-Output ""PROGRESS:Creating simulated resource pools backup (PowerCLI not available)""
                    $simulatedResourcePools = @(
                        @{{
                            Name = 'Production-RP'
                            CpuExpandableReservation = $true
                            CpuLimitMhz = -1
                            CpuReservationMhz = 2000
                            CpuSharesLevel = 'High'
                            MemExpandableReservation = $true
                            MemLimitGB = -1
                            MemReservationGB = 4
                            MemSharesLevel = 'High'
                            Parent = 'Production-Cluster'
                        }}
                    )
                    
                    foreach ($simulatedResourcePool in $simulatedResourcePools) {{
                        $safeFileName = $simulatedResourcePool.Name -replace '[\\/:*?""""<>|]', '_'
                        $configFile = Join-Path $resourcePoolsPath ""$safeFileName.json""
                        $simulatedResourcePool | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                        Write-Output ""Saved: $safeFileName.json (simulated)""
                    }}
                    
                    Write-Output ""Resource pools backup completed - $($simulatedResourcePools.Count) simulated resource pools backed up""
                }}
            }} catch {{
                Write-Error ""Resource pools backup failed: $($_.Exception.Message)""
                throw
            }}
        ";

        try
        {
            var result = await ExecuteCommandAsync(command, cancellationToken);
            var lines = result.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("PROGRESS:"))
                {
                    var progressText = trimmedLine[9..];
                    progressCallback?.Invoke(progressText);
                }
                else
                {
                    WriteLog($"🏊 {trimmedLine}", "INFO");
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 Resource pools backup was cancelled", "WARNING");
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Resource pools backup failed: {ex.Message}", "ERROR");
            throw;
        }
    }

    public async Task BackupFoldersAsync(string backupPath, CancellationToken cancellationToken = default, Action<string>? progressCallback = null)
    {
        WriteLog("📁 Starting VM Folders backup...", "INFO");
        progressCallback?.Invoke("Initializing VM Folders backup...");

        var command = $@"
            try {{
                $foldersPath = Join-Path '{backupPath}' 'Folders'
                New-Item -Path $foldersPath -ItemType Directory -Force | Out-Null
                Write-Output ""Created Folders backup directory""
                
                if (Get-Module -Name VMware.PowerCLI -ListAvailable -ErrorAction SilentlyContinue) {{
                    if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) {{
                        Write-Output ""PROGRESS:Discovering VM folders...""
                        $vmFolders = Get-Folder -Server $global:SourceVIServer -ErrorAction SilentlyContinue | Where-Object {{ $_.Type -eq 'VM' -and $_.Name -ne 'vm' }}
                        Write-Output ""Found $($vmFolders.Count) VM folders""
                        
                        $folderIndex = 0
                        foreach ($vmFolder in $vmFolders) {{
                            $folderIndex++
                            Write-Output ""PROGRESS:Processing folder $folderIndex of $($vmFolders.Count): $($vmFolder.Name)""
                            Write-Output ""Backing up folder: $($vmFolder.Name)""
                            
                            $folderConfig = @{{
                                Name = $vmFolder.Name
                                Type = $vmFolder.Type.ToString()
                                Parent = $vmFolder.Parent.Name
                                ParentType = $vmFolder.Parent.Type.ToString()
                            }}
                            
                            $safeFileName = $vmFolder.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $foldersPath ""$safeFileName.json""
                            $folderConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json""
                        }}
                        
                        Write-Output ""Folders backup completed - $($vmFolders.Count) folders backed up""
                    }} else {{
                        Write-Output ""PROGRESS:Creating simulated folders backup (no vCenter connection)""
                        $simulatedFolders = @(
                            @{{
                                Name = 'Production'
                                Type = 'VM'
                                Parent = 'vm'
                                ParentType = 'Folder'
                            }},
                            @{{
                                Name = 'Development'
                                Type = 'VM'
                                Parent = 'vm'
                                ParentType = 'Folder'
                            }}
                        )
                        
                        foreach ($simulatedFolder in $simulatedFolders) {{
                            $safeFileName = $simulatedFolder.Name -replace '[\\/:*?""""<>|]', '_'
                            $configFile = Join-Path $foldersPath ""$safeFileName.json""
                            $simulatedFolder | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                            Write-Output ""Saved: $safeFileName.json (simulated)""
                        }}
                        
                        Write-Output ""Folders backup completed - $($simulatedFolders.Count) simulated folders backed up""
                    }}
                }} else {{
                    Write-Output ""PROGRESS:Creating simulated folders backup (PowerCLI not available)""
                    $simulatedFolders = @(
                        @{{
                            Name = 'Production'
                            Type = 'VM'
                            Parent = 'vm'
                            ParentType = 'Folder'
                        }},
                        @{{
                            Name = 'Development'
                            Type = 'VM'
                            Parent = 'vm'
                            ParentType = 'Folder'
                        }}
                    )
                    
                    foreach ($simulatedFolder in $simulatedFolders) {{
                        $safeFileName = $simulatedFolder.Name -replace '[\\/:*?""""<>|]', '_'
                        $configFile = Join-Path $foldersPath ""$safeFileName.json""
                        $simulatedFolder | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
                        Write-Output ""Saved: $safeFileName.json (simulated)""
                    }}
                    
                    Write-Output ""Folders backup completed - $($simulatedFolders.Count) simulated folders backed up""
                }}
            }} catch {{
                Write-Error ""Folders backup failed: $($_.Exception.Message)""
                throw
            }}
        ";

        try
        {
            var result = await ExecuteCommandAsync(command, cancellationToken);
            var lines = result.Split('\n');
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("PROGRESS:"))
                {
                    var progressText = trimmedLine[9..];
                    progressCallback?.Invoke(progressText);
                }
                else
                {
                    WriteLog($"📁 {trimmedLine}", "INFO");
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 VM Folders backup was cancelled", "WARNING");
            throw;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ VM Folders backup failed: {ex.Message}", "ERROR");
            throw;
        }
    }

    #endregion

    public void DisconnectAll()
    {
        try
        {
            WriteLog("🔌 Disconnecting from all vCenter servers...", "INFO");

            if (IsPowerCLIAvailable)
            {
                ExecuteCommand("if ($global:SourceVIServer -and $global:SourceVIServer.IsConnected) { Disconnect-VIServer -Server $global:SourceVIServer -Confirm:$false -Force }");
                ExecuteCommand("if ($global:DestVIServer -and $global:DestVIServer.IsConnected) { Disconnect-VIServer -Server $global:DestVIServer -Confirm:$false -Force }");
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

    private async Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ExecuteCommand(command);
        }, cancellationToken);
    }

    private string ExecuteCommand(string command)
    {
        if (_runspace == null || _runspace.RunspaceStateInfo.State != RunspaceState.Opened)
        {
            throw new InvalidOperationException("PowerShell runspace is not available or not opened");
        }

        try
        {
            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;

            ps.AddScript(command);

            var results = ps.Invoke();
            var output = string.Empty;

            if (results != null)
            {
                foreach (var result in results)
                {
                    output += result.ToString() + Environment.NewLine;
                }
            }

            if (ps.Streams.Error.Count > 0)
            {
                foreach (var error in ps.Streams.Error)
                {
                    throw new InvalidOperationException(error.ToString());
                }
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"PowerShell execution failed: {ex.Message}", ex);
        }
    }
    // Add these methods for inventory tree functionality
    public async Task<List<DatacenterInfo>> GetDatacentersAsync()
    {
        try
        {
            if (IsPowerCLIAvailable && IsSourceConnected)
            {
                var command = @"
                Get-Datacenter -Server $global:SourceVIServer | ForEach-Object {
                    ""$($_.Name)|$($_.Id)""
                }
            ";

                var result = await ExecuteCommandAsync(command);
                var datacenters = new List<DatacenterInfo>();

                foreach (var line in result.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        datacenters.Add(new DatacenterInfo { Name = parts[0], Id = parts[1] });
                    }
                }

                return datacenters;
            }
            else
            {
                return new List<DatacenterInfo>
            {
                new() { Name = "Datacenter1", Id = "datacenter-1" }
            };
            }
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Error getting datacenters: {ex.Message}", "ERROR");
            return new List<DatacenterInfo>();
        }
    }

    public async Task<List<ClusterInfo>> GetClustersAsync(DatacenterInfo datacenter)
    {
        try
        {
            if (IsPowerCLIAvailable && IsSourceConnected)
            {
                var command = $@"
                Get-Cluster -Location (Get-Datacenter -Name '{datacenter.Name}') -Server $global:SourceVIServer | ForEach-Object {{
                    ""$($_.Name)|$($_.Id)""
                }}
            ";

                var result = await ExecuteCommandAsync(command);
                var clusters = new List<ClusterInfo>();

                foreach (var line in result.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        clusters.Add(new ClusterInfo { Name = parts[0], Id = parts[1] });
                    }
                }

                return clusters;
            }
            else
            {
                return new List<ClusterInfo>
            {
                new() { Name = "Production-Cluster", Id = "cluster-1" },
                new() { Name = "Development-Cluster", Id = "cluster-2" }
            };
            }
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Error getting clusters: {ex.Message}", "ERROR");
            return new List<ClusterInfo>();
        }
    }

    public async Task<List<HostInfo>> GetHostsAsync(ClusterInfo cluster)
    {
        try
        {
            if (IsPowerCLIAvailable && IsSourceConnected)
            {
                var command = $@"
                Get-VMHost -Location (Get-Cluster -Name '{cluster.Name}') -Server $global:SourceVIServer | ForEach-Object {{
                    ""$($_.Name)|$($_.Id)|$($_.ConnectionState)""
                }}
            ";

                var result = await ExecuteCommandAsync(command);
                var hosts = new List<HostInfo>();

                foreach (var line in result.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        hosts.Add(new HostInfo { Name = parts[0], Id = parts[1], ConnectionState = parts[2] });
                    }
                }

                return hosts;
            }
            else
            {
                return new List<HostInfo>
            {
                new() { Name = "esxi01.domain.local", Id = "host-1", ConnectionState = "Connected" },
                new() { Name = "esxi02.domain.local", Id = "host-2", ConnectionState = "Connected" }
            };
            }
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Error getting hosts: {ex.Message}", "ERROR");
            return new List<HostInfo>();
        }
    }

    public async Task<List<VMInfo>> GetVMsAsync(HostInfo host)
    {
        try
        {
            if (IsPowerCLIAvailable && IsSourceConnected)
            {
                var command = $@"
                Get-VM -Location (Get-VMHost -Name '{host.Name}') -Server $global:SourceVIServer | ForEach-Object {{
                    ""$($_.Name)|$($_.Id)|$($_.PowerState)""
                }}
            ";

                var result = await ExecuteCommandAsync(command);
                var vms = new List<VMInfo>();

                foreach (var line in result.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        vms.Add(new VMInfo { Name = parts[0], Id = parts[1], PowerState = parts[2] });
                    }
                }

                return vms;
            }
            else
            {
                return new List<VMInfo>
            {
                new() { Name = "WebServer01", Id = "vm-1", PowerState = "PoweredOn" },
                new() { Name = "Database01", Id = "vm-2", PowerState = "PoweredOn" }
            };
            }
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Error getting VMs: {ex.Message}", "ERROR");
            return new List<VMInfo>();
        }
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

    // Similar methods for VMs and Clusters


    public void Dispose()
    {
        DisconnectAll();
        _runspace?.Dispose();
        GC.SuppressFinalize(this);
    }
}
