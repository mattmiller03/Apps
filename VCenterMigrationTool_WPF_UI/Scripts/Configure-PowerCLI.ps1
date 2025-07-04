<#
.SYNOPSIS
Configures global PowerCLI settings for migration operations
#>

param(
    [ValidateSet("Ignore","Prompt","Fail")]
    [string]$InvalidCertAction = "Ignore",
    
    [bool]$CEIPSetting = $false,
    
    [ValidateSet("Single","Multiple")]
    [string]$DefaultServerMode = "Multiple"
)

try {
    # Set PowerCLI configuration
    Set-PowerCLIConfiguration -InvalidCertificateAction $InvalidCertAction -Confirm:$false | Out-Null
    Set-PowerCLIConfiguration -Scope User -ParticipateInCEIP $CEIPSetting -Confirm:$false | Out-Null
    Set-PowerCLIConfiguration -DefaultVIServerMode $DefaultServerMode -Confirm:$false | Out-Null

    # Verify settings
    $config = Get-PowerCLIConfiguration
    return @{
        Success = $true
        InvalidCertAction = $config.InvalidCertificateAction
        CEIPEnabled = $config.ParticipateInCEIP
        ServerMode = $config.DefaultVIServerMode
    } | ConvertTo-Json
}
catch {
    Write-Error "PowerCLI configuration failed: $_"
    exit 1
}
