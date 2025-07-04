<#
.SYNOPSIS
Tests connectivity to vCenter servers with authentication support

.DESCRIPTION
Verifies network and API connectivity to specified vCenter server with credentials

.PARAMETER Server
Target vCenter server address (FQDN or IP)

.PARAMETER Username
Username for vCenter authentication

.PARAMETER Password
SecureString password for vCenter authentication

.PARAMETER ConnectionType
Specifies if testing source or destination connection (affects output only)

.OUTPUTS
JSON object with test results including:
- ConnectionStatus: Boolean success state
- Version: vCenter version if connected
- NetworkAccess: Boolean for basic connectivity
- ApiAccess: Boolean for successful authentication
- Timestamp: When test was performed
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$true)]
    [string]$Username,
    
    [Parameter(Mandatory=$true)]
    [SecureString]$Password,
    
    [ValidateSet("Source","Destination")]
    [string]$ConnectionType = "Source"
)

$ErrorActionPreference = "Stop"

# Initialize result object
$result = @{
    ConnectionType = $ConnectionType
    Server = $Server
    ConnectionStatus = $false
    Version = $null
    NetworkAccess = $false
    ApiAccess = $false
    Timestamp = (Get-Date -Format "o")
    ErrorMessage = $null
}

try {
    # Convert SecureString to plain text for PSCredential
    $credential = New-Object System.Management.Automation.PSCredential (
        $Username,
        $Password
    )

    # Test basic network connectivity (HTTPS port)
    $result.NetworkAccess = Test-NetConnection -ComputerName $Server -Port 443 -InformationLevel Quiet
    
    if (-not $result.NetworkAccess) {
        throw "Network connectivity test failed (port 443)"
    }

    # Check PowerCLI availability
    if (-not (Get-Module -Name VMware.PowerCLI -ListAvailable -ErrorAction SilentlyContinue)) {
        throw "VMware.PowerCLI module not available"
    }

    # Test API connectivity
    try {
        $connection = Connect-VIServer -Server $Server -Credential $credential -ErrorAction Stop
        
        $result.ConnectionStatus = $true
        $result.Version = $connection.Version
        $result.ApiAccess = $true
        
        Disconnect-VIServer -Server $connection -Confirm:$false -ErrorAction SilentlyContinue
    }
    catch {
        $result.ErrorMessage = "API Connection failed: $($_.Exception.Message)"
        $result.ApiAccess = $false
    }
}
catch {
    $result.ErrorMessage = $_.Exception.Message
}
finally {
    # Clear credential objects from memory
    if ($credential) { $credential.Password.Dispose() }
    if ($connection) { $connection = $null }
}

return $result | ConvertTo-Json -Depth 3
