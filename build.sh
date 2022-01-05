#! /usr/bin/env bash
# -----------------------------------------------------------------------------------
# This file will build Spatial4n and package the Nuget builds.
#
# Syntax:
#   build[.bat] [<options>]
#
# Available Options:
#
#   --Version <Version>
#   -v <Version> - Assembly version number. If not supplied, the version will be the same 
#                  as PackageVersion (excluding any pre-release tag).
#
#   --PackageVersion <PackageVersion>
#   -pv <PackageVersion> - Nuget package version. Default is calculated using the nbgv tool based on version.json.
#
#   --Configuration <Configuration>
#   -config <Configuration> - MSBuild configuration for the build.
#
#   --Test
#   -t - Run the tests.
#
#   All options are case insensitive.
#
# -----------------------------------------------------------------------------------
pwsh -ExecutionPolicy bypass -Command "& './build.ps1'" "$@"