#requires -Version 5.1

<#
.SYNOPSIS
Instala o Mvp24Hours DevKit globalmente no Cursor.

.DESCRIPTION
Configura o servidor MCP mvp24hours em ~/.cursor/mcp.json e copia a skill
mvp24hours-router para ~/.cursor/skills/.

.EXAMPLE
.\Install-Mvp24HoursCursorDevKit.ps1

.EXAMPLE
.\Install-Mvp24HoursCursorDevKit.ps1 -RepoRoot "C:\Dev\Github\mvp24hours\mvp24hours-dotnet" -Force
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [string] $RepoRoot,

    [switch] $Force,

    [switch] $SkipSkill,

    [switch] $SkipMcp
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\Mvp24HoursDevKit.Common.ps1"

$resolvedRepoRoot = Resolve-Mvp24HoursRepoRoot -RepoRoot $RepoRoot -ScriptRoot $PSScriptRoot
$dotnetVersion = Test-DotNetSdk

Write-Host "Mvp24Hours Cursor DevKit install"
Write-Host "  Repository root : $resolvedRepoRoot"
Write-Host "  .NET SDK          : $dotnetVersion"

if (-not $SkipMcp) {
    $mcpPath = Get-Mvp24HoursDevKitCursorMcpPath
    $serverEntry = Get-Mvp24HoursCursorMcpServerEntry -RepoRoot $resolvedRepoRoot

    Merge-McpJsonServer `
        -ConfigKind Cursor `
        -ConfigPath $mcpPath `
        -ServerEntry $serverEntry

    Write-Host "  MCP configuration : $mcpPath"
}

if (-not $SkipSkill) {
    $skillSource = Get-Mvp24HoursDevKitCursorSkillSourcePath -RepoRoot $resolvedRepoRoot
    $skillDestination = Get-Mvp24HoursDevKitCursorSkillPath

    Copy-Mvp24HoursSkill `
        -SourcePath $skillSource `
        -DestinationPath $skillDestination `
        -Force:$Force

    Write-Host "  Skill             : $skillDestination"
}

Write-Host ''
Write-Host 'Done. Restart Cursor to load the global MCP server and skill.'
