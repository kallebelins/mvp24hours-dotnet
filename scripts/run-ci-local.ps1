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

.PARAMETER SkipFormat
    Skip dotnet format verification.

.PARAMETER SkipWarningsGate
    Skip TreatWarningsAsErrors build.

.EXAMPLE
    ./scripts/run-ci-local.ps1
    ./scripts/run-ci-local.ps1 -SkipSamples -SkipCoverage
#>
param(
    [switch]$SkipSamples,
    [switch]$SkipCoverage,
    [switch]$SkipFormat,
    [switch]$SkipWarningsGate
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

try {
    Invoke-Step 'Restore main solution' {
        dotnet restore src/Mvp24Hours.slnx
    }

    Invoke-Step 'Build main solution (Release)' {
        dotnet build src/Mvp24Hours.slnx --configuration Release --no-restore
    }

    if ($SkipCoverage) {
        Invoke-Step 'Test main solution (no coverage)' {
            dotnet test src/Mvp24Hours.slnx --configuration Release --no-build --verbosity minimal
        }
    }
    else {
        Invoke-Step 'Test main solution (with coverage)' {
            dotnet test src/Mvp24Hours.slnx `
                --configuration Release `
                --no-build `
                --verbosity minimal `
                --settings coverlet.runsettings `
                --collect:"XPlat Code Coverage" `
                --results-directory ./test-results
        }

        Invoke-Step 'Coverage regression gate' {
            dotnet tool install -g dotnet-reportgenerator-globaltool --ignore-failed-sources 2>$null
            reportgenerator `
                -reports:"test-results/**/coverage.cobertura.xml" `
                -targetdir:"./test-results/coverage-report" `
                -reporttypes:"JsonSummary" `
                -assemblyfilters:"+Mvp24Hours*"
            & "$PSScriptRoot/check-coverage-gate.ps1" `
                -SummaryJsonPath ./test-results/coverage-report/Summary.json `
                -MinimumLineCoverage 37 `
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
