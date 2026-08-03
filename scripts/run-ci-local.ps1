#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the same gates as CI/CD and Samples CI locally.

.DESCRIPTION
    Mirrors .github/workflows/ci.yml and .github/workflows/samples-ci.yml.
    Prerequisites: .NET 10 SDK, Docker Desktop (for integration tests).

.PARAMETER SkipSamples
    Skip Samples CI steps.

.PARAMETER SkipCoverage
    Skip coverage collection and gate (faster iteration).

.PARAMETER SkipIntegration
    Skip integration tests (no Docker required).

.PARAMETER SkipFormat
    Skip dotnet format verification.

.PARAMETER SkipWarningsGate
    Skip TreatWarningsAsErrors build.

.PARAMETER MinimumLineCoverage
    Coverage floor (default 75 — Phase 3 gate).

.EXAMPLE
    ./scripts/run-ci-local.ps1
    ./scripts/run-ci-local.ps1 -SkipSamples -SkipIntegration
#>
param(
    [switch]$SkipSamples,
    [switch]$SkipCoverage,
    [switch]$SkipIntegration,
    [switch]$SkipFormat,
    [switch]$SkipWarningsGate,
    [double]$MinimumLineCoverage = 75
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )
    Write-Host "`n=== $Name ===" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "Step failed: $Name (exit code $LASTEXITCODE)"
    }
}

function Invoke-TestProjectsWithCoverage {
    param(
        [string]$Filter,
        [string]$ResultsDirectory
    )

    if (Test-Path $ResultsDirectory) {
        Remove-Item $ResultsDirectory -Recurse -Force
    }

    New-Item -ItemType Directory -Force $ResultsDirectory | Out-Null

    $testProjects = Get-ChildItem src/Tests -Filter *.csproj -Recurse
    foreach ($project in $testProjects) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project.Name)
        Write-Host "  -> $projectName" -ForegroundColor DarkGray

        $attempt = 0
        $maxAttempts = 2
        do {
            $attempt++
            if ($attempt -gt 1) {
                Write-Host "    retry $attempt/$maxAttempts after testhost failure" -ForegroundColor Yellow
                [System.GC]::Collect()
                [System.GC]::WaitForPendingFinalizers()
                Start-Sleep -Seconds 2
            }

            dotnet test $project.FullName `
                --configuration Release `
                --no-build `
                --filter $Filter `
                --verbosity quiet `
                --settings coverlet.runsettings `
                --collect:"XPlat Code Coverage" `
                --results-directory (Join-Path $ResultsDirectory $projectName)
        } while ($LASTEXITCODE -ne 0 -and $attempt -lt $maxAttempts)

        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed in $projectName (exit code $LASTEXITCODE)"
        }

        [System.GC]::Collect()
        [System.GC]::WaitForPendingFinalizers()
    }
}

try {
    Invoke-Step 'Restore main solution' {
        dotnet restore src/Mvp24Hours.slnx
    }

    Invoke-Step 'Build main solution (Release)' {
        dotnet build src/Mvp24Hours.slnx --configuration Release --no-restore
    }

    if ($SkipCoverage) {
        Invoke-Step 'Unit tests (no coverage)' {
            dotnet test src/Mvp24Hours.slnx `
                --configuration Release `
                --no-build `
                --filter "Category!=Integration" `
                --verbosity minimal
        }

        if (-not $SkipIntegration) {
            Invoke-Step 'Integration tests (no coverage)' {
                dotnet test src/Mvp24Hours.slnx `
                    --configuration Release `
                    --no-build `
                    --filter "Category=Integration" `
                    --verbosity minimal
            }
        }
    }
    else {
        Invoke-Step 'Unit tests (with coverage)' {
            Invoke-TestProjectsWithCoverage -Filter "Category!=Integration" -ResultsDirectory ./test-results/unit
        }

        if (-not $SkipIntegration) {
            Invoke-Step 'Integration tests (with coverage)' {
                Invoke-TestProjectsWithCoverage -Filter "Category=Integration" -ResultsDirectory ./test-results/integration
            }
        }

        Invoke-Step 'Coverage regression gate' {
            dotnet tool install -g dotnet-reportgenerator-globaltool --ignore-failed-sources 2>$null
            $reports = @()
            if (Test-Path ./test-results/unit) {
                $reports += Get-ChildItem ./test-results/unit -Recurse -Filter coverage.cobertura.xml | ForEach-Object { $_.FullName }
            }
            if ((-not $SkipIntegration) -and (Test-Path ./test-results/integration)) {
                $reports += Get-ChildItem ./test-results/integration -Recurse -Filter coverage.cobertura.xml | ForEach-Object { $_.FullName }
            }
            if ($reports.Count -eq 0) {
                throw 'No coverage.cobertura.xml files found.'
            }
            & "$PSScriptRoot/merge-coverage-report.ps1" `
                -ReportPaths $reports `
                -TargetDir ./test-results/coverage-report
            & "$PSScriptRoot/check-coverage-gate.ps1" `
                -SummaryJsonPath ./test-results/coverage-report/Summary.json `
                -MinimumLineCoverage $MinimumLineCoverage `
                -TargetLineCoverage 95
        }
    }

    if (-not $SkipFormat) {
        Invoke-Step 'Code format verification (PR gate)' {
            dotnet format src/Mvp24Hours.slnx `
                --exclude-diagnostics IDE0130 IDE1006 `
                --verify-no-changes `
                --verbosity diagnostic
        }
    }

    if (-not $SkipWarningsGate) {
        Invoke-Step 'TreatWarningsAsErrors build (PR gate)' {
            dotnet build src/Mvp24Hours.slnx `
                --configuration Release `
                --no-incremental `
                /p:TreatWarningsAsErrors=true
        }
    }

    if (-not $SkipSamples) {
        Invoke-Step 'Restore samples solution' {
            dotnet restore samples/Mvp24Hours.Samples.slnx
        }

        Invoke-Step 'Build samples solution (Release)' {
            dotnet build samples/Mvp24Hours.Samples.slnx --configuration Release --no-restore
        }

        Invoke-Step 'Samples unit tests (Category=Unit)' {
            dotnet test samples/Mvp24Hours.Samples.slnx `
                --configuration Release `
                --no-build `
                --filter "Category=Unit" `
                --verbosity minimal
        }

        Invoke-Step 'Samples integration tests (Category=Integration)' {
            dotnet test samples/Mvp24Hours.Samples.slnx `
                --configuration Release `
                --no-build `
                --filter "Category=Integration" `
                --verbosity minimal
        }
    }

    Write-Host "`nAll local CI gates passed." -ForegroundColor Green
}
finally {
    Pop-Location
}
