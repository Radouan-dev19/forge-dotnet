[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $Path = 'content/reference',

    [string] $Search = '',

    [string] $Skill = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$arguments = @(
    'run',
    '--project', 'src/ForgeDotNet.Web/ForgeDotNet.Web.csproj',
    '--no-build',
    '--no-launch-profile',
    '--',
    '--load-catalog', $Path
)
if (-not [string]::IsNullOrWhiteSpace($Search)) {
    $arguments += @('--search', $Search)
}
if (-not [string]::IsNullOrWhiteSpace($Skill)) {
    $arguments += @('--skill', $Skill)
}

Push-Location $repositoryRoot
try {
    & dotnet $arguments
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
