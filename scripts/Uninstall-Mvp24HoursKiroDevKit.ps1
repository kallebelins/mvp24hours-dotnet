#requires -Version 5.1

<#
.SYNOPSIS
Remove a instalacao global do Mvp24Hours DevKit no Kiro.

.DESCRIPTION
Remove apenas a entrada mvp24hours de ~/.kiro/settings/mcp.json, apaga as 36
skills globais em ~/.kiro/skills/ (e o legado mvp24hours-router) e remove a
variavel legada MVP24HOURS_MCP_REPO_ROOT do usuario, se existir.

.EXAMPLE
.\Uninstall-Mvp24HoursKiroDevKit.ps1
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [string] $RepoRoot,

    [switch] $SkipSkill,

    [switch] $SkipMcp
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\Mvp24HoursDevKit.Common.ps1"

$resolvedRepoRoot = Resolve-Mvp24HoursRepoRoot -RepoRoot $RepoRoot -ScriptRoot $PSScriptRoot

Write-Host 'Mvp24Hours Kiro DevKit uninstall'

if (-not $SkipMcp) {
    $mcpPath = Get-Mvp24HoursDevKitKiroMcpPath
    $removed = Remove-McpJsonServer -ConfigKind Kiro -ConfigPath $mcpPath

    if ($removed) {
        Write-Host "  Removed MCP server '$($script:Mvp24HoursDevKitServerName)' from $mcpPath"
    }
    else {
        Write-Host "  MCP server '$($script:Mvp24HoursDevKitServerName)' was not configured in $mcpPath"
    }
}

if (-not $SkipSkill) {
    $removedCount = Uninstall-Mvp24HoursCatalogSkills `
        -RepoRoot $resolvedRepoRoot `
        -ConfigKind Kiro

    Write-Host "  Removed $removedCount skill folder(s) from $(Get-Mvp24HoursDevKitKiroSkillsRoot)"
}

$removedLegacyEnvVar = Remove-LegacyMvp24HoursMcpRepoRootEnvVar
if ($removedLegacyEnvVar) {
    Write-Host '  Removed legacy environment variable MVP24HOURS_MCP_REPO_ROOT'
}

Write-Host ''
Write-Host 'Done. Restart Kiro to apply the changes.'
