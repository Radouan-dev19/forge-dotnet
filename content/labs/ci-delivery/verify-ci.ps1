[CmdletBinding()]
param([switch]$SkipDocker)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
Push-Location $root
try {
    & dotnet restore content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj --disable-parallel
    if ($LASTEXITCODE -ne 0) { throw "Restauration API en échec : $LASTEXITCODE" }
    & dotnet build content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Compilation API en échec : $LASTEXITCODE" }
    & dotnet test content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj --no-build --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Tests API en échec : $LASTEXITCODE" }
    & dotnet test content/labs/testing-strategy/ForgeTestingLab.Tests/ForgeTestingLab.Tests.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Tests de stratégie en échec : $LASTEXITCODE" }
    if (-not $SkipDocker) {
        & docker build --file content/labs/container-delivery/Dockerfile --tag forge-api-lab:s11-s20 .
        if ($LASTEXITCODE -ne 0) { throw "Construction Docker en échec : $LASTEXITCODE" }
    }
}
finally { Pop-Location }
