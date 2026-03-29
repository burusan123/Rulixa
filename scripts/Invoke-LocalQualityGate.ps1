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

$repoRoot = Split-Path -Parent $PSScriptRoot
$phase5Root = Join-Path $repoRoot 'artifacts\local-quality'
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')

$originalOutputRoot = $env:RULIXA_LOCAL_QUALITY_OUTPUT_ROOT
$originalRunId = $env:RULIXA_LOCAL_QUALITY_RUN_ID
$originalSmoke = $env:RULIXA_RUN_ASSESSMEISTER_SMOKE

try {
    $env:RULIXA_LOCAL_QUALITY_OUTPUT_ROOT = $phase5Root
    $env:RULIXA_LOCAL_QUALITY_RUN_ID = $runId

    if ($IncludeOptionalSmoke) {
        $env:RULIXA_RUN_ASSESSMEISTER_SMOKE = '1'
    }
    else {
        Remove-Item Env:RULIXA_RUN_ASSESSMEISTER_SMOKE -ErrorAction SilentlyContinue
    }

    Push-Location $repoRoot

    Invoke-DotNet -Arguments @('build', '.\Rulixa.sln')
    Invoke-DotNet -Arguments @(
        'test',
        '.\tests\Rulixa.Application.Tests\Rulixa.Application.Tests.csproj',
        '--no-build',
        '--filter',
        'FullyQualifiedName~CompareEvidenceBundleTests|FullyQualifiedName~LocalQualityGateRunWriterTests')
    Invoke-DotNet -Arguments @(
        'test',
        '.\tests\Rulixa.Plugin.WpfNet8.Tests\Rulixa.Plugin.WpfNet8.Tests.csproj',
        '--no-build',
        '--filter',
        'FullyQualifiedName~AcceptanceMatrixTests|FullyQualifiedName~QualityArtifactTests|FullyQualifiedName~LocalQualityGateRunnerTests|FullyQualifiedName~AssessMeisterOptionalSmokeTests|FullyQualifiedName~AssessMeisterLegacyOptionalSmokeTests')

    $summaryPath = Join-Path $phase5Root "$runId\summary.md"
    $gatePath = Join-Path $phase5Root "$runId\gate.json"
    $kpiPath = Join-Path $phase5Root "$runId\kpi.json"

    Write-Host "Local quality gate completed."
    Write-Host "RunId: $runId"
    Write-Host "Summary: $summaryPath"
    Write-Host "Gate: $gatePath"
    Write-Host "KPI: $kpiPath"
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
        $env:RULIXA_RUN_ASSESSMEISTER_SMOKE = $originalSmoke
    }
    else {
        Remove-Item Env:RULIXA_RUN_ASSESSMEISTER_SMOKE -ErrorAction SilentlyContinue
    }
}
