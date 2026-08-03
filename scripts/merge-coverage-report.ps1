#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true)]
    [string[]]$ReportPaths,

    [Parameter(Mandatory = $true)]
    [string]$TargetDir,

    [string]$AssemblyFilters = '+Mvp24Hours*;-Mvp24Hours*.Test*;-Mvp24Hours*.Tests*'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    dotnet tool install -g dotnet-reportgenerator-globaltool --ignore-failed-sources | Out-Null
}

$reportsArg = ($ReportPaths | ForEach-Object { $_ -replace '\\', '/' }) -join ';'

reportgenerator `
    -reports:"$reportsArg" `
    -targetdir:"$TargetDir" `
    -reporttypes:"JsonSummary" `
    -assemblyfilters:"$AssemblyFilters"

Write-Host "Merged coverage report written to $TargetDir/Summary.json"
