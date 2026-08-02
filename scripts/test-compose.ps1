[CmdletBinding()]
param(
    [string] $BaseUri = 'http://localhost:5012'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$assertionCount = 0

function Assert-RuntimeCondition {
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

function Get-HttpText {
    param(
        [Parameter(Mandatory)]
        [string] $Uri
    )

    Add-Type -AssemblyName System.Net.Http
    $client = New-Object System.Net.Http.HttpClient
    $client.Timeout = [TimeSpan]::FromSeconds(20)

    try {
        $response = $client.GetAsync($Uri).GetAwaiter().GetResult()
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        return [PSCustomObject]@{
            StatusCode = [int]$response.StatusCode
            Content = $content
        }
    }
    finally {
        $client.Dispose()
    }
}

Push-Location $repositoryRoot

try {
    $containerIds = @(& docker compose ps --quiet forge-dotnet)
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose ps failed with exit code $LASTEXITCODE."
    }

    Assert-RuntimeCondition ($containerIds.Count -eq 1) 'Exactly one running Forge.NET container is expected.'
    $containerId = $containerIds[0]
    $inspectOutput = & docker inspect $containerId
    if ($LASTEXITCODE -ne 0) {
        throw "docker inspect failed with exit code $LASTEXITCODE."
    }

    $inspection = (($inspectOutput -join [Environment]::NewLine) | ConvertFrom-Json)[0]
    Assert-RuntimeCondition ($inspection.State.Status -eq 'running') 'The Forge.NET container must be running.'
    Assert-RuntimeCondition ($inspection.State.Health.Status -eq 'healthy') 'The Forge.NET container must be healthy.'
    Assert-RuntimeCondition ($inspection.Config.User -eq '1654') 'The runtime user must be the built-in non-root UID 1654.'
    Assert-RuntimeCondition ($inspection.HostConfig.ReadonlyRootfs -eq $true) 'The effective root filesystem must be read-only.'
    Assert-RuntimeCondition ($inspection.HostConfig.Privileged -eq $false) 'The effective container must not be privileged.'
    Assert-RuntimeCondition (@($inspection.HostConfig.CapDrop) -contains 'ALL') 'All effective Linux capabilities must be dropped.'
    Assert-RuntimeCondition (@($inspection.HostConfig.SecurityOpt) -contains 'no-new-privileges:true') 'The effective container must disable privilege escalation.'
    $bindsProperty = $inspection.HostConfig.PSObject.Properties['Binds']
    Assert-RuntimeCondition ($null -eq $bindsProperty -or $null -eq $bindsProperty.Value -or @($bindsProperty.Value).Count -eq 0) 'No host bind mount is allowed.'

    $portBindings = @($inspection.HostConfig.PortBindings.'8080/tcp')
    Assert-RuntimeCondition (@($inspection.HostConfig.PortBindings.PSObject.Properties).Count -eq 1) 'Exactly one container port binding is expected.'
    Assert-RuntimeCondition ($portBindings.Count -eq 1) 'Port 8080 must have exactly one host binding.'
    Assert-RuntimeCondition ($portBindings[0].HostIp -eq '127.0.0.1') 'The effective Web port must bind only to IPv4 loopback.'

    $mounts = @($inspection.Mounts)
    Assert-RuntimeCondition ($mounts.Count -eq 1) 'Exactly one effective mount is expected.'
    Assert-RuntimeCondition ($mounts[0].Type -eq 'volume') 'The effective data mount must be a Docker volume.'
    Assert-RuntimeCondition ($mounts[0].Destination -eq '/var/lib/forge-dotnet') 'The effective data volume destination is unexpected.'
    Assert-RuntimeCondition (($inspectOutput -join [Environment]::NewLine) -notmatch '(?i)docker\.sock') 'The Docker socket must not appear in the effective configuration.'

    $sdkList = @(& docker exec $containerId dotnet --list-sdks)
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet --list-sdks failed with exit code $LASTEXITCODE."
    }

    Assert-RuntimeCondition ($sdkList.Count -eq 0) 'The runtime image must not contain a .NET SDK.'

    $normalizedBaseUri = $BaseUri.TrimEnd('/')
    $health = Get-HttpText "$normalizedBaseUri/health"
    Assert-RuntimeCondition ($health.StatusCode -eq 200) 'The health endpoint must return HTTP 200.'
    Assert-RuntimeCondition ($health.Content -eq 'Healthy') "The health endpoint must return 'Healthy'."

    $blazorBootstrap = Get-HttpText "$normalizedBaseUri/_framework/blazor.web.js"
    Assert-RuntimeCondition ($blazorBootstrap.StatusCode -eq 200) 'The Blazor bootstrap script must return HTTP 200.'
    Assert-RuntimeCondition ($blazorBootstrap.Content.Length -gt 100000) 'The Blazor bootstrap script is unexpectedly small.'
    Assert-RuntimeCondition ($blazorBootstrap.Content -match 'Blazor') 'The Blazor bootstrap response is not the expected script.'

    $logs = @(& docker compose logs --no-color --tail 200 forge-dotnet)
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose logs failed with exit code $LASTEXITCODE."
    }

    $logText = $logs -join [Environment]::NewLine
    Assert-RuntimeCondition ($logText -notmatch '"LogLevel":"(Error|Critical)"') 'The current container logs contain an error or critical event.'

    Write-Output "Compose runtime checks passed: $assertionCount assertions."
}
finally {
    Pop-Location
}
