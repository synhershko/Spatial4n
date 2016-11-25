properties {
	[string]$base_directory   = resolve-path "..\."
	[string]$release_directory  = "$base_directory\.release"
	[string]$source_directory = "$base_directory"
	[string]$tools_directory  = "$base_directory\.tools"
	[string]$output_directory = "$release_directory\packagesource"
	[string]$template_directory = "$base_directory\.nuget"

	[string]$version          = "0.0.0"
	[string]$packageVersion   = "$version-pre"
	[string]$configuration    = "Release"

	[string[]]$target_frameworks = @("net35", "net40")

	[string]$common_assembly_info = "$base_directory\CommonAssemblyInfo.cs"
	[string]$copyright_year = [DateTime]::Today.Year.ToString() #Get the current year from the system
	[string]$copyright = "Copyright © 2012 - $copyright_year spatial4j and Itamar Syn-Hershko"
	[string]$company_name = ""
}

task default -depends Finalize

task Clean -description "This task cleans up the build directory" {
	Remove-Item $release_directory -Force -Recurse -ErrorAction SilentlyContinue
	
	Write-Host "Base Directory: $base_directory"
	Write-Host "Release Directory: $release_directory"
	Write-Host "Source Directory: $source_directory"
	Write-Host "Tools Directory: $tools_directory"
	Write-Host "Output Directory: $output_directory"
	Write-Host "Template Directory: $template_directory"
	Write-Host "Version: $version"
	Write-Host "Package Version: $packageVersion"
	Write-Host "Configuration: $configuration"
}

task Init -description "This tasks makes sure the build environment is correctly setup" {  
	#If build runner is MyGet or version is not passed in, parse it from $packageVersion
	if (($env:BuildRunner -ne $null -and $env:BuildRunner -eq "MyGet") -or $version -eq "0.0.0") {		
		$version = $packageVersion
		if ($version.Contains("-") -eq $true) {
			$version = $version.SubString(0, $version.IndexOf("-"))
		}
		echo "Updated version to: $version"
	}
	
	#Backup the original CommonAssemblyInfo.cs file
	Ensure-Directory-Exists "$release_directory"
	Move-Item $common_assembly_info "$common_assembly_info.bak" -Force

	Generate-Assembly-Info `
		-file $common_assembly_info `
		-company $company_name `
		-version $version `
		-packageVersion $packageVersion `
		-copyright $copyright

	# Ensure we have the latest version of NuGet
	exec { 
		&"$tools_directory\nuget\NuGet.exe" update -self
	} -ErrorAction SilentlyContinue
}

task Restore -depends Clean -description "This task runs NuGet package restore" {
	exec { 
		&"$tools_directory\nuget\NuGet.exe" restore "$source_directory\Spatial4n.Legacy.sln"
	}

	#NOTE: Need to run dotnet restore on each directory or they won't build
	$project_directory = "$source_directory\Spatial4n.Core"

	exec {
		cd $project_directory
		dotnet restore
	}

	$project_directory = "$source_directory\Spatial4n.Core.NTS"

	exec {
		cd $project_directory
		dotnet restore
	}
}

task Compile -depends Clean, Init, Restore -description "This task compiles the solution" {

	Write-Host "Compiling..." -ForegroundColor Green

	Build-Framework-Versions $target_frameworks
}

task Package -depends Compile -description "This tasks makes creates the NuGet packages" {
	
	#create the nuget package output directory
	Ensure-Directory-Exists "$output_directory"

	Create-Spatial4n-Core-Package
	Create-Spatial4n-Core-NTS-Package
}

task Finalize -depends Package -description "This tasks finalizes the build" {  
	#Restore the original CommonAssemblyInfo.cs file from backup
	Remove-Item $common_assembly_info -Force -ErrorAction SilentlyContinue
	Move-Item "$common_assembly_info.bak" $common_assembly_info -Force
}

function Create-Spatial4n-Core-Package {
	$output_nuspec_file = "$template_directory\Spatial4n.Core.nuspec"
	
	exec { 
		&"$tools_directory\nuget\NuGet.exe" pack $output_nuspec_file -Symbols -Version $packageVersion -OutputDirectory $output_directory -properties "copyright=$copyright"
	}
}

function Create-Spatial4n-Core-NTS-Package {
	$output_nuspec_file = "$template_directory\Spatial4n.Core.NTS.nuspec"
	
	exec { 
		&"$tools_directory\nuget\NuGet.exe" pack $output_nuspec_file -Symbols -Version $packageVersion -OutputDirectory $output_directory -properties "copyright=$copyright"
	}
}

function Build-Framework-Versions ([string[]] $target_frameworks) {
	#create the build for each version of the framework
	foreach ($target_framework in $target_frameworks) {
		Build-Spatial4n-Core-Legacy-Framework-Version $target_framework
		Build-Spatial4n-Core-NTS-Legacy-Framework-Version $target_framework
	}

	Build-Spatial4n-Core
	Build-Spatial4n-Core-NTS
}

function Build-Spatial4n-Core {
	
	Write-Host "Compiling Spatial4n.Core (Portable)" -ForegroundColor Blue

	$project_directory = "$source_directory\Spatial4n.Core"
	$build_config = [System.String]::Concat($configuration, "_Strong_Name")

	exec { 
		cd $project_directory; 
		dotnet build --configuration $build_config
	}
}

function Build-Spatial4n-Core-NTS {
	Write-Host "Compiling Spatial4n.Core.NTS (Portable)" -ForegroundColor Blue

	$project_directory = "$source_directory\Spatial4n.Core.NTS"
	$build_config = [System.String]::Concat($configuration, "_Strong_Name")

	exec { 
		
		cd $project_directory; 
		dotnet build --configuration $build_config
	}
}

function Build-Spatial4n-Core-Legacy-Framework-Version ([string] $target_framework) {
	$target_framework_upper = $target_framework.toUpper()
	$build_config = Get-TargetFramework-Configuration $target_framework
	
	Write-Host "Compiling Spatial4n.Core for $target_framework_upper" -ForegroundColor Blue

	exec { 
		msbuild "$source_directory\Spatial4n.Core.Legacy\Spatial4n.Core.csproj" `
			/verbosity:quiet `
			/property:Configuration=$build_config `
			"/t:Clean;Rebuild" `
			/property:WarningLevel=3
	}
}

function Build-Spatial4n-Core-NTS-Legacy-Framework-Version ([string] $target_framework) {
	$target_framework_upper = $target_framework.toUpper()
	$build_config = Get-TargetFramework-Configuration $target_framework
	
	Write-Host "Compiling Spatial4n.Core.NTS for $target_framework_upper" -ForegroundColor Blue

	exec { 
		msbuild "$source_directory\Spatial4n.Core.NTS.Legacy\Spatial4n.Core.NTS.csproj" `
			/verbosity:quiet `
			/property:Configuration=$build_config `
			"/t:Clean;Rebuild" `
			/property:WarningLevel=3
	}
}

function Ensure-Directory-Exists([string] $path)
{
	if ([System.IO.Path]::GetFileName($path) -eq "") {
		#add a fake file name if it doesn't exist
		$file = "$path\dummy.tmp"
		$dir = [System.IO.Path]::GetDirectoryName($file)
	}
	elseif ($path.EndsWith("\") -eq $true) {
		#add a fake file name and slash if it is missing
		$file = "$pathdummy.tmp"
		$dir = [System.IO.Path]::GetDirectoryName($file)
	} else {
		#assume the path contains a file name
		$dir = $path
	}
	if ([System.IO.Directory]::Exists($dir) -eq $false) {
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}
}

function Generate-Assembly-Info
{
param(
	[string]$copyright, 
	[string]$version,
	[string]$packageVersion,
	[string]$company,
	[string]$file = $(throw "file is a required parameter.")
)
  $asmInfo = "using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompanyAttribute(""$company"")]
[assembly: AssemblyCopyrightAttribute(""$copyright"")]
[assembly: AssemblyInformationalVersionAttribute(""$packageVersion"")]
[assembly: AssemblyFileVersionAttribute(""$version"")]
"
	$dir = [System.IO.Path]::GetDirectoryName($file)
	if ([System.IO.Directory]::Exists($dir) -eq $false)
	{
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}

	Write-Host "Generating assembly info file: $file"
	out-file -filePath $file -encoding UTF8 -inputObject $asmInfo
}

function Is-Prerelease {
	if ($packageVersion.Contains("-")) {
		return $true
	}
	return $false
}

function Get-TargetFramework-Configuration ([string] $net_version) {
	$build_config = "Release"
	if ($net_version -eq "net35") {
		$build_config = "Release35"
	}
	return $build_config
}