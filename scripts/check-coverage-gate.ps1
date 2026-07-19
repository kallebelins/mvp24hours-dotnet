param(
    [Parameter(Mandatory = $true)]
    [string]$SummaryJsonPath,

    [double]$MinimumLineCoverage = 37,

    [double]$TargetLineCoverage = 95
)

$summary = Get-Content $SummaryJsonPath -Raw | ConvertFrom-Json
$lineCoverage = [double]$summary.summary.linecoverage

Write-Host "Coverage gate: line $([math]::Round($lineCoverage, 1))% (minimum $MinimumLineCoverage%, target $TargetLineCoverage%)"

if ($lineCoverage -lt $MinimumLineCoverage) {
    Write-Error "Line coverage $([math]::Round($lineCoverage, 1))% is below minimum threshold $MinimumLineCoverage%."
    exit 1
}

if ($lineCoverage -lt $TargetLineCoverage) {
    Write-Warning "Target line coverage $TargetLineCoverage% not yet reached (current $([math]::Round($lineCoverage, 1))%). Regression gate passed."
}

Write-Host "Coverage gate passed."
exit 0
