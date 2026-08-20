#requires -Version 5.1

Set-StrictMode -Version Latest

$script:Mvp24HoursDevKitServerName = 'mvp24hours'
$script:Mvp24HoursDevKitSkillName = 'skill-router'
$script:Mvp24HoursDevKitLegacySkillName = 'mvp24hours-router'
$script:Mvp24HoursDevKitMcpProjectRelativePath = 'mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj'

function Get-Mvp24HoursDevKitCursorMcpPath {
    return Join-Path -Path $env:USERPROFILE -ChildPath '.cursor\mcp.json'
}

function Get-Mvp24HoursDevKitVsCodeMcpPath {
    return Join-Path -Path $env:APPDATA -ChildPath 'Code\User\mcp.json'
}

function Get-Mvp24HoursDevKitCursorSkillsRoot {
    return Join-Path -Path $env:USERPROFILE -ChildPath '.cursor\skills'
}

function Get-Mvp24HoursDevKitVsCodeSkillsRoot {
    return Join-Path -Path $env:USERPROFILE -ChildPath '.copilot\skills'
}

function Get-Mvp24HoursDevKitCursorSkillPath {
    return Join-Path -Path (Get-Mvp24HoursDevKitCursorSkillsRoot) -ChildPath $script:Mvp24HoursDevKitSkillName
}

function Get-Mvp24HoursDevKitVsCodeSkillPath {
    return Join-Path -Path (Get-Mvp24HoursDevKitVsCodeSkillsRoot) -ChildPath $script:Mvp24HoursDevKitSkillName
}

function Get-Mvp24HoursUserSkillsRoot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Cursor', 'VsCode')]
        [string] $ConfigKind
    )

    if ($ConfigKind -eq 'Cursor') {
        return Get-Mvp24HoursDevKitCursorSkillsRoot
    }

    return Get-Mvp24HoursDevKitVsCodeSkillsRoot
}

function Get-Mvp24HoursDevKitCursorLegacySkillPath {
    return Join-Path -Path $env:USERPROFILE -ChildPath ".cursor\skills\$($script:Mvp24HoursDevKitLegacySkillName)"
}

function Get-Mvp24HoursDevKitVsCodeLegacySkillPath {
    return Join-Path -Path $env:USERPROFILE -ChildPath ".copilot\skills\$($script:Mvp24HoursDevKitLegacySkillName)"
}

function Get-Mvp24HoursSkillRouterSourceDir {
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot
    )

    return Join-Path -Path $RepoRoot -ChildPath 'skills\orchestration'
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

function Get-Mvp24HoursYamlSkillName {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $FilePath
    )

    $lines = Get-Content -LiteralPath $FilePath -TotalCount 20 -ErrorAction Stop
    if ($lines.Count -eq 0 -or $lines[0] -ne '---') {
        return $null
    }

    for ($index = 1; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        if ($line -eq '---') {
            break
        }

        if ($line -match '^name:\s*(.+)\s*$') {
            return $Matches[1].Trim()
        }
    }

    return $null
}

function Get-Mvp24HoursDomainSkillFiles {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot
    )

    $skillsRoot = Join-Path -Path $RepoRoot -ChildPath 'skills'
    $skipFileNames = @(
        'README.md',
        'COMPLETION_STATUS.md',
        'PROJECT_SUMMARY.md',
        'SKILLS_GENERATION_GUIDE.md',
        'SKILL_TEMPLATE.md',
        'skill-catalog.md',
        'mcp-scenarios.md'
    )

    $results = @()
    $markdownFiles = Get-ChildItem -LiteralPath $skillsRoot -Recurse -Filter '*.md' -File
    foreach ($markdownFile in $markdownFiles) {
        if ($skipFileNames -contains $markdownFile.Name) {
            continue
        }

        $skillName = Get-Mvp24HoursYamlSkillName -FilePath $markdownFile.FullName
        if ([string]::IsNullOrWhiteSpace($skillName)) {
            continue
        }

        if ($skillName -eq $script:Mvp24HoursDevKitSkillName) {
            continue
        }

        $results += [pscustomobject]@{
            Name = $skillName
            Path = $markdownFile.FullName
        }
    }

    return $results
}

function Copy-Mvp24HoursNamedSkill {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $SourceFile,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $DestinationDirectory,

        [switch] $Force
    )

    if (-not (Test-Path -LiteralPath $SourceFile -PathType Leaf)) {
        throw "Skill source file not found at '$SourceFile'."
    }

    if ((Test-Path -LiteralPath $DestinationDirectory -PathType Container) -and -not $Force) {
        throw "Skill already exists at '$DestinationDirectory'. Use -Force to overwrite."
    }

    $destinationParent = Split-Path -Path $DestinationDirectory -Parent
    if (-not (Test-Path -LiteralPath $destinationParent -PathType Container)) {
        if ($PSCmdlet.ShouldProcess($destinationParent, 'Create directory')) {
            New-Item -Path $destinationParent -ItemType Directory -Force | Out-Null
        }
    }

    if ($PSCmdlet.ShouldProcess($DestinationDirectory, 'Install named skill')) {
        if (Test-Path -LiteralPath $DestinationDirectory -PathType Container) {
            Remove-Item -LiteralPath $DestinationDirectory -Recurse -Force
        }

        New-Item -Path $DestinationDirectory -ItemType Directory -Force | Out-Null
        Copy-Item -LiteralPath $SourceFile -Destination (Join-Path -Path $DestinationDirectory -ChildPath 'SKILL.md') -Force
    }
}

function Install-Mvp24HoursCatalogSkills {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot,

        [Parameter(Mandatory)]
        [ValidateSet('Cursor', 'VsCode')]
        [string] $ConfigKind,

        [switch] $Force
    )

    $skillsRoot = Get-Mvp24HoursUserSkillsRoot -ConfigKind $ConfigKind
    $routerDestination = Join-Path -Path $skillsRoot -ChildPath $script:Mvp24HoursDevKitSkillName

    Copy-Mvp24HoursSkillRouter `
        -RepoRoot $RepoRoot `
        -DestinationPath $routerDestination `
        -Force:$Force

    $installed = 1
    foreach ($skillFile in @(Get-Mvp24HoursDomainSkillFiles -RepoRoot $RepoRoot)) {
        $destination = Join-Path -Path $skillsRoot -ChildPath $skillFile.Name
        Copy-Mvp24HoursNamedSkill `
            -SourceFile $skillFile.Path `
            -DestinationDirectory $destination `
            -Force:$Force
        $installed++
    }

    return [pscustomobject]@{
        SkillsRoot = $skillsRoot
        Count = $installed
        RouterPath = $routerDestination
    }
}

function Uninstall-Mvp24HoursCatalogSkills {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot,

        [Parameter(Mandatory)]
        [ValidateSet('Cursor', 'VsCode')]
        [string] $ConfigKind
    )

    $skillsRoot = Get-Mvp24HoursUserSkillsRoot -ConfigKind $ConfigKind
    $removed = 0

    $names = @($script:Mvp24HoursDevKitSkillName, $script:Mvp24HoursDevKitLegacySkillName)
    foreach ($skillFile in @(Get-Mvp24HoursDomainSkillFiles -RepoRoot $RepoRoot)) {
        $names += $skillFile.Name
    }

    foreach ($name in ($names | Select-Object -Unique)) {
        $destination = Join-Path -Path $skillsRoot -ChildPath $name
        if (Remove-Mvp24HoursSkill -DestinationPath $destination) {
            $removed++
        }
    }

    return $removed
}

function Copy-Mvp24HoursSkillRouter {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $DestinationPath,

        [switch] $Force
    )

    $skillsRoot = Join-Path -Path $RepoRoot -ChildPath 'skills'
    $sourceDir = Get-Mvp24HoursSkillRouterSourceDir -RepoRoot $RepoRoot
    $routerFile = Join-Path -Path $sourceDir -ChildPath 'skill-router.md'
    $catalogFile = Join-Path -Path $sourceDir -ChildPath 'skill-catalog.md'
    $scenariosFile = Join-Path -Path $sourceDir -ChildPath 'mcp-scenarios.md'

    foreach ($requiredFile in @($routerFile, $catalogFile, $scenariosFile)) {
        if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
            throw "Skill source file not found at '$requiredFile'."
        }
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

    $domainDirectories = @(
        Get-ChildItem -LiteralPath $skillsRoot -Directory |
            Where-Object { $_.Name -ne 'orchestration' }
    )

    if ($domainDirectories.Count -eq 0) {
        throw "No domain skill directories found under '$skillsRoot'."
    }

    if ($PSCmdlet.ShouldProcess($DestinationPath, 'Assemble skill-router and domain catalog')) {
        if (Test-Path -LiteralPath $DestinationPath -PathType Container) {
            Remove-Item -LiteralPath $DestinationPath -Recurse -Force
        }

        New-Item -Path $DestinationPath -ItemType Directory -Force | Out-Null
        Copy-Item -LiteralPath $routerFile -Destination (Join-Path -Path $DestinationPath -ChildPath 'SKILL.md') -Force
        Copy-Item -LiteralPath $catalogFile -Destination (Join-Path -Path $DestinationPath -ChildPath 'skill-catalog.md') -Force
        Copy-Item -LiteralPath $scenariosFile -Destination (Join-Path -Path $DestinationPath -ChildPath 'mcp-scenarios.md') -Force

        $catalogDestination = Join-Path -Path $DestinationPath -ChildPath 'catalog'
        New-Item -Path $catalogDestination -ItemType Directory -Force | Out-Null

        foreach ($domainDirectory in $domainDirectories) {
            Copy-Item `
                -LiteralPath $domainDirectory.FullName `
                -Destination $catalogDestination `
                -Recurse `
                -Force
        }
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
