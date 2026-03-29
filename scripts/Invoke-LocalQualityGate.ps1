param(
    [switch]$IncludeOptionalSmoke
)

$ErrorActionPreference = 'Stop'

function Invoke-DotNet {
    param(
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed: dotnet $($Arguments -join ' ')"
    }
}

function Invoke-DotNetObservation {
    param(
        [string[]]$Arguments
    )

    & dotnet @Arguments
    return $LASTEXITCODE
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$qualityRoot = Join-Path $repoRoot 'artifacts\local-quality'
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')

$originalOutputRoot = $env:RULIXA_LOCAL_QUALITY_OUTPUT_ROOT
$originalRunId = $env:RULIXA_LOCAL_QUALITY_RUN_ID
$originalSmoke = $env:RULIXA_RUN_OPTIONAL_SMOKE

try {
    $env:RULIXA_LOCAL_QUALITY_OUTPUT_ROOT = $qualityRoot
    $env:RULIXA_LOCAL_QUALITY_RUN_ID = $runId

    if ($IncludeOptionalSmoke) {
        $env:RULIXA_RUN_OPTIONAL_SMOKE = '1'
    }
    else {
        Remove-Item Env:RULIXA_RUN_OPTIONAL_SMOKE -ErrorAction SilentlyContinue
    }

    Push-Location $repoRoot

    Invoke-DotNet -Arguments @('build', '.\Rulixa.sln')
    Invoke-DotNet -Arguments @(
        'test',
        '.\tests\Rulixa.Application.Tests\Rulixa.Application.Tests.csproj',
        '--no-build',
        '--filter',
        'FullyQualifiedName~CompareEvidenceBundleTests|FullyQualifiedName~LocalQualityGateRunWriterTests|FullyQualifiedName~QualityHandoffOutcomeEvaluatorTests|FullyQualifiedName~PublicDocsHardeningTests')
    Invoke-DotNet -Arguments @(
        'test',
        '.\tests\Rulixa.Plugin.WpfNet8.Tests\Rulixa.Plugin.WpfNet8.Tests.csproj',
        '--no-build',
        '--filter',
        'FullyQualifiedName~AcceptanceMatrixTests|FullyQualifiedName~QualityArtifactTests|FullyQualifiedName~LocalQualityGateRunnerTests|FullyQualifiedName~HandoffQualityTests')

    if ($IncludeOptionalSmoke) {
        $optionalSmokeExitCode = Invoke-DotNetObservation -Arguments @(
            'test',
            '.\tests\Rulixa.Plugin.WpfNet8.Tests\Rulixa.Plugin.WpfNet8.Tests.csproj',
            '--no-build',
            '--filter',
            'FullyQualifiedName~RealWorkspaceOptionalSmokeTests|FullyQualifiedName~LegacyRealWorkspaceOptionalSmokeTests')
        if ($optionalSmokeExitCode -ne 0) {
            Write-Warning 'Optional smoke tests failed. They remain observation-only and do not change gate result.'
        }
    }

    $summaryPath = Join-Path $qualityRoot "$runId\summary.md"
    $gatePath = Join-Path $qualityRoot "$runId\gate.json"
    $kpiPath = Join-Path $qualityRoot "$runId\kpi.json"
    $gate = Get-Content -Path $gatePath -Raw | ConvertFrom-Json

    Write-Host "Local quality gate completed."
    Write-Host "RunId: $runId"
    Write-Host "Summary: $summaryPath"
    Write-Host "Gate: $gatePath"
    Write-Host "KPI: $kpiPath"

    if (-not $gate.passed) {
        throw "quality gate failed: $($gate.failedChecks -join ', ')"
    }
}
finally {
    Pop-Location

    if ($null -ne $originalOutputRoot) {
        $env:RULIXA_LOCAL_QUALITY_OUTPUT_ROOT = $originalOutputRoot
    }
    else {
        Remove-Item Env:RULIXA_LOCAL_QUALITY_OUTPUT_ROOT -ErrorAction SilentlyContinue
    }

    if ($null -ne $originalRunId) {
        $env:RULIXA_LOCAL_QUALITY_RUN_ID = $originalRunId
    }
    else {
        Remove-Item Env:RULIXA_LOCAL_QUALITY_RUN_ID -ErrorAction SilentlyContinue
    }

    if ($null -ne $originalSmoke) {
        $env:RULIXA_RUN_OPTIONAL_SMOKE = $originalSmoke
    }
    else {
        Remove-Item Env:RULIXA_RUN_OPTIONAL_SMOKE -ErrorAction SilentlyContinue
    }
}
