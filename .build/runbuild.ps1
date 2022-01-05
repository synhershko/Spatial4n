properties {
	[string]$baseDirectory   = resolve-path "../."
	[string]$releaseDirectory  = "$baseDirectory/release"
	[string]$toolsDirectory  = "$baseDirectory/tools"
	[string]$sourceDirectory = "$baseDirectory"
	[string]$nugetPackageDirectory = "$releaseDirectory/NuGetPackages"
	[string]$testResultsDirectory = "$releaseDirectory/TestResults"
	[string]$solutionFile = "$baseDirectory/Spatial4n.sln"
	[string]$versionScriptFile = "$baseDirectory/build/version.ps1"
	[string]$testResultsFileName = "TestResults.trx"

	[string]$packageVersion       = ""  
	[string]$assemblyVersion      = ""
	[string]$informationalVersion = ""
	[string]$fileVersion          = ""
	[string]$configuration        = "Release"
	[string]$platform             = "Any CPU"
	[bool]$backupFiles            = $true

	#test parameters
	[string]$testPlatforms        = "x64"
}

$backedUpFiles = New-Object System.Collections.ArrayList
$versionInfo = @{}

task default -depends Test

task Clean -description "This task cleans up the build directory" {
	Remove-Item $releaseDirectory -Force -Recurse -ErrorAction SilentlyContinue
	Get-ChildItem $baseDirectory -Include *.bak -Recurse | foreach ($_) {Remove-Item $_.FullName}
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
	    Invoke-Expression "$baseDirectory/.build/dotnet-install.ps1 -Version 2.2.401"
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

	Write-Host "Base Directory: $baseDirectory"
	Write-Host "Release Directory: $releaseDirectory"
	Write-Host "Source Directory: $sourceDirectory"
	Write-Host "Tools Directory: $toolsDirectory"
	Write-Host "NuGet Package Directory: $nugetPackageDirectory"
	Write-Host "Template Directory: $template_directory"
	Write-Host "AssemblyVersion: $localAssemblyVersion"
	Write-Host "Package Version: $localPackageVersion"
	Write-Host "File Version: $localFileVersion"
	Write-Host "InformationalVersion Version: $localInformationalVersion"
	Write-Host "Configuration: $configuration"
	
	Ensure-Directory-Exists "$releaseDirectory"
}

task Compile -depends Clean, Init -description "This task compiles the solution" {

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
	Ensure-Directory-Exists "$nugetPackageDirectory"

	$localPackageVersion = $versionInfo['PackageVersion']

	Exec {
		&dotnet pack "$solutionFile" `
			--configuration $configuration `
			--output "$nugetPackageDirectory" `
			--no-build `
			--no-restore `
			/p:PackageVersion="$localPackageVersion" `
			/p:SkipGitVersioning=true
	}
}

task Test -depends Pack -description "This task runs the tests" {

	pushd $baseDirectory
    $testProjects = Get-ChildItem -Path "$sourceDirectory/**/*.csproj" -Recurse
    popd

	$testProjects = $testProjects | Sort-Object -Property FullName
	Ensure-Directory-Exists $testResultsDirectory

	foreach ($testProject in $testProjects) {
		$testName = $testProject.Directory.Name
	
		# Call the target to get the configured test frameworks for this project. We only read the first line because MSBuild adds extra output.
		$frameworksString = $(dotnet build "$testProject" --verbosity minimal --nologo --no-restore /t:PrintTargetFrameworks /p:TestProjectsOnly=true)[0].Trim()
	
		#Write-Host "Test Framework String: $frameworksString"
		if ($frameworksString -eq 'none') {
			Write-Host "Skipping project '$testProject' because it is not marked with `<IsTestProject`>true`<`/IsTestProject`> and/or it contains no test frameworks for the current environment." -ForegroundColor DarkYellow
			continue
		}
	
		[string[]]$frameworks = $frameworksString -split '\s*;\s*'
		foreach ($framework in $frameworks) {

			$testPlatformArray = $testPlatforms -split '\s*[;,]\s*'
			foreach ($testPlatform in $testPlatformArray) {

				$testResultDirectory = "$testResultsDirectory/$framework/$testPlatform/$testName"
				Ensure-Directory-Exists $testResultDirectory

				Write-Host "Running tests for: $testName,$framework,$testPlatform" -ForegroundColor Green
				&dotnet test "$testProject" `
					--configuration $configuration `
					--framework $framework `
					--no-build `
					--no-restore `
					--blame-hang-timeout 10minutes `
					--blame-hang-dump-type mini `
					--results-directory "$testResultDirectory" `
					--logger:"trx;LogFileName=$testResultsFileName" `
					-- RunConfiguration.TargetPlatform=$testPlatform
				#	--logger:"console;verbosity=normal"
			}
			Write-Host ""
			Write-Host "See the .trx logs in $(Normalize-FileSystemSlashes "$testResultsDirectory/$framework") for more details." -ForegroundColor DarkCyan
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

function Normalize-FileSystemSlashes([string]$path) {
	$sep = [System.IO.Path]::DirectorySeparatorChar
	return $($path -replace '/',$sep -replace '\\',$sep)
}