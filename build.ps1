# Parses and validates the command arguments and bootstraps the Psake build script with the cleaned values

[string]$packageVersion = ''
[string]$packageVersion = ''
[string]$fileVersion = ''
[string]$configuration = 'Release'
[bool]$runTests = $false

for ([int]$i = 0; $i -lt $args.Length; $i++) {
	$arg = $args[$i]
	$lowerdArg =  "$arg".ToLowerInvariant()
	
	if ($lowerdArg -eq '-t' -or $lowerdArg -eq '--test') {
		$runTests = $true
	}
	if ($lowerdArg -eq '-pv' -or $lowerdArg -eq '--packageversion') {
		$packageVersion = Get-NextArg($args, $i, $arg)
	}
	if ($lowerdArg -eq '-v' -or $lowerdArg -eq '--version') {
		$fileVersion = Get-NextArg($args, $i, $arg)
	}
	if ($lowerdArg -eq '-config' -or $lowerdArg -eq '--configuration') {
		$configuration = Get-NextArg($args, $i, $arg)
	}
}

[string[]]$task = 'Pack'
if ($runTests) {
	$task = 'Pack','Test'
}
$parameters = @{}
$properties = @{}

if (-not [string]::IsNullOrWhiteSpace($packageVersion)) {
	$properties.packageVersion='$packageVersion'
}
if (-not [string]::IsNullOrWhiteSpace($fileVersion)) {
	$properties.fileVersion='$fileVersion'
}
if (-not [string]::IsNullOrWhiteSpace($configuration)) {
	$properties.configuration='$configuration'
}

Import-Module "$PSScriptRoot/.build/psake.psm1"
Invoke-Psake "$PSScriptRoot/.build/runbuild.ps1" -task $task -properties $properties -parameters $parameters

function Get-NextArg([string[]]$args, [int]$i, [string]$argName) {
	$i = $i + 1
	if ($args.Length - 1 -ge $i -and -not $args[$i].StartsWith('-')) {
		return $args[$i]
	} else {
		throw "'$argName' requires a value to be passed as the next argument"
	}
}