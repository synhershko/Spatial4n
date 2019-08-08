properties {
	[string]$base_directory   = resolve-path "..\."
	[string]$release_directory  = "$base_directory\.release"
	[string]$tools_directory  = "$base_directory\.tools"
	[string]$source_directory = "$base_directory"
	[string]$nuget_package_directory = "$release_directory\packagesource"
	[string]$solutionFile = "$base_directory\Spatial4n.sln"

	[string]$packageVersion   = "0.4.1"  
	[string]$version          = "0.0.0"
	[string]$configuration    = "Release"
	[bool]$backupFiles        = $true

	[string]$common_assembly_info = "$base_directory\CommonAssemblyInfo.cs"
	[string]$copyright_year = [DateTime]::Today.Year.ToString() #Get the current year from the system
	[string]$copyright = "Copyright © 2012 - $copyright_year spatial4j and Itamar Syn-Hershko"
	[string]$company_name = ""
}

$backedUpFiles = New-Object System.Collections.ArrayList

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

	Write-Host "Base Directory: $base_directory"
	Write-Host "Release Directory: $release_directory"
	Write-Host "Source Directory: $source_directory"
	Write-Host "Tools Directory: $tools_directory"
	Write-Host "NuGet Package Directory: $nuget_package_directory"
	Write-Host "Template Directory: $template_directory"
	Write-Host "Version: $version"
	Write-Host "Package Version: $packageVersion"
	Write-Host "Configuration: $configuration"
	
	Ensure-Directory-Exists "$release_directory"
}

task Compile -depends Clean, Init -description "This task compiles the solution" {

	Write-Host "Compiling..." -ForegroundColor Green

	pushd $base_directory
	$projects = Get-ChildItem -Path "*.csproj" -Recurse
	popd

	Exec {
		&dotnet msbuild $solutionFile /t:Restore
	}

	#If build runner is MyGet or version is not passed in, parse it from $packageVersion
	if (($env:BuildRunner -ne $null -and $env:BuildRunner -eq "MyGet") -or $version -eq "0.0.0") {		
		$version = $packageVersion
		if ($version.Contains("-") -eq $true) {
			$version = $version.SubString(0, $version.IndexOf("-"))
		}
		echo "Updated version to: $version"
	}

	$pv = $packageVersion
	#check for presense of Git
	& where.exe git.exe
	if ($LASTEXITCODE -eq 0) {
		$gitCommit = ((git rev-parse --verify --short=10 head) | Out-String).Trim()
		$pv = "$packageVersion commit:[$gitCommit]"
	}

	try {
		Backup-File $common_assembly_info

		Generate-Assembly-Info `
			-fileVersion $version `
			-file $common_assembly_info

		Exec {
			&dotnet msbuild $solutionFile /t:Build `
				/p:Configuration=$configuration `
				/p:InformationalVersion=$pv `
				/p:Company=$company_name `
				/p:Copyright=$copyright
		}
	} finally {
		Restore-File $common_assembly_info
	}
}

task Pack -depends Compile -description "This task creates the NuGet packages" {
	Ensure-Directory-Exists $nuget_package_directory

	pushd $base_directory
	$packages = Get-ChildItem -Path "*.csproj" -Recurse | ? { !$_.Directory.Name.Contains(".Test") }
	popd

	foreach ($package in $packages) {
		Write-Host "Creating NuGet package for $package..." -ForegroundColor Magenta
		Exec {
			&dotnet pack $package --output $nuget_package_directory --configuration $configuration --no-build --include-symbols /p:PackageVersion=$packageVersion
		}
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

function Generate-Assembly-Info {
param(
	[string]$fileVersion,
	[string]$file = $(throw "file is a required parameter.")
)

  $asmInfo = "using System;
using System.Reflection;

[assembly: AssemblyFileVersion(""$fileVersion"")]
"
	$dir = [System.IO.Path]::GetDirectoryName($file)
	Ensure-Directory-Exists $dir

	Write-Host "Generating assembly info file: $file"
	Out-File -filePath $file -encoding UTF8 -inputObject $asmInfo
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