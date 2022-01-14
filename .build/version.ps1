<#
.SYNOPSIS
Generates the PackageVersion, AssemblyVersion, FileVersion, and InformationalVersion
based on configuration and/or provided parameters.

.OUTPUTS
Generates an array with the following lines in "Name: Value" format
InformationalVersion
FileVersion
AssemblyVersion
PackageVersion

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

.PARAMETER UseLegacyPackageVersion
If true, will not put a - between the pre-release label and the version height
(i.e. -beta1234 rather than -beta-1234). The default value is $false.

.PARAMETER MinimumSdkVersion
The minimum .NET SDK version to require in order to process the command.
The default value is '6.0.100'.
#>

Param(
    [string]$informationalVersion = "",
    [string]$fileVersion = "",
    [string]$assemblyVersion = "",
    [string]$packageVersion = "",
    [string]$nbgvToolVersion = "3.5.73-alpha",
    [switch]$useLegacyPackageVersion = $false,
    [string]$minimumSdkVersion = "6.0.100"
)

function Force-ThreeOrFourComponents([version]$version) {
    
    [int]$maj = [Math]::Max(0, $version.Major)
    [int]$min = [Math]::Max(0, $version.Minor)
    [int]$bld = [Math]::Max(0, $version.Build)
    [int]$rev = $version.Revision

    if ($rev -gt 0) {
        return New-Object System.Version -ArgumentList @($maj, $min, $bld, $rev)
    } else {
        return New-Object System.Version -ArgumentList @($maj, $min, $bld)
    }
}

# Check prerequisites
$sdkVersion = ((& dotnet --version) | Out-String).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "dotnet command was not found. Please install .NET $minimumSdkVersion or higher SDK and make sure it is in your PATH."
}
$releaseVersion = if ($sdkVersion.Contains('-')) { "$sdkVersion".Substring(0, "$sdkVersion".IndexOf('-')) } else { $sdkVersion }
if ([version]$releaseVersion -lt ([version]$minimumSdkVersion)) {
    throw "Minimum .NET SDK $minimumSdkVersion required. Current SDK version is $releaseVersion. Please install the required SDK before running the command."
}

$repoRoot = Split-Path $PSScriptRoot -Parent # Assumes this file is in the /.build directory

#Write-Host "Repo Root: $repoRoot"

& dotnet tool install nbgv --tool-path $repoRoot --version $nbgvToolVersion | Out-Null
try {
    #Write-Host "Generating Version Numbers..."

    $versionInfo = @{}
    $versionInfoString = & $repoRoot/nbgv get-version

    # parse the version numbers and put them into a hashtable
    $versionInfoSplit = $versionInfoString -split '\r?\n' # split $a into lines, whether it has CRLF or LF-only line endings
    foreach ($line in $versionInfoSplit) {
        $kvp = $line -split '\:\s+?'
        $versionInfo.Add($kvp[0], $($kvp[1]).Trim())
    }

    [version]$ver = [version]$versionInfo['Version']
    # 3 or 4 digit number for packageVersion or assemblyInfoVersion
    $version = $(Force-ThreeOrFourComponents $ver).ToString()
    
    # Get the NuGet package version if it wasn't passed in
    if ([string]::IsNullOrWhiteSpace($packageVersion)) {
        $packageVersion = $version
        $nugetPackageVersion = $versionInfo['NuGetPackageVersion']

        # Write-Host "NuGet Package Version: $nugetPackageVersion"
        # Only matches if this is a pre-release version - take the extra dash out between the label and count
        if ($nugetPackageVersion -match "(?<=(?:\d+)(?:\.\d+)(?:\.\d+)(?:\.\d+)?)(-[^\-]+)?-(.*)$") {
            $label = $Matches[1]
            $preReleaseVersion = if ($useLegacyPackageVersion) { $Matches[2] } else { "-" + $Matches[2] }
            $packageVersion = $version + $label + $preReleaseVersion
        }
    }
    # Get the pre-release version (we need to chop off the commit hash, because it is added automatically in the build)
    $assemblyInformationalVersion = $versionInfo['AssemblyInformationalVersion']
    $preReleaseVersion = if ($assemblyInformationalVersion -match "(?<=(?:\d+)(?:\.\d+)(?:\.\d+)(?:\.\d+)?)(-[^\s]+)?") { $Matches[1] } else { '' }

    $informationalVersion = $(if ([string]::IsNullOrEmpty($informationalVersion)) { $version } else { $informationalVersion }) + $preReleaseVersion
    $fileVersion = if ([string]::IsNullOrEmpty($fileVersion)) { $version } else { $fileVersion }
    $assemblyVersion = if ([string]::IsNullOrEmpty($assemblyVersion)) { $versionInfo['AssemblyVersion'] } else { $assemblyVersion }
    

    # Output - needs to be parsed by caller
    "InformationalVersion: $informationalVersion"
    "FileVersion: $fileVersion"
    "AssemblyVersion: $assemblyVersion"
    "PackageVersion: $packageVersion"

} finally {
    & dotnet tool uninstall nbgv --tool-path $repoRoot | Out-Null
}
