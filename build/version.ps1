<#
.SYNOPSIS
Generates the PackgeVersion, AssemblyVersion, FileVersion, and InformationalVersion
based on configuration and/or provided parameters.

.OUTPUTS
Generates the output environment variables:
CI_InformationalVersion
CI_FileVersion
CI_AssemblyVersion
CI_PackageVersion

.PARAMETER InformationalVersion
The informational version for the assembly.

.PARAMETER FileVersion
The file version for the assembly.

.PARAMETER AssemblyVersion
The assembly version for the assembly. This version affects binary compatibility of the assembly,
if changed it will no longer be compatible with the version it was changed from as a drop-in replacement.

.PARAMETER PackageVersion
The NuGet package version that will be displayed at NuGet.org.

.PARAMETER NBGVToolVersion
The version of the NBGV tool that will be installed to generate the version information.
#>

Param(
    [string]$informationalVersion = ""
    [string]$fileVersion = ""
    [string]$assemblyVersion = ""
    [string]$packageVersion = ""
    [string]$nbgvToolVersion = "3.5.68-alpha"
)

function Show-EnvironmentVariables() {
    $environmentVars = Get-ChildItem -path env:* | sort Name
    foreach($var in $environmentVars) {
        $keyname = $var.Key
        $keyvalue = $var.Value
        Write-Output "${keyname}: $keyvalue"
    }
}

# Check prerequisites
& where.exe dotnet.exe
if ($LASTEXITCODE -ne 0) {
	Write-Host "dotnet.exe was not found. Please install .NET 6 SDK."
}

$repoRoot = Split-Path $PSScriptRoot -Parent # Assumes this file is in the /build directory

& dotnet install nbgv --tool-path $repoRoot --version $nbgvToolVersion
try {
    & nbgv cloud --common-vars --all-vars

    Show-EnvironmentVariables

    # 3 or 4 digit number for packageVersion or assemblyInfoVersion
    $version = if ($env:NBGV_VERSIONREVISION -eq '-1') { "$env:NBGV_MAJORMINORVERSION.$env:NBGV_BUILDNUMBER" } else { $env:NBGV_VERSION }

    # Get the NuGet package version if it wasn't passed in
    if ([string]::IsNullOrEmpty($packageVersion)) {
        $packageVersionRaw = $env:NBGV_NUGETPACKAGEVERSION
        $packageDashIndex = "$packageVersionRaw".IndexOf('-')
        $packageVersion = if ($packageDashIndex -eq -1) { $version } else { $version + "$packageVersionRaw".Substring($packageDashIndex, "$packageVersionRaw".Length - $packageDashIndex) }
    }

    $informationalVersion = $(if ([string]::IsNullOrEmpty($informationalVersion)) { $version } else { $informationalVersion }) + $env:NBGV_PRERELEASEVERSION
    $fileVersion = if ([string]::IsNullOrEmpty($fileVersion)) { $env:NBGV_ASSEMBLYFILEVERSION } else { $fileVersion }
    $assemblyVersion = if ([string]::IsNullOrEmpty($assemblyVersion)) { $env:NBGV_ASSEMBLYVERSION } else { $assemblyVersion }

    # Set the environment variables temporarily for this process
    if ($env:TF_BUILD) {
        Write-Host "##vso[task.setvariable variable=CI_InformationalVersion;]$informationalVersion"
        Write-Host "##vso[task.setvariable variable=CI_FileVersion;]$fileVersion"
        Write-Host "##vso[task.setvariable variable=CI_AssemblyVersion;]$assemblyVersion"
        Write-Host "##vso[task.setvariable variable=CI_PackageVersion;]$packageVersion"
        Write-Host "##vso[build.updatebuildnumber]$packageVersion"
    } else {
        $env:CI_InformationalVersion = $informationalVersion
        $env:CI_FileVersion = $fileVersion
        $env:CI_AssemblyVersion = $assemblyVersion
        $env:CI_PackageVersion = $packageVersion
    }

    Show-EnvironmentVariables

} finally {
    & dotnet uninstall nbgv --tool-path $repoRoot
}