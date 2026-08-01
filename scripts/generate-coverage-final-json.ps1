param(
    [Parameter(Mandatory = $true)]
    [string]$SummaryJsonPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [int]$Passed = 4492,
    [int]$Failed = 0,
    [int]$Skipped = 6,
    [double]$TargetLineCoverage = 95
)

$summary = Get-Content $SummaryJsonPath -Raw | ConvertFrom-Json
$s = $summary.summary

$assemblies = @($summary.coverage.assemblies | ForEach-Object {
    @{
        name               = $_.name
        hasCoverageData    = $true
        lineCoverage       = [math]::Round($_.coverage, 1)
        coveredLines       = $_.coveredlines
        coverableLines     = $_.coverablelines
        branchCoverage     = if ($null -ne $_.branchcoverage) { [math]::Round($_.branchcoverage, 1) } else { $null }
        methodCoverage     = if ($null -ne $_.methodcoverage) { [math]::Round($_.methodcoverage, 1) } else { $null }
        coveredMethods     = $_.coveredmethods
        totalMethods       = $_.totalmethods
        classCount         = $_.classes
    }
})

$productionAssemblies = @($assemblies | Where-Object { $_.name -notmatch '\.Test$' })
$withData = $productionAssemblies.Count

$result = [ordered]@{
    generatedAt = (Get-Date).ToUniversalTime().ToString('o')
    source      = [ordered]@{
        command           = 'dotnet test src/Mvp24Hours.slnx --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./test-results'
        reportGenerator   = 'reportgenerator -reports:"test-results/**/coverage.cobertura.xml" -targetdir:"./test-results/coverage-report-final" -reporttypes:"Html;JsonSummary;Cobertura" -assemblyfilters:"+Mvp24Hours*"'
        htmlReport        = 'tasks/coverage-final-report.html'
        jsonSummary       = 'test-results/coverage-report-final/Summary.json'
        mergedCobertura   = 'test-results/coverage-report-final/Cobertura.xml'
        coverletFiles     = 18
        runSettings       = 'coverlet.runsettings'
        testDirectoryBuild = 'src/Tests/Directory.Build.props'
    }
    testRun     = [ordered]@{
        passed = $Passed
        failed = $Failed
        skipped = $Skipped
        total  = $Passed + $Failed + $Skipped
        note   = '18 test projects; 6 skipped BulkOperations InMemory tests (EFCore + SQLServer).'
    }
    summary     = [ordered]@{
        lineCoverage                           = [math]::Round($s.linecoverage, 1)
        coveredLines                           = $s.coveredlines
        coverableLines                         = $s.coverablelines
        branchCoverage                         = [math]::Round($s.branchcoverage, 1)
        coveredBranches                        = $s.coveredbranches
        totalBranches                          = $s.totalbranches
        methodCoverage                         = [math]::Round($s.methodcoverage, 1)
        coveredMethods                         = $s.coveredmethods
        totalMethods                           = $s.totalmethods
        assembliesWithCoverageData             = $withData
        productionProjectsTotal                = 12
        productionProjectsWithoutCoverageData    = [math]::Max(0, 12 - $withData)
        targetLineCoverage                     = $TargetLineCoverage
        targetMet                              = ($s.linecoverage -ge $TargetLineCoverage)
    }
    assemblies  = $productionAssemblies
}

$result | ConvertTo-Json -Depth 6 | Set-Content -Path $OutputPath -Encoding UTF8

Write-Host "Line coverage: $($s.linecoverage)% (target $TargetLineCoverage%, met: $($result.summary.targetMet))"
