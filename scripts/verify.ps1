[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$verificationFailure = $null
$sqlLabManaged = $false
Push-Location $repositoryRoot

try {
    & dotnet restore ForgeDotNet.sln --disable-parallel
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

    & dotnet build ForgeDotNet.sln --no-restore --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE." }

    $testProjects = @(
        'tests/ForgeDotNet.UnitTests/ForgeDotNet.UnitTests.csproj',
        'tests/ForgeDotNet.EndToEndTests/ForgeDotNet.EndToEndTests.csproj'
    )

    foreach ($testProject in $testProjects) {
        & dotnet test $testProject --no-build --no-restore --disable-build-servers --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw "dotnet test failed for $testProject with exit code $LASTEXITCODE." }
    }

    $integrationProject = 'tests/ForgeDotNet.IntegrationTests/ForgeDotNet.IntegrationTests.csproj'
    $sqlIntegrationFilter = @(
        'FullyQualifiedName~SqlLabSecurityTests',
        'FullyQualifiedName~SqlEfContentTests',
        'FullyQualifiedName~EverySqlExamSolutionPassesAndStarterFailsOnDisposableSqlLab'
    ) -join '|'
    $nonSqlIntegrationFilter = @(
        'FullyQualifiedName!~SqlLabSecurityTests',
        'FullyQualifiedName!~SqlEfContentTests',
        'FullyQualifiedName!~EverySqlExamSolutionPassesAndStarterFailsOnDisposableSqlLab'
    ) -join '&'

    & dotnet test $integrationProject --no-build --no-restore --disable-build-servers --filter $nonSqlIntegrationFilter --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw "non-SqlLab integration tests failed with exit code $LASTEXITCODE." }

    $sqlLabManaged = $true
    & powershell -ExecutionPolicy Bypass -File scripts/start-sql-lab.ps1
    if ($LASTEXITCODE -ne 0) { throw "SqlLab startup failed with exit code $LASTEXITCODE." }

    & dotnet test $integrationProject --no-build --no-restore --disable-build-servers --filter $sqlIntegrationFilter --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw "SqlLab integration tests failed with exit code $LASTEXITCODE." }

    & dotnet run --project src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --no-build --no-launch-profile -- --validate-content content/fixtures/valid
    if ($LASTEXITCODE -ne 0) { throw "valid content fixture validation failed with exit code $LASTEXITCODE." }

    & dotnet run --project src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --no-build --no-launch-profile -- --validate-content content/fixtures/invalid
    if ($LASTEXITCODE -ne 1) { throw "invalid content fixtures returned unexpected exit code $LASTEXITCODE (expected 1)." }

    & dotnet run --project src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --no-build --no-launch-profile -- --validate-content content/sql
    if ($LASTEXITCODE -ne 0) { throw "SQL/EF content validation failed with exit code $LASTEXITCODE." }

    & dotnet run --project src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --no-build --no-launch-profile -- --load-catalog content/reference --search evaluer --skill csharp.types
    if ($LASTEXITCODE -ne 0) { throw "reference catalog loading failed with exit code $LASTEXITCODE." }

    & dotnet run --project src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --no-build --no-launch-profile -- --load-catalog content/sql --search commandes --skill sql.join
    if ($LASTEXITCODE -ne 0) { throw "SQL/EF catalog loading failed with exit code $LASTEXITCODE." }

    & dotnet format ForgeDotNet.sln --no-restore --verify-no-changes
    if ($LASTEXITCODE -ne 0) { throw "dotnet format failed with exit code $LASTEXITCODE." }
}
catch {
    $verificationFailure = $_
}
finally {
    if ($sqlLabManaged) {
        try {
            & powershell -ExecutionPolicy Bypass -File scripts/stop-sql-lab.ps1
            if ($LASTEXITCODE -ne 0) { throw "SqlLab shutdown failed with exit code $LASTEXITCODE." }
        }
        catch {
            if ($null -eq $verificationFailure) { $verificationFailure = $_ }
            else { [Console]::Error.WriteLine("Additional SqlLab cleanup failure: $($_.Exception.Message)") }
        }
    }

    Pop-Location
}

if ($null -ne $verificationFailure) { throw $verificationFailure }
