#requires -Version 5.1

Set-StrictMode -Version Latest

$script:Mvp24HoursDevKitServerName = 'mvp24hours'
$script:Mvp24HoursDevKitSkillName = 'mvp24hours-router'
$script:Mvp24HoursDevKitMcpProjectRelativePath = 'mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj'

function Get-Mvp24HoursDevKitCursorMcpPath {
    return Join-Path -Path $env:USERPROFILE -ChildPath '.cursor\mcp.json'
}

function Get-Mvp24HoursDevKitVsCodeMcpPath {
    return Join-Path -Path $env:APPDATA -ChildPath 'Code\User\mcp.json'
}

function Get-Mvp24HoursDevKitCursorSkillPath {
    return Join-Path -Path $env:USERPROFILE -ChildPath ".cursor\skills\$($script:Mvp24HoursDevKitSkillName)"
}

function Get-Mvp24HoursDevKitVsCodeSkillPath {
    return Join-Path -Path $env:USERPROFILE -ChildPath ".copilot\skills\$($script:Mvp24HoursDevKitSkillName)"
}

function Get-Mvp24HoursDevKitCursorSkillSourcePath {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot
    )

    return Join-Path -Path $RepoRoot -ChildPath "devkit\cursor\.cursor\skills\$($script:Mvp24HoursDevKitSkillName)"
}

function Get-Mvp24HoursDevKitVsCodeSkillSourcePath {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot
    )

    return Join-Path -Path $RepoRoot -ChildPath "devkit\vscode\.github\skills\$($script:Mvp24HoursDevKitSkillName)"
}

function ConvertTo-McpJsonPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $Path
    )

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    return ($resolvedPath -replace '\\', '/')
}

function Resolve-Mvp24HoursRepoRoot {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $RepoRoot,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string] $ScriptRoot = $PSScriptRoot
    )

    $candidateRoot = if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        Split-Path -Path $ScriptRoot -Parent
    }
    else {
        $RepoRoot
    }

    if (-not (Test-Path -LiteralPath $candidateRoot -PathType Container)) {
        throw "Repository root not found: '$candidateRoot'."
    }

    $resolvedRoot = (Resolve-Path -LiteralPath $candidateRoot).Path
    $mcpProjectPath = Join-Path -Path $resolvedRoot -ChildPath $script:Mvp24HoursDevKitMcpProjectRelativePath

    if (-not (Test-Path -LiteralPath $mcpProjectPath -PathType Leaf)) {
        throw "MCP project not found at '$mcpProjectPath'. Provide -RepoRoot pointing to the mvp24hours-dotnet clone root."
    }

    return $resolvedRoot
}

function Test-DotNetSdk {
    [CmdletBinding()]
    param()

    $dotnetCommand = Get-Command -Name dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCommand) {
        throw '.NET SDK not found on PATH. Install .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0'
    }

    $versionLine = (& dotnet --version 2>&1 | Select-Object -First 1).ToString().Trim()
    if ([string]::IsNullOrWhiteSpace($versionLine) -or $versionLine -notmatch '^\d+\.\d+') {
        throw "Failed to detect .NET SDK version. Output: $versionLine"
    }

    if ($versionLine -notmatch '^10\.') {
        Write-Warning "Detected .NET SDK version '$versionLine'. Mvp24Hours MCP requires .NET 10."
    }

    return $versionLine
}

function Get-Mvp24HoursCursorMcpServerEntry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot
    )

    $normalizedRoot = ConvertTo-McpJsonPath -Path $RepoRoot
    $projectPath = "$normalizedRoot/mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj"

    return [ordered]@{
        command = 'dotnet'
        args = @(
            'run',
            '--project',
            $projectPath
        )
        env = [ordered]@{
            MVP24HOURS_REPO_ROOT = $normalizedRoot
        }
    }
}

function Get-Mvp24HoursVsCodeMcpServerEntry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot
    )

    $normalizedRoot = ConvertTo-McpJsonPath -Path $RepoRoot
    $projectPath = "$normalizedRoot/mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj"

    return [ordered]@{
        type = 'stdio'
        command = 'dotnet'
        args = @(
            'run',
            '--project',
            $projectPath
        )
        env = [ordered]@{
            MVP24HOURS_REPO_ROOT = $normalizedRoot
        }
    }
}

function ConvertTo-OrderedHashtable {
    [CmdletBinding()]
    param(
        [Parameter()]
        $InputObject
    )

    if ($null -eq $InputObject) {
        return [ordered]@{}
    }

    if ($InputObject -is [System.Collections.IDictionary]) {
        $result = [ordered]@{}
        foreach ($entry in $InputObject.GetEnumerator()) {
            $result[$entry.Key] = $entry.Value
        }

        return $result
    }

    $orderedResult = [ordered]@{}
    foreach ($property in $InputObject.PSObject.Properties) {
        $orderedResult[$property.Name] = $property.Value
    }

    return $orderedResult
}

function Read-McpJsonConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $ConfigPath
    )

    if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        return [ordered]@{}
    }

    $rawContent = Get-Content -LiteralPath $ConfigPath -Raw -Encoding UTF8
    if ([string]::IsNullOrWhiteSpace($rawContent)) {
        return [ordered]@{}
    }

    $parsed = $rawContent | ConvertFrom-Json
    return ConvertTo-OrderedHashtable -InputObject $parsed
}

function Write-McpJsonConfig {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $ConfigPath,

        [Parameter(Mandatory)]
        [hashtable] $Config
    )

    $configDirectory = Split-Path -Path $ConfigPath -Parent
    if (-not (Test-Path -LiteralPath $configDirectory -PathType Container)) {
        if ($PSCmdlet.ShouldProcess($configDirectory, 'Create directory')) {
            New-Item -Path $configDirectory -ItemType Directory -Force | Out-Null
        }
    }

    $json = ($Config | ConvertTo-Json -Depth 10)

    if ($PSCmdlet.ShouldProcess($ConfigPath, 'Write MCP configuration')) {
        Set-Content -LiteralPath $ConfigPath -Value $json -Encoding UTF8
    }
}

function Merge-McpJsonServer {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Cursor', 'VsCode')]
        [string] $ConfigKind,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $ConfigPath,

        [Parameter(Mandatory)]
        [hashtable] $ServerEntry
    )

    $serversPropertyName = if ($ConfigKind -eq 'Cursor') { 'mcpServers' } else { 'servers' }
    $config = Read-McpJsonConfig -ConfigPath $ConfigPath
    $servers = ConvertTo-OrderedHashtable -InputObject $config[$serversPropertyName]

    $servers[$script:Mvp24HoursDevKitServerName] = $ServerEntry
    $config[$serversPropertyName] = $servers

    Write-McpJsonConfig -ConfigPath $ConfigPath -Config $config
}

function Remove-McpJsonServer {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Cursor', 'VsCode')]
        [string] $ConfigKind,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $ConfigPath
    )

    if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        Write-Verbose "MCP configuration not found at '$ConfigPath'. Nothing to remove."
        return $false
    }

    $serversPropertyName = if ($ConfigKind -eq 'Cursor') { 'mcpServers' } else { 'servers' }
    $config = Read-McpJsonConfig -ConfigPath $ConfigPath
    $servers = ConvertTo-OrderedHashtable -InputObject $config[$serversPropertyName]

    if (-not $servers.Contains($script:Mvp24HoursDevKitServerName)) {
        return $false
    }

    $servers.Remove($script:Mvp24HoursDevKitServerName) | Out-Null

    if ($servers.Count -eq 0) {
        $config.Remove($serversPropertyName)
    }
    else {
        $config[$serversPropertyName] = $servers
    }

    if ($config.Count -eq 0) {
        if ($PSCmdlet.ShouldProcess($ConfigPath, 'Remove empty MCP configuration file')) {
            Remove-Item -LiteralPath $ConfigPath -Force
        }
    }
    else {
        Write-McpJsonConfig -ConfigPath $ConfigPath -Config $config
    }

    return $true
}

function Remove-LegacyMvp24HoursMcpRepoRootEnvVar {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    $legacyEnvVarName = 'MVP24HOURS_MCP_REPO_ROOT'
    $currentValue = [Environment]::GetEnvironmentVariable($legacyEnvVarName, [EnvironmentVariableTarget]::User)

    if ([string]::IsNullOrWhiteSpace($currentValue)) {
        return $false
    }

    if ($PSCmdlet.ShouldProcess($legacyEnvVarName, 'Remove legacy user environment variable')) {
        [Environment]::SetEnvironmentVariable(
            $legacyEnvVarName,
            $null,
            [EnvironmentVariableTarget]::User
        )
    }

    return $true
}

function Copy-Mvp24HoursSkill {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $SourcePath,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $DestinationPath,

        [switch] $Force
    )

    if (-not (Test-Path -LiteralPath $SourcePath -PathType Container)) {
        throw "Skill source not found at '$SourcePath'."
    }

    if ((Test-Path -LiteralPath $DestinationPath -PathType Container) -and -not $Force) {
        throw "Skill already exists at '$DestinationPath'. Use -Force to overwrite."
    }

    $destinationParent = Split-Path -Path $DestinationPath -Parent
    if (-not (Test-Path -LiteralPath $destinationParent -PathType Container)) {
        if ($PSCmdlet.ShouldProcess($destinationParent, 'Create directory')) {
            New-Item -Path $destinationParent -ItemType Directory -Force | Out-Null
        }
    }

    if ($PSCmdlet.ShouldProcess($DestinationPath, 'Copy skill directory')) {
        if (Test-Path -LiteralPath $DestinationPath -PathType Container) {
            Remove-Item -LiteralPath $DestinationPath -Recurse -Force
        }

        Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Recurse -Force
    }
}

function Remove-Mvp24HoursSkill {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $DestinationPath
    )

    if (-not (Test-Path -LiteralPath $DestinationPath -PathType Container)) {
        Write-Verbose "Skill not found at '$DestinationPath'. Nothing to remove."
        return $false
    }

    if ($PSCmdlet.ShouldProcess($DestinationPath, 'Remove skill directory')) {
        Remove-Item -LiteralPath $DestinationPath -Recurse -Force
    }

    return $true
}
