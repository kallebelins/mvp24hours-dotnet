#requires -Version 5.1

<#
.SYNOPSIS
Cria ou sobrescreve uma solução SLNX para cada pasta de microserviço.

.DESCRIPTION
Cada pasta imediatamente abaixo de RootPath é considerada um microserviço.

O script:

1. Procura recursivamente arquivos .csproj;
2. Converte o nome da pasta para PascalCase;
3. Cria uma solução .slnx dentro da pasta;
4. Sobrescreve a solução caso ela já exista;
5. Adiciona todos os projetos encontrados usando caminhos relativos.

.EXAMPLE
.\New-MicroserviceSolutions.ps1 `
    -RootPath "C:\Dev\Github\mvp24hours\mvp24hours-dotnet\samples\src"
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $RootPath = (Get-Location).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-PascalCase {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $Name
    )

    # Exemplos:
    # meu-projeto-um  -> MeuProjetoUm
    # meu_projeto_um  -> MeuProjetoUm
    # meu projeto um  -> MeuProjetoUm

    $parts = @(
        $Name -split '[^a-zA-Z0-9]+'
    )

    $convertedParts = @(
        foreach ($part in $parts) {
            if ([string]::IsNullOrWhiteSpace($part)) {
                continue
            }

            if ($part.Length -eq 1) {
                $part.ToUpperInvariant()
                continue
            }

            $normalizedPart = $part.ToLowerInvariant()

            $normalizedPart.Substring(0, 1).ToUpperInvariant() +
                $normalizedPart.Substring(1)
        }
    )

    return $convertedParts -join ""
}

function Get-RelativeProjectPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $BasePath,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $ProjectPath
    )

    $normalizedBasePath = [System.IO.Path]::GetFullPath($BasePath)

    if (-not $normalizedBasePath.EndsWith(
        [System.IO.Path]::DirectorySeparatorChar.ToString()
    )) {
        $normalizedBasePath += [System.IO.Path]::DirectorySeparatorChar
    }

    $normalizedProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)

    $baseUri = [System.Uri]::new($normalizedBasePath)
    $projectUri = [System.Uri]::new($normalizedProjectPath)

    $relativePath = $baseUri.MakeRelativeUri($projectUri).ToString()
    $relativePath = [System.Uri]::UnescapeDataString($relativePath)

    # SLNX utiliza "/" nos caminhos.
    return $relativePath.Replace('\', '/')
}

function Write-SlnxFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $SolutionPath,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]] $ProjectPaths
    )

    $solutionDirectory = Split-Path `
        -Path $SolutionPath `
        -Parent

    $temporaryFileName = ".$([System.IO.Path]::GetFileName($SolutionPath)).tmp"

    $temporaryPath = Join-Path `
        -Path $solutionDirectory `
        -ChildPath $temporaryFileName

    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.IndentChars = "  "
    $settings.OmitXmlDeclaration = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $settings.NewLineChars = [Environment]::NewLine
    $settings.NewLineHandling = [System.Xml.NewLineHandling]::Replace

    $writer = $null

    try {
        $writer = [System.Xml.XmlWriter]::Create(
            $temporaryPath,
            $settings
        )

        $writer.WriteStartElement("Solution")

        foreach ($projectPath in $ProjectPaths) {
            $writer.WriteStartElement("Project")
            $writer.WriteAttributeString("Path", $projectPath)
            $writer.WriteEndElement()
        }

        $writer.WriteEndElement()
        $writer.Flush()
    }
    finally {
        if ($null -ne $writer) {
            $writer.Dispose()
        }
    }

    try {
        # Sobrescreve o arquivo SLNX caso ele já exista.
        Move-Item `
            -LiteralPath $temporaryPath `
            -Destination $SolutionPath `
            -Force
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item `
                -LiteralPath $temporaryPath `
                -Force `
                -ErrorAction SilentlyContinue
        }
    }
}

$resolvedRootPath = (Resolve-Path -LiteralPath $RootPath).Path

Write-Host ""
Write-Host "Pasta raiz: $resolvedRootPath"
Write-Host ""

$ignoredDirectories = @(
    ".git",
    ".github",
    ".vs",
    ".idea",
    "bin",
    "obj",
    "node_modules",
    "packages"
)

# Cada pasta imediatamente abaixo de src representa um microserviço.
$microserviceFolders = @(
    Get-ChildItem `
        -LiteralPath $resolvedRootPath `
        -Directory |
        Where-Object {
            $_.Name -notin $ignoredDirectories
        } |
        Sort-Object Name
)

$createdCount = 0
$overwrittenCount = 0
$skippedCount = 0
$errorCount = 0

foreach ($microserviceFolder in $microserviceFolders) {
    try {
        # @() garante que o resultado sempre seja um array,
        # mesmo quando somente um .csproj for encontrado.
        $projects = @(
            Get-ChildItem `
                -LiteralPath $microserviceFolder.FullName `
                -Filter "*.csproj" `
                -File `
                -Recurse |
                Where-Object {
                    $_.FullName -notmatch `
                        '[\\/](bin|obj|\.git|\.github|\.vs|\.idea|node_modules|packages)[\\/]'
                } |
                Sort-Object FullName
        )

        if ($projects.Count -eq 0) {
            Write-Warning (
                "Nenhum projeto .csproj encontrado em '{0}'." -f
                $microserviceFolder.FullName
            )

            $skippedCount++
            continue
        }

        $solutionName = ConvertTo-PascalCase `
            -Name $microserviceFolder.Name

        if ([string]::IsNullOrWhiteSpace($solutionName)) {
            Write-Warning (
                "Não foi possível gerar o nome da solução para '{0}'." -f
                $microserviceFolder.Name
            )

            $skippedCount++
            continue
        }

        $solutionPath = Join-Path `
            -Path $microserviceFolder.FullName `
            -ChildPath "$solutionName.slnx"

        $solutionAlreadyExists = Test-Path `
            -LiteralPath $solutionPath

        $relativeProjectPaths = @(
            foreach ($project in $projects) {
                Get-RelativeProjectPath `
                    -BasePath $microserviceFolder.FullName `
                    -ProjectPath $project.FullName
            }
        )

        # Garante novamente que o resultado continue sendo um array
        # após o Sort-Object.
        $relativeProjectPaths = @(
            $relativeProjectPaths |
                Sort-Object -Unique
        )

        $operation = if ($solutionAlreadyExists) {
            "Sobrescrever solução com $($relativeProjectPaths.Count) projeto(s)"
        }
        else {
            "Criar solução com $($relativeProjectPaths.Count) projeto(s)"
        }

        if (-not $PSCmdlet.ShouldProcess($solutionPath, $operation)) {
            continue
        }

        Write-SlnxFile `
            -SolutionPath $solutionPath `
            -ProjectPaths $relativeProjectPaths

        if ($solutionAlreadyExists) {
            $overwrittenCount++
            Write-Host "Sobrescrita: $solutionPath"
        }
        else {
            $createdCount++
            Write-Host "Criada:      $solutionPath"
        }

        foreach ($projectPath in $relativeProjectPaths) {
            Write-Host "  + $projectPath"
        }

        Write-Host ""
    }
    catch {
        $errorCount++

        Write-Error (
            "Erro ao processar a pasta '{0}': {1}" -f
            $microserviceFolder.FullName,
            $_.Exception.Message
        )
    }
}

Write-Host "Processamento concluído."
Write-Host "Criadas:       $createdCount"
Write-Host "Sobrescritas:  $overwrittenCount"
Write-Host "Sem projetos:  $skippedCount"
Write-Host "Com erro:      $errorCount"
