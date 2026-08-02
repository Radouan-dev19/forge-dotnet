[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $Path = 'content/fixtures/valid'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot

try {
    & dotnet run --project src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --no-build --no-launch-profile -- --validate-content $Path
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
