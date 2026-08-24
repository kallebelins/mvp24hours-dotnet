#requires -Version 5.1

<#
.SYNOPSIS
Instala o Mvp24Hours DevKit globalmente no Kiro.

.DESCRIPTION
Configura o servidor MCP mvp24hours em ~/.kiro/settings/mcp.json e instala as 36
skills globais em ~/.kiro/skills/ (skill-router com catalog/ + cada especialidade
como pasta independente SKILL.md).

O projeto MCP e compilado uma vez em Release e a entrada do mcp.json usa
'dotnet run --no-build', para que Cursor, VS Code e Kiro possam iniciar o servidor
ao mesmo tempo sem disputar os arquivos de bin/ e obj/.

.EXAMPLE
.\Install-Mvp24HoursKiroDevKit.ps1

.EXAMPLE
.\Install-Mvp24HoursKiroDevKit.ps1 -RepoRoot "C:\Dev\Github\mvp24hours\mvp24hours-dotnet" -Force
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [string] $RepoRoot,

    [switch] $Force,

    [switch] $SkipSkill,

    [switch] $SkipMcp,

    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\Mvp24HoursDevKit.Common.ps1"

$resolvedRepoRoot = Resolve-Mvp24HoursRepoRoot -RepoRoot $RepoRoot -ScriptRoot $PSScriptRoot
$dotnetVersion = Test-DotNetSdk

Write-Host 'Mvp24Hours Kiro DevKit install'
Write-Host "  Repository root : $resolvedRepoRoot"
Write-Host "  .NET SDK          : $dotnetVersion"

if (-not $SkipMcp) {
    $mcpPath = Get-Mvp24HoursDevKitKiroMcpPath
    $mcpConfiguration = Get-Mvp24HoursMcpConfiguration

    if ($SkipBuild) {
        Write-Host "  MCP build         : skipped ($mcpConfiguration binaries must already exist)"
    }
    else {
        Invoke-Mvp24HoursMcpBuild -RepoRoot $resolvedRepoRoot | Out-Null
        Write-Host "  MCP build         : $mcpConfiguration"
    }

    $serverEntry = Get-Mvp24HoursKiroMcpServerEntry -RepoRoot $resolvedRepoRoot

    Merge-McpJsonServer `
        -ConfigKind Kiro `
        -ConfigPath $mcpPath `
        -ServerEntry $serverEntry

    Write-Host "  MCP configuration : $mcpPath"
}

if (-not $SkipSkill) {
    $installResult = Install-Mvp24HoursCatalogSkills `
        -RepoRoot $resolvedRepoRoot `
        -ConfigKind Kiro `
        -Force:$Force

    Write-Host "  Skills ($($installResult.Count)) : $($installResult.SkillsRoot)"
    Write-Host "  Router            : $($installResult.RouterPath)"
}

Write-Host ''
Write-Host 'Done. Restart Kiro to load the global MCP server and skills.'
