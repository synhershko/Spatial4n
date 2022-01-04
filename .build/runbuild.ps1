properties {
	[string]$base_directory   = resolve-path "..\."
	[string]$release_directory  = "$base_directory\.release"
	[string]$tools_directory  = "$base_directory\.tools"
	[string]$source_directory = "$base_directory"
	[string]$nuget_package_directory = "$release_directory\packagesource"
	[string]$solutionFile = "$base_directory\Spatial4n.sln"
	[string]$versionScriptFile = "$base_directory/build/version.ps1"

	[string]$packageVersion       = ""  
	[string]$assemblyVersion      = ""
	[string]$informationalVersion = ""
	[string]$fileVersion          = ""
	[string]$configuration        = "Release"
	[string]$platform             = "Any CPU"
	[bool]$backupFiles            = $true
}

$backedUpFiles = New-Object System.Collections.ArrayList
$versionInfo = @{}

task default -depends Test

task Clean -description "This task cleans up the build directory" {
	Remove-Item $release_directory -Force -Recurse -ErrorAction SilentlyContinue
	Get-ChildItem $base_directory -Include *.bak -Recurse | foreach ($_) {Remove-Item $_.FullName}
}

task InstallSDK -description "This task makes sure the correct SDK version is installed" {
	& where.exe dotnet.exe
	$sdkVersion = ""

	if ($LASTEXITCODE -eq 0) {
		$sdkVersion = ((& dotnet.exe --version) | Out-String).Trim()
	}
	
	Write-Host "Current SDK version: $sdkVersion" -ForegroundColor Yellow
	if (([version]$sdkVersion) -lt ([version]"2.2.401")) {
		Write-Host "Require SDK version 2.2.401, installing..." -ForegroundColor Red
		#Install the correct version of the .NET SDK for this build
	    Invoke-Expression "$base_directory/.build/dotnet-install.ps1 -Version 2.2.401"
	}

	# Safety check - this should never happen
	& where.exe dotnet.exe

	if ($LASTEXITCODE -ne 0) {
		throw "Could not find dotnet CLI in PATH. Please install the .NET Core 2.0 SDK."
	}
}

task Init -depends InstallSDK -description "This tasks makes sure the build environment is correctly setup" {  

	# Get the version info
	$versionInfoString = Invoke-Expression -Command "$versionScriptFile -PackageVersion ""$packageVersion"" -AssemblyVersion ""$assemblyVersion"" -InformationalVersion ""$informationalVersion"" -FileVersion ""$fileVersion"""
	Write-Host $versionInfoString

    # parse the version numbers and put them into a hashtable
    $versionInfoSplit = $versionInfoString -split '\r?\n' # split $a into lines, whether it has CRLF or LF-only line endings
    foreach ($line in $versionInfoSplit) {
        $kvp = $line -split '\:\s+?'
        $versionInfo.Add($kvp[0], $($kvp[1]).Trim())
    }
	$localInformationalVersion = $versionInfo['InformationalVersion']
	$localFileVersion = $versionInfo['FileVersion']
	$localAssemblyVersion = $versionInfo['AssemblyVersion']
	$localPackageVersion = $versionInfo['PackageVersion']

	Write-Host "Base Directory: $base_directory"
	Write-Host "Release Directory: $release_directory"
	Write-Host "Source Directory: $source_directory"
	Write-Host "Tools Directory: $tools_directory"
	Write-Host "NuGet Package Directory: $nuget_package_directory"
	Write-Host "Template Directory: $template_directory"
	Write-Host "AssemblyVersion: $localAssemblyVersion"
	Write-Host "Package Version: $localPackageVersion"
	Write-Host "File Version: $localFileVersion"
	Write-Host "InformationalVersion Version: $localInformationalVersion"
	Write-Host "Configuration: $configuration"
	
	Ensure-Directory-Exists "$release_directory"
}

task Compile -depends Clean, Init -description "This task compiles the solution" {

	Write-Host "Compiling..." -ForegroundColor Green

	$localInformationalVersion = $versionInfo['InformationalVersion']
	$localFileVersion = $versionInfo['FileVersion']
	$localAssemblyVersion = $versionInfo['AssemblyVersion']

	Exec {
		&dotnet build "$solutionFile" `
			--configuration "$configuration" `
			/p:Platform="$platform" `
			/p:InformationalVersion="$localInformationalVersion" `
			/p:FileVersion="$localFileVersion" `
			/p:AssemblyVersion="$localAssemblyVersion" `
			/p:TestAllTargetFrameworks=true `
			/p:PortableDebugTypeOnly=true `
			/p:SkipGitVersioning=true
	}
}

task Pack -depends Compile -description "This task creates the NuGet packages" {
	Ensure-Directory-Exists "$nuget_package_directory"

	$localPackageVersion = $versionInfo['PackageVersion']

	Exec {
		&dotnet pack "$solutionFile" `
			--configuration $configuration `
			--output "$nuget_package_directory" `
			--no-build `
			--no-restore `
			/p:PackageVersion="$localPackageVersion" `
			/p:SkipGitVersioning=true
	}
}

task Test -depends Pack -description "This task runs the tests" {
	$testProject = "$base_directory\Spatial4n.Tests\Spatial4n.Tests.csproj"
	$xml = [xml](Get-Content $testProject)
	$targetFrameworks = [string]$xml.Project.PropertyGroup.TargetFrameworks;
	$frameworks = $targetFrameworks.Split(';', [StringSplitOptions]::RemoveEmptyEntries)

	foreach ($framework in $frameworks) {
		Write-Host "Running tests for framework: $framework" -ForegroundColor Green

		Exec {
			&dotnet test $testProject --configuration $configuration --framework $framework.Trim() --no-build
		}
	}
}

function Backup-File([string]$path) {
	if ($backupFiles -eq $true) {
		Copy-Item $path "$path.bak" -Force
		$backedUpFiles.Insert(0, $path)
	} else {
		Write-Host "Ignoring backup of file $path" -ForegroundColor DarkRed
	}
}

function Restore-File([string]$path) {
	if ($backupFiles -eq $true) {
		if (Test-Path "$path.bak") {
			Move-Item "$path.bak" $path -Force
		}
		$backedUpFiles.Remove($path)
	}
}

function Ensure-Directory-Exists([string] $path)
{
	if (!(Test-Path $path)) {
		New-Item $path -ItemType Directory
	}
}

function New-TemporaryDirectory {
    $parent = [System.IO.Path]::GetTempPath()
    [string] $name = [System.Guid]::NewGuid()
    New-Item -ItemType Directory -Path (Join-Path $parent $name)
}