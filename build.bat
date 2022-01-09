@echo off
GOTO endcommentblock
:: -----------------------------------------------------------------------------------
:: This file will build Spatial4n and package the Nuget builds.
::
:: Syntax:
::   build[.bat] [<options>]
::
:: Available Options:
::
::   --file-version <FileVersion>
::   -fv <FileVersion> - File version number. If not supplied, the file version will be the same
::                  as PackageVersion (excluding any pre-release tag).
::
::   --PackageVersion <PackageVersion>
::   -pv <PackageVersion> - Nuget package version. Default is calculated using the nbgv tool based on version.json.
::
::   --configuration <Configuration>
::   -config <Configuration> - MSBuild configuration for the build.
::
::   --test
::   -t - Run the tests.
::
::   All options are case insensitive.
::
:: -----------------------------------------------------------------------------------
:endcommentblock

where pwsh >nul 2>nul
if %ERRORLEVEL% NEQ 0 (echo "Powershell could not be found. Please install version 3 or higher.") else (pwsh -ExecutionPolicy bypass -Command "& '%~dpn0.ps1'" %*)