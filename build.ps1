#!/usr/bin/env pwsh
# Fallout build bootstrapper. Runs the build project directly (no global tool needed).
#   ./build.ps1 Generate      # regenerate the TheTVDB client from the live spec
#   ./build.ps1                # default target
[CmdletBinding()]
Param([Parameter(ValueFromRemainingArguments = $true)] [string[]] $BuildArguments)

$ErrorActionPreference = 'Stop'
dotnet run --project "$PSScriptRoot/build/_build.csproj" -- @BuildArguments
exit $LASTEXITCODE
