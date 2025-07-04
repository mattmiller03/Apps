<#
.SYNOPSIS
  Apply or reapply EVC mode to a single VM using modern PowerCLI 13.3 methods.

.DESCRIPTION
  This script performs the following steps:
   1. Connects to vCenter using the given credentials (if not already connected).
   2. Retrieves the target VM and its associated HostView.
   3. Determines the cluster that the VM belongs to, including robust error handling
      if the VM's host or cluster cannot be determined.
   4. Powers off the VM if it is running.
   5. Constructs a HostFeatureMask array:
      • If -EvcModeKey is provided, it uses the mask from that standard EVC mode.
      • If not, and the cluster is in a userDefined EVC mode, its mask is imported.
      • Otherwise, a custom mask is read from a JSON or CSV file specified by -MaskFile.
   6. Calls ApplyEvcModeVM_Task and waits for completion.
   7. Optionally powers the VM on again.
   8. Logs all operations to the console (with color coding) and to a log file.

.PARAMETER VCServer
  The vCenter server to connect to.

.PARAMETER Credential
  (Optional) A PSCredential object containing credentials for vCenter.

.PARAMETER VMName
  The name of the VM to reconfigure.

.PARAMETER EvcModeKey
  (Optional) The key of a standard EVC mode (e.g., "intel-icelake") to apply.
  If provided, this takes precedence over the cluster's userDefined mode and MaskFile.
  You can list supported keys using: (Get-View ServiceInstance).Capability.SupportedEvcMode | Select Key

.PARAMETER MaskFile
  Path to a JSON or CSV file defining your custom EVC mask.
  This is used if -EvcModeKey is not provided AND the cluster is not in userDefined EVC mode.

.PARAMETER CompleteMasks
  Switch. If set, requires that ALL specified CPU features be present.

.PARAMETER PowerOnAfter
  Switch. If provided, powers the VM back on after applying EVC mode (if it was running).

.PARAMETER LogFolder
  Folder where the logs will be saved. Defaults to the script folder (via $PSScriptRoot)
  or the current working directory if run interactively.

.EXAMPLE
  # Apply a standard EVC mode (e.g., Intel Icelake)
  $cred = Get-Credential
  .\Apply-EvcModeVM.ps1 -VCServer vcenter.domain.local -Credential $cred -VMName MyVM -EvcModeKey "intel-icelake" -CompleteMasks -PowerOnAfter

.EXAMPLE
  # Apply a custom mask from a file (if cluster is not userDefined EVC)
  $cred = Get-Credential
  .\Apply-EvcModeVM.ps1 -VCServer vcenter.domain.local -Credential $cred -VMName MyVM -MaskFile .\SkylakeEvcMask.json -CompleteMasks -PowerOnAfter

.EXAMPLE
  # Use cluster's userDefined EVC mask (if set and -EvcModeKey is not used)
  $cred = Get-Credential
  .\Apply-EvcModeVM.ps1 -VCServer vcenter.domain.local -Credential $cred -VMName MyVM -MaskFile .\SomeMaskFile.json -CompleteMasks -PowerOnAfter
  # Note: MaskFile is still required by parameter definition, but its content is ignored
  # if the cluster is in userDefined mode and -EvcModeKey is not used.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$VCServer,
    [Parameter(Mandatory=$false)][System.Management.Automation.PSCredential]$Credential,
    [Parameter(Mandatory=$true)][string]$VMName,
    [Parameter(Mandatory=$false)][string]$EvcModeKey, # New optional parameter
    [Parameter(Mandatory=$true)][string]$MaskFile,    # Still mandatory, used as fallback
    [switch]$CompleteMasks,
    [switch]$PowerOnAfter,
    [string]$LogFolder = $(if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path })
)

#region Functions

function Write-Log {
    param(
        [ValidateSet('INFO','WARN','ERROR')][string]$Level = 'INFO',
        [Parameter(Mandatory)][string]$Message
    )
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logLine = "{0} [{1}] {2}" -f $timestamp, $Level, $Message
    
    # Write with color to console
    switch ($Level) {
        'INFO'  { Write-Host $logLine -ForegroundColor Cyan }
        'WARN'  { Write-Host $logLine -ForegroundColor Yellow }
        'ERROR' { Write-Host $logLine -ForegroundColor Red }
    }
    
    # Append to log file
    Add-Content -Path $script:LogFile -Value $logLine
}

function Test-VMwareConnection {
    try {
        $null = Get-View -Id 'ServiceInstance' -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Connect-VMwareServer {
    param(
        [string]$Server,
        [System.Management.Automation.PSCredential]$Cred
    )
    try {
        if ($Cred) {
            Connect-VIServer -Server $Server -Credential $Cred -ErrorAction Stop | Out-Null
            Write-Log INFO "Connected to vCenter '$Server' using provided credentials."
        }
        else {
            Connect-VIServer -Server $Server -ErrorAction Stop | Out-Null
            Write-Log INFO "Connected to vCenter '$Server' using current user credentials."
        }
        return $true
    }
    catch {
        Write-Log ERROR "Failed to connect to vCenter: $($_.Exception.Message)"
        return $false
    }
}

function Get-VMWithView {
    param([string]$Name)
    try {
        $vm = Get-VM -Name $Name -ErrorAction Stop
        $vmView = Get-View -VIObject $vm -ErrorAction Stop
        return @{
            VM = $vm
            View = $vmView
        }
    }
    catch {
        Write-Log ERROR "Failed to find VM '$Name': $($_.Exception.Message)"
        return $null
    }
}

function Get-ClusterFromVM {
    param($VMView)
    try {
        # Ensure the VM is assigned to a host
        if (-not $VMView.Runtime.Host) {
            Write-Log WARN "VM '$($VMView.Name)' is not assigned to any host (Runtime.Host is null)."
            return $null
        }
        $hostView = Get-View -Id $VMView.Runtime.Host -ErrorAction Stop
        if (-not $hostView.Parent) {
            Write-Log WARN "Host '$($hostView.Name)' has no parent resource."
            return $null
        }
        $parentResource = Get-View -Id $hostView.Parent -ErrorAction Stop
        if ($parentResource.GetType().Name -eq 'ClusterComputeResource') {
            return $parentResource
        }
        # Check if there's an additional level of hierarchy
        if ($parentResource.Parent) {
            $grandParent = Get-View -Id $parentResource.Parent -ErrorAction Stop
            if ($grandParent.GetType().Name -eq 'ClusterComputeResource') {
                return $grandParent
            }
        }
        Write-Log WARN "VM's host is not part of a cluster. Parent type: $($parentResource.GetType().Name)"
        return $null
    }
    catch {
        Write-Log ERROR "Failed to retrieve cluster: $($_.Exception.Message)"
        return $null
    }
}

function Import-FeatureMaskFile {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Log ERROR "Mask file '$FilePath' not found."
        return $null
    }
    
    try {
        $masks = @()
        $extension = [System.IO.Path]::GetExtension($FilePath).ToLower()
        switch ($extension) {
            '.json' {
                $rawData = Get-Content -Path $FilePath -Raw | ConvertFrom-Json
                foreach ($entry in $rawData) {
                    $mask = New-Object VMware.Vim.HostFeatureMask
                    $mask.FeatureName = $entry.FeatureName
                    $mask.Key = $entry.Key
                    $mask.Value = $entry.Value
                    $masks += $mask
                }
            }
            '.csv' {
                $rawData = Import-Csv -Path $FilePath
                foreach ($entry in $rawData) {
                    $mask = New-Object VMware.Vim.HostFeatureMask
                    $mask.FeatureName = $entry.FeatureName
                    $mask.Key = $entry.Key
                    $mask.Value = $entry.Value
                    $masks += $mask
                }
            }
            default {
                Write-Log ERROR "Unsupported mask file format. Use .json or .csv"
                return $null
            }
        }
        
        Write-Log INFO "Imported $($masks.Count) feature masks from file."
        return $masks
    }
    catch {
        Write-Log ERROR "Failed to parse mask file: $($_.Exception.Message)"
        return $null
    }
}

function Wait-VMwareTask {
    param($TaskMoRef)
    try {
        $task = Get-View -Id $TaskMoRef -ErrorAction Stop
        while ($task.Info.State -in 'running','queued') {
            Start-Sleep -Seconds 2
            $task.UpdateViewData('Info')
        }
        if ($task.Info.State -eq 'success') {
            Write-Log INFO "Task completed successfully."
            return $true
        }
        else {
            $errorMessage = $task.Info.Error.LocalizedMessage
            Write-Log ERROR "Task failed: $errorMessage"
            return $false
        }
    }
    catch {
        Write-Log ERROR "Error monitoring task: $($_.Exception.Message)"
        return $false
    }
}
#endregion

#region Main Script
try {
    # Initialize log file
    if (-not (Test-Path $LogFolder)) {
        New-Item -ItemType Directory -Path $LogFolder -Force | Out-Null
    }
    $script:LogFile = Join-Path $LogFolder ("ApplyEvc_{0}_{1:yyyyMMdd_HHmmss}.log" -f $VMName, (Get-Date))
    Write-Log INFO "Starting EVC application for VM '$VMName'. Log file: $script:LogFile"
    
    # Connect to vCenter if not already connected
    if (-not (Test-VMwareConnection)) {
        Write-Log INFO "No active vCenter connection; connecting to '$VCServer'..."
        if (-not (Connect-VMwareServer -Server $VCServer -Cred $Credential)) {
            throw "Failed to connect to vCenter server."
        }
    }
    else {
        Write-Log INFO "Using existing vCenter connection."
    }
    
    # Retrieve the VM and its view
    $vmData = Get-VMWithView -Name $VMName
    if (-not $vmData) {
        throw "VM not found or access denied."
    }
    $vm = $vmData.VM
    $vmView = $vmData.View
    Write-Log INFO "Found VM '$($vm.Name)' (MoRef: $($vmView.MoRef.Value))"
    
    # --- Determine the EVC mask source and get the mask ---
    $maskArray = @()
    $maskSource = "Unknown" # For logging

    if (-not [string]::IsNullOrEmpty($EvcModeKey)) {
        # Option 1: Use mask from a specified standard EVC mode key
        Write-Log INFO "Attempting to use mask from specified standard EVC mode key: '$EvcModeKey'."
        try {
            $si = Get-View ServiceInstance
            $supportedMode = $si.Capability.SupportedEvcMode | Where-Object {$_.key -eq $EvcModeKey} | Select-Object -First 1
            
            if ($supportedMode -and $supportedMode.FeatureMask) {
                $maskArray = $supportedMode.FeatureMask
                $maskSource = "Standard EVC Mode '$EvcModeKey'"
                Write-Log INFO "Successfully retrieved mask from standard EVC mode '$EvcModeKey'."
            } else {
                 Write-Log ERROR "Specified standard EVC mode key '$EvcModeKey' not found or has no feature mask."
                 throw "Invalid or unsupported EVC mode key provided."
            }
        }
        catch {
             Write-Log ERROR "Failed to retrieve mask for standard EVC mode '$EvcModeKey': $($_.Exception.Message)"
             throw "Error retrieving standard EVC mode mask."
        }

    } else {
        # Option 2: Check cluster's userDefined mode
        $clusterView = Get-ClusterFromVM -VMView $vmView
        if ($clusterView) {
             Write-Log INFO "VM is in cluster '$($clusterView.Name)'."
             $evcConfig = $clusterView.Configuration.EvcConfig
             if ($evcConfig.ModeKey -eq 'userDefined' -and $evcConfig.Mask) {
                 Write-Log INFO "Cluster has a custom (userDefined) EVC mode; importing cluster mask."
                 $maskArray = $evcConfig.Mask
                 $maskSource = "Cluster UserDefined EVC Mode"
                 Write-Log INFO "Imported $($maskArray.Count) feature masks from cluster configuration."
             }
        } else {
             Write-Log WARN "Could not determine cluster for VM or cluster is not in userDefined mode. Proceeding with custom mask file as fallback."
        }

        # Option 3: Fallback to custom mask file if no standard mode key and no cluster userDefined mode
        if (-not $maskArray) {
            Write-Log INFO "Reading custom mask from '$MaskFile'..."
            $maskArray = Import-FeatureMaskFile -FilePath $MaskFile
            $maskSource = "Custom Mask File '$MaskFile'"
            if (-not $maskArray) {
                throw "Failed to load feature masks from file."
            }
        }
    }
    
    Write-Log INFO "Using mask source: $maskSource"
    # --- End of mask determination ---

    # Power off the VM if it is currently powered on
    $wasPoweredOn = $vm.PowerState -eq 'PoweredOn'
    if ($wasPoweredOn) {
        Write-Log INFO "VM is powered on. Initiating power off..."
        Stop-VM -VM $vm -Confirm:$false -ErrorAction Stop | Out-Null
        do {
            Start-Sleep -Seconds 2
            # Refresh VM object to check state
            $vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue
            if (-not $vm) {
                 Write-Log WARN "VM object disappeared during power off wait. Assuming powered off."
                 break # Exit loop if VM object is gone
            }
        } until ($vm.PowerState -eq 'PoweredOff')
        Write-Log INFO "VM powered off successfully."
    }
    else {
        Write-Log INFO "VM is already powered off."
    }
    
    # Apply EVC mode by using the HostFeatureMask array
    Write-Log INFO "Applying EVC mode with $($maskArray.Count) masks, completeMasks=$($CompleteMasks.IsPresent)..."
    
    # Ensure maskArray is not empty before calling the task
    if (-not $maskArray -or $maskArray.Count -eq 0) {
        Write-Log ERROR "No feature masks were determined to apply."
        throw "No feature masks to apply."
    }

    $taskMoRef = $vmView.ApplyEvcModeVM_Task($maskArray, [bool]$CompleteMasks.IsPresent)
    if (-not (Wait-VMwareTask -TaskMoRef $taskMoRef)) {
        throw "EVC mode application failed."
    }
    Write-Log INFO "EVC mode applied successfully to VM '$VMName'."
    
    # Optionally power the VM back on
    if ($PowerOnAfter -and $wasPoweredOn) {
        Write-Log INFO "Powering VM back on..."
        Start-VM -VM $vm -ErrorAction Stop | Out-Null
        Write-Log INFO "VM powered on successfully."
    }
    
    Write-Log INFO "Script completed successfully."
}
catch {
    Write-Log ERROR "Script failed: $($_.Exception.Message)"
    exit 1
}
#endregion
