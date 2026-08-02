[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot

try {
    & dotnet run --project src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --no-build --no-launch-profile -- `
        --load-catalog content/reference `
        --search evaluer `
        --skill csharp.types `
        --reload-from content/fixtures/invalid/missing-required
    if ($LASTEXITCODE -ne 1) {
        throw "catalog reload smoke test returned exit code $LASTEXITCODE (expected 1)."
    }

    Write-Host 'Catalogue smoke test passed: invalid reload refused and previous snapshot preserved.'
}
finally {
    Pop-Location
}
