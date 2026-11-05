param(
    [switch]$Verify,
    [ValidateSet('', 'net10.0-windows10.0.19041.0', 'net10.0-android', 'net10.0-ios', 'net10.0-maccatalyst')]
    [string]$NativeTarget = ''
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$repo = Split-Path $PSScriptRoot -Parent
$previousTarget = $env:QuantumMobileTargetFramework
try {
    $solution = Join-Path $PSScriptRoot 'Quantum.Mobile.Checks.slnx'
    if ($NativeTarget) {
        $env:QuantumMobileTargetFramework = $NativeTarget
        $solution = Join-Path $repo 'Quantum.Mobile.slnx'
        dotnet restore $solution
    }
    $arguments = @('--no-restore')
    if ($Verify) { $arguments += '--verify-no-changes' }
    dotnet format whitespace $solution @arguments
    dotnet format style $solution --severity info --diagnostics IDE0011 IDE0007 IDE0022 IDE0040 IDE0044 IDE0005 IDE0161 @arguments
}
finally {
    $env:QuantumMobileTargetFramework = $previousTarget
}
