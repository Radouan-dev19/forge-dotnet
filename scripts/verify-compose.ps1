[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composePath = Join-Path $repositoryRoot 'docker-compose.yml'
$dockerfilePath = Join-Path $repositoryRoot 'src\ForgeDotNet.Web\Dockerfile'
$assertionCount = 0

function Assert-ComposeCondition {
    param(
        [Parameter(Mandatory)]
        [bool] $Condition,

        [Parameter(Mandatory)]
        [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }

    $script:assertionCount++
}

Push-Location $repositoryRoot

try {
    $configurationOutput = & docker compose --file $composePath config --format json
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose config failed with exit code $LASTEXITCODE."
    }

    $configurationJson = $configurationOutput -join [Environment]::NewLine
    $configuration = $configurationJson | ConvertFrom-Json
    $serviceProperties = @($configuration.services.PSObject.Properties)

    Assert-ComposeCondition ($serviceProperties.Count -eq 1) 'Compose must define exactly one service.'
    Assert-ComposeCondition ($serviceProperties[0].Name -eq 'forge-dotnet') "The only service must be named 'forge-dotnet'."

    $service = $configuration.services.'forge-dotnet'
    $ports = @($service.ports)
    Assert-ComposeCondition ($ports.Count -eq 1) 'The Web service must publish exactly one port.'
    Assert-ComposeCondition ($ports[0].host_ip -eq '127.0.0.1') 'The Web port must bind only to IPv4 loopback.'
    Assert-ComposeCondition ([int]$ports[0].target -eq 8080) 'The container port must be 8080.'
    Assert-ComposeCondition ([int]$ports[0].published -ge 1 -and [int]$ports[0].published -le 65535) 'The published port must be valid.'

    Assert-ComposeCondition ($service.read_only -eq $true) 'The container root filesystem must be read-only.'
    $privilegedProperty = $service.PSObject.Properties['privileged']
    Assert-ComposeCondition ($null -eq $privilegedProperty -or $privilegedProperty.Value -eq $false) 'The container must not be privileged.'
    Assert-ComposeCondition (@($service.cap_drop) -contains 'ALL') 'All Linux capabilities must be dropped.'
    Assert-ComposeCondition (@($service.security_opt) -contains 'no-new-privileges:true') 'no-new-privileges must be enabled.'
    Assert-ComposeCondition ($service.init -eq $true) 'The container init process must be enabled.'

    $mounts = @($service.volumes)
    Assert-ComposeCondition ($mounts.Count -eq 1) 'Exactly one persistent mount is expected.'
    Assert-ComposeCondition ($mounts[0].type -eq 'volume') 'Progression data must use a named volume.'
    Assert-ComposeCondition ($mounts[0].target -eq '/var/lib/forge-dotnet') 'The data volume target is unexpected.'

    $environmentProperties = @($service.environment.PSObject.Properties)
    $environmentNames = @($environmentProperties.Name | Sort-Object)
    $allowedEnvironmentNames = @(
        'ASPNETCORE_ENVIRONMENT',
        'ASPNETCORE_HTTP_PORTS',
        'CodeRunner__Mode',
        'DOTNET_EnableDiagnostics',
        'LocalData__DirectoryPath',
        'Web__UseHttpsRedirection'
    ) | Sort-Object
    Assert-ComposeCondition (($environmentNames -join '|') -eq ($allowedEnvironmentNames -join '|')) 'Compose exposes an unexpected environment variable.'
    Assert-ComposeCondition ($service.environment.ASPNETCORE_ENVIRONMENT -eq 'Production') 'Compose must use the Production environment.'
    Assert-ComposeCondition ($service.environment.CodeRunner__Mode -eq 'Manual') 'Compose must keep code execution in honest manual mode without a Docker socket.'
    Assert-ComposeCondition ($service.environment.LocalData__DirectoryPath -eq '/var/lib/forge-dotnet/data') 'The SQLite directory must remain inside the named volume.'

    Assert-ComposeCondition ($null -ne $service.healthcheck) 'The Web service must define a healthcheck.'
    Assert-ComposeCondition ((@($service.healthcheck.test) -join ' ') -match '127\.0\.0\.1:8080/health') 'The healthcheck must target the local health endpoint.'

    Assert-ComposeCondition ($configurationJson -notmatch '(?i)docker\.sock') 'The Docker socket must never be mounted.'
    Assert-ComposeCondition ($configurationJson -notmatch '(?i)sqlserver|mssql') 'Compose must not anticipate SqlLab.'
    Assert-ComposeCondition ($configurationJson -notmatch '(?i)(password|secret|token)') 'Compose must not contain secret-like configuration.'

    $dockerfile = Get-Content -Raw -Encoding UTF8 $dockerfilePath
    $runtimeStage = ($dockerfile -split '(?m)^FROM ')[-1]
    Assert-ComposeCondition ($dockerfile -match 'dotnet/sdk:10\.0\.302-alpine3\.23@sha256:[a-f0-9]{64}') 'The SDK build image must be pinned by digest.'
    Assert-ComposeCondition ($runtimeStage -match '^mcr\.microsoft\.com/dotnet/aspnet:10\.0-alpine3\.23@sha256:[a-f0-9]{64} AS runtime') 'The runtime image must be ASP.NET-only and pinned by digest.'
    Assert-ComposeCondition ($runtimeStage -notmatch 'dotnet/sdk') 'The final runtime stage must not contain the SDK image.'
    Assert-ComposeCondition ($runtimeStage -match '(?m)^USER \$APP_UID\r?$') 'The runtime container must use the built-in non-root user.'

    Write-Output "Compose configuration checks passed: $assertionCount assertions."
}
finally {
    Pop-Location
}
