# Parses and validates the command arguments and bootstraps the Psake build script with the cleaned values

function Get-NextArg([string[]]$arguments, [int]$i, [string]$argName) {
    $i++
    if ($arguments.Length -gt $i -and -not $($arguments[$i]).StartsWith('-')) {
        return $arguments[$i]
    } else {
        throw $("'$argName' requires a value to be passed as the next argument")
    }
}

[string]$packageVersion = ''
[string]$fileVersion = ''
[string]$configuration = 'Release'
[bool]$runTests = $false

for ([int]$i = 0; $i -lt $args.Length; $i++) {
    $arg = $args[$i]
    $loweredArg =  "$arg".ToLowerInvariant()
    
    if ($loweredArg -eq '-t' -or $loweredArg -eq '--test') {
        $runTests = $true
    } elseif ($loweredArg -eq '-config' -or $loweredArg -eq '--configuration') {
        $configuration = Get-NextArg $args $i $arg
        $i++
    } else {
        throw $("Unrecognized argument: '$arg'")
    }
}

[string[]]$task = 'Pack'
if ($runTests) {
    $task = 'Pack','Test'
}
$parameters = @{}
$properties = @{}

if (-not [string]::IsNullOrWhiteSpace($packageVersion)) {
    $properties.packageVersion=$packageVersion
}
if (-not [string]::IsNullOrWhiteSpace($fileVersion)) {
    $properties.fileVersion=$fileVersion
}
if (-not [string]::IsNullOrWhiteSpace($configuration)) {
    $properties.configuration=$configuration
}

Import-Module "$PSScriptRoot/.build/psake/psake.psm1"
Invoke-Psake "$PSScriptRoot/.build/runbuild.ps1" -task $task -properties $properties -parameters $parameters