#requires -Version 5.1

<#
.SYNOPSIS
Instala o Mvp24Hours DevKit globalmente no VS Code.

.DESCRIPTION
Configura o servidor MCP mvp24hours em %APPDATA%\Code\User\mcp.json e copia a skill
mvp24hours-router para ~/.copilot/skills/.

.EXAMPLE
.\Install-Mvp24HoursVsCodeDevKit.ps1

.EXAMPLE
.\Install-Mvp24HoursVsCodeDevKit.ps1 -RepoRoot "C:\Dev\Github\mvp24hours\mvp24hours-dotnet" -Force
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

Write-Host 'Mvp24Hours VS Code DevKit install'
Write-Host "  Repository root : $resolvedRepoRoot"
Write-Host "  .NET SDK          : $dotnetVersion"

if (-not $SkipMcp) {
    $mcpPath = Get-Mvp24HoursDevKitVsCodeMcpPath
    $serverEntry = Get-Mvp24HoursVsCodeMcpServerEntry -RepoRoot $resolvedRepoRoot

    Merge-McpJsonServer `
        -ConfigKind VsCode `
        -ConfigPath $mcpPath `
        -ServerEntry $serverEntry

    Write-Host "  MCP configuration : $mcpPath"
}

if (-not $SkipSkill) {
    $skillSource = Get-Mvp24HoursDevKitVsCodeSkillSourcePath -RepoRoot $resolvedRepoRoot
    $skillDestination = Get-Mvp24HoursDevKitVsCodeSkillPath

    Copy-Mvp24HoursSkill `
        -SourcePath $skillSource `
        -DestinationPath $skillDestination `
        -Force:$Force

    Write-Host "  Skill             : $skillDestination"
}

Write-Host ''
Write-Host 'Done. Reload VS Code and run MCP: List Servers to start mvp24hours.'
