[CmdletBinding()]
param(
    [string]$ImageReference = 'forge-dotnet-runner:test',
    [string]$DockerContext = 'desktop-linux',
    [string]$ScanTimeout = '15m',
    [double]$ScannerCpuCount = 1
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scannerReference = 'aquasec/trivy@sha256:be1190afcb28352bfddc4ddeb71470835d16462af68d310f9f4bca710961a41e'
$expectedScannerImageId = 'sha256:c0a2b004a57047aff2bc7a8b87d693d368ba40cd10ef9bb1213345f043f416dd'
$scanIdentifier = [Guid]::NewGuid().ToString('N').Substring(0, 12)
$cacheVolume = 'forge-trivy-cache-' + $scanIdentifier
$workVolume = 'forge-trivy-work-' + $scanIdentifier
$downloadContainer = 'forge-trivy-db-' + [Guid]::NewGuid().ToString('N').Substring(0, 12)
$scanContainer = 'forge-trivy-scan-' + [Guid]::NewGuid().ToString('N').Substring(0, 12)
$cacheVolumeCreated = $false
$workVolumeCreated = $false

function Invoke-Docker {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & docker --context $DockerContext @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Docker a échoué avec le code $LASTEXITCODE : $($Arguments -join ' ')"
    }
}

function Test-DockerContainerExists {
    param([Parameter(Mandatory)][string]$Name)

    $containerIds = @(& docker --context $DockerContext container ls --all --quiet --filter "name=^/$Name$")
    if ($LASTEXITCODE -ne 0) {
        throw "Docker n’a pas pu contrôler le conteneur temporaire $Name."
    }

    return $containerIds.Count -gt 0
}

if ($DockerContext -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$') {
    throw 'Le contexte Docker est invalide.'
}

if ($ScanTimeout -notmatch '^[1-9][0-9]?m$') {
    throw 'Le timeout Trivy doit être exprimé entre 1m et 99m.'
}

if ($ScannerCpuCount -lt 0.5 -or $ScannerCpuCount -gt 4) {
    throw 'Le quota CPU du scanner Trivy doit rester compris entre 0,5 et 4.'
}

$scannerCpuArgument = $ScannerCpuCount.ToString([Globalization.CultureInfo]::InvariantCulture)

try {
    Write-Host 'Vérification du moteur et de l’image runner...'
    Invoke-Docker -Arguments @('version', '--format', '{{.Server.Version}}')
    Invoke-Docker -Arguments @('image', 'inspect', $ImageReference, '--format', '{{.Id}}')

    $localScannerIds = @(& docker --context $DockerContext image ls --quiet $scannerReference)
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker n’a pas pu rechercher l’image Trivy épinglée.'
    }
    if ($localScannerIds.Count -eq 0) {
        Write-Host 'Téléchargement de l’image Trivy épinglée...'
        Invoke-Docker -Arguments @('pull', $scannerReference)
    }
    else {
        Write-Host 'Réutilisation de l’image Trivy épinglée déjà présente.'
    }

    $scannerId = (& docker --context $DockerContext image inspect $scannerReference --format '{{.Id}}').Trim()
    if ($LASTEXITCODE -ne 0 -or $scannerId -ne $expectedScannerImageId) {
        throw 'L’image Trivy téléchargée ne correspond pas à l’identifiant amd64 approuvé.'
    }

    Invoke-Docker -Arguments @('volume', 'create', '--label', 'forge-dotnet.scan=true', $cacheVolume)
    $cacheVolumeCreated = $true
    Invoke-Docker -Arguments @('volume', 'create', '--label', 'forge-dotnet.scan=true', $workVolume)
    $workVolumeCreated = $true

    Write-Host 'Téléchargement isolé de la base CVE...'
    Invoke-Docker -Arguments @(
        'run', '--rm', '--name', $downloadContainer,
        '--label', 'forge-dotnet.scan=true',
        '--network', 'bridge',
        '--read-only',
        '--cap-drop', 'ALL',
        '--security-opt', 'no-new-privileges=true',
        '--security-opt', 'seccomp=builtin',
        '--memory', '1073741824',
        '--memory-swap', '1073741824',
        '--cpus', $scannerCpuArgument,
        '--pids-limit', '128',
        '--mount', "type=volume,src=$cacheVolume,dst=/root/.cache,volume-nocopy",
        '--tmpfs', '/tmp:rw,nosuid,nodev,noexec,size=268435456',
        $scannerId,
        'image', '--download-db-only', '--no-progress', '--disable-telemetry'
    )

    Write-Host 'Analyse hors ligne de l’image runner...'
    Invoke-Docker -Arguments @(
        'run', '--rm', '--name', $scanContainer,
        '--label', 'forge-dotnet.scan=true',
        '--network', 'none',
        '--read-only',
        '--cap-drop', 'ALL',
        '--security-opt', 'no-new-privileges=true',
        '--security-opt', 'seccomp=builtin',
        '--memory', '1073741824',
        '--memory-swap', '1073741824',
        '--cpus', $scannerCpuArgument,
        '--pids-limit', '128',
        '--mount', 'type=bind,src=/var/run/docker.sock,dst=/var/run/docker.sock',
        '--mount', "type=volume,src=$cacheVolume,dst=/root/.cache,volume-nocopy",
        '--mount', "type=volume,src=$workVolume,dst=/tmp,volume-nocopy",
        '--env', 'DOCKER_HOST=unix:///var/run/docker.sock',
        $scannerId,
        'image',
        '--timeout', $ScanTimeout,
        '--skip-db-update',
        '--skip-java-db-update',
        '--offline-scan',
        '--skip-vex-repo-update',
        '--disable-telemetry',
        '--image-src', 'docker',
        '--scanners', 'vuln',
        '--severity', 'CRITICAL',
        '--exit-code', '1',
        '--no-progress',
        $ImageReference
    )
}
finally {
    if (Test-DockerContainerExists -Name $downloadContainer) {
        Invoke-Docker -Arguments @('rm', '--force', $downloadContainer)
    }
    if (Test-DockerContainerExists -Name $scanContainer) {
        Invoke-Docker -Arguments @('rm', '--force', $scanContainer)
    }

    if ($cacheVolumeCreated) {
        Invoke-Docker -Arguments @('volume', 'rm', '--force', $cacheVolume)
    }
    if ($workVolumeCreated) {
        Invoke-Docker -Arguments @('volume', 'rm', '--force', $workVolume)
    }
}
