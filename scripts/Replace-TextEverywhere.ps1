#requires -Version 5.1

<#
.SYNOPSIS
Substitui texto em pasta de destino, incluindo nomes de pastas, nomes de arquivos e conteúdo de arquivos de texto.

.DESCRIPTION
Executa busca/substituição case-insensitive na pasta de destino e em todas as subpastas.
Ao substituir, tenta preservar o estilo de caixa da ocorrência encontrada:
- MAIUSCULO -> REPLACEMENT em MAIUSCULO
- minusculo -> replacement em minusculo
- Capitalizado -> Replacement capitalizado

.EXAMPLE
.\Replace-TextEverywhere.ps1 `
	-DestinationPath "C:\Repo\MeuProjeto" `
	-SearchText "App" `
	-ReplaceText "Orders" `
	-WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param(
	[Parameter(Mandatory)]
	[ValidateNotNullOrEmpty()]
	[string] $DestinationPath,

	[Parameter(Mandatory)]
	[ValidateNotNullOrEmpty()]
	[string] $SearchText,

	[Parameter(Mandatory)]
	[AllowEmptyString()]
	[string] $ReplaceText,

	[Parameter()]
	[string[]] $IncludeExtensions = @(
		".cs", ".csproj", ".sln", ".slnx",
		".json", ".yml", ".yaml", ".xml",
		".props", ".targets", ".config",
		".md", ".txt", ".ps1", ".psm1",
		".cmd", ".bat", ".sh", ".editorconfig",
		".dockerignore", ".gitignore"
	)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Convert-ReplacementByCase {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string] $MatchedText,

		[Parameter(Mandatory)]
		[AllowEmptyString()]
		[string] $Replacement
	)

	if ([string]::IsNullOrEmpty($Replacement)) {
		return $Replacement
	}

	if ($MatchedText -cmatch '^[^a-z]*[A-Z][^a-z]*$') {
		return $Replacement.ToUpperInvariant()
	}

	if ($MatchedText -cmatch '^[^A-Z]*[a-z][^A-Z]*$') {
		return $Replacement.ToLowerInvariant()
	}

	if ($MatchedText -cmatch '^[A-Z][a-z0-9]*$') {
		if ($Replacement.Length -eq 1) {
			return $Replacement.ToUpperInvariant()
		}

		return $Replacement.Substring(0, 1).ToUpperInvariant() +
			$Replacement.Substring(1).ToLowerInvariant()
	}

	return $Replacement
}

function Replace-TextCaseInsensitive {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string] $InputText,

		[Parameter(Mandatory)]
		[string] $OldText,

		[Parameter(Mandatory)]
		[AllowEmptyString()]
		[string] $NewText
	)

	$escapedPattern = [Regex]::Escape($OldText)

	return [Regex]::Replace(
		$InputText,
		$escapedPattern,
		{
			param($match)
			Convert-ReplacementByCase -MatchedText $match.Value -Replacement $NewText
		},
		[System.Text.RegularExpressions.RegexOptions]::IgnoreCase
	)
}

function Should-ProcessFileContent {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[System.IO.FileInfo] $File,

		[Parameter(Mandatory)]
		[string[]] $Extensions
	)

	$specialNames = @(
		"Dockerfile",
		"docker-compose.yml",
		"docker-compose.yaml"
	)

	if ($File.Name -in $specialNames) {
		return $true
	}

	return $Extensions -contains $File.Extension
}

function Assert-PathInsideDestination {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[string] $Path,

		[Parameter(Mandatory)]
		[string] $BasePath
	)

	$fullPath = [System.IO.Path]::GetFullPath($Path)
	$fullBasePath = [System.IO.Path]::GetFullPath($BasePath)

	if (-not $fullBasePath.EndsWith([System.IO.Path]::DirectorySeparatorChar.ToString())) {
		$fullBasePath += [System.IO.Path]::DirectorySeparatorChar
	}

	if (-not $fullPath.StartsWith($fullBasePath, [System.StringComparison]::OrdinalIgnoreCase) -and
		-not $fullPath.Equals($fullBasePath.TrimEnd([System.IO.Path]::DirectorySeparatorChar), [System.StringComparison]::OrdinalIgnoreCase)) {
		throw "Path '$Path' is outside of destination '$BasePath'."
	}
}

if ([string]::IsNullOrWhiteSpace($SearchText)) {
	throw "SearchText cannot be empty."
}

$resolvedDestination = (Resolve-Path -LiteralPath $DestinationPath).Path
Assert-PathInsideDestination -Path $resolvedDestination -BasePath $resolvedDestination

$ignoredDirectoryNames = @(
	".git",
	".vs",
	".idea",
	"bin",
	"obj",
	"node_modules",
	"packages"
)

$directoriesRenamed = 0
$filesRenamed = 0
$filesUpdated = 0

# 1) Renomeia pastas de baixo para cima para evitar quebrar caminhos durante o processo.
$directories = @(
	Get-ChildItem -LiteralPath $resolvedDestination -Directory -Recurse |
		Where-Object { $_.Name -notin $ignoredDirectoryNames } |
		Sort-Object FullName -Descending
)

foreach ($directory in $directories) {
	$newDirectoryName = Replace-TextCaseInsensitive -InputText $directory.Name -OldText $SearchText -NewText $ReplaceText

	if ($newDirectoryName -ceq $directory.Name) {
		continue
	}

	$targetDirectoryPath = Join-Path -Path $directory.Parent.FullName -ChildPath $newDirectoryName

	if (Test-Path -LiteralPath $targetDirectoryPath) {
		throw "Cannot rename directory '$($directory.FullName)' because target '$targetDirectoryPath' already exists."
	}

	if ($PSCmdlet.ShouldProcess($directory.FullName, "Rename directory to '$newDirectoryName'")) {
		Rename-Item -LiteralPath $directory.FullName -NewName $newDirectoryName
		$directoriesRenamed++
	}
}

# 2) Renomeia arquivos.
$filesForRename = @(
	Get-ChildItem -LiteralPath $resolvedDestination -File -Recurse |
		Where-Object {
			$_.FullName -notmatch '[\\/](bin|obj|\.git|\.vs|\.idea|node_modules|packages)[\\/]'
		}
)

foreach ($file in $filesForRename) {
	$newFileName = Replace-TextCaseInsensitive -InputText $file.Name -OldText $SearchText -NewText $ReplaceText

	if ($newFileName -ceq $file.Name) {
		continue
	}

	$targetFilePath = Join-Path -Path $file.DirectoryName -ChildPath $newFileName

	if (Test-Path -LiteralPath $targetFilePath) {
		throw "Cannot rename file '$($file.FullName)' because target '$targetFilePath' already exists."
	}

	if ($PSCmdlet.ShouldProcess($file.FullName, "Rename file to '$newFileName'")) {
		Rename-Item -LiteralPath $file.FullName -NewName $newFileName
		$filesRenamed++
	}
}

# 3) Substitui conteúdo em arquivos de texto conhecidos.
$filesForContent = @(
	Get-ChildItem -LiteralPath $resolvedDestination -File -Recurse |
		Where-Object {
			$_.FullName -notmatch '[\\/](bin|obj|\.git|\.vs|\.idea|node_modules|packages)[\\/]'
		}
)

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

foreach ($file in $filesForContent) {
	if (-not (Should-ProcessFileContent -File $file -Extensions $IncludeExtensions)) {
		continue
	}

	$originalText = [System.IO.File]::ReadAllText($file.FullName)
	$updatedText = Replace-TextCaseInsensitive -InputText $originalText -OldText $SearchText -NewText $ReplaceText

	if ($updatedText -ceq $originalText) {
		continue
	}

	if ($PSCmdlet.ShouldProcess($file.FullName, "Replace content occurrences of '$SearchText'")) {
		[System.IO.File]::WriteAllText($file.FullName, $updatedText, $utf8NoBom)
		$filesUpdated++
	}
}

Write-Host ""
Write-Host "Replacement completed in: $resolvedDestination"
Write-Host "Directories renamed : $directoriesRenamed"
Write-Host "Files renamed       : $filesRenamed"
Write-Host "Files updated       : $filesUpdated"
Write-Host ""
