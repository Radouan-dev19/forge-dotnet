[CmdletBinding()]
param(
    [switch]$IncludeWeb,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$secretDirectory = Join-Path $repositoryRoot '.secrets'
$secretPath = Join-Path $secretDirectory 'sql-lab-sa-password.txt'

if (-not (Test-Path -LiteralPath $secretDirectory)) {
    New-Item -ItemType Directory -Path $secretDirectory | Out-Null
}

if (-not (Test-Path -LiteralPath $secretPath)) {
    $randomBytes = [byte[]]::new(32)
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($randomBytes) }
    finally { $generator.Dispose() }
    $randomText = [Convert]::ToBase64String($randomBytes).Replace('+', 'A').Replace('/', 'b').TrimEnd('=')
    $secret = "F!${randomText}a9"
    [System.IO.File]::WriteAllText($secretPath, $secret, [System.Text.UTF8Encoding]::new($false))
}

$secretInfo = Get-Item -LiteralPath $secretPath
if (-not $secretInfo.FullName.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Le secret SqlLab doit rester dans le dépôt local ignoré par Git.'
}

$env:SQL_LAB_SA_PASSWORD_FILE = $secretInfo.FullName
$env:FORGE_SQL_LAB_ENABLED = if ($IncludeWeb) { 'true' } else { 'false' }
$bridgePort = if ([string]::IsNullOrWhiteSpace($env:FORGE_SQL_LAB_PORT)) { 14333 } else { [int]$env:FORGE_SQL_LAB_PORT }

$arguments = @('compose', '--profile', 'sql-lab', '--profile', 'sql-lab-test', 'up', '-d')
if (-not $NoBuild) { $arguments += '--build' }
$arguments += @('sql-lab', 'sql-lab-test-bridge')
if ($IncludeWeb) { $arguments += 'forge-dotnet' }

& docker @arguments
if ($LASTEXITCODE -ne 0) { throw "docker compose up a échoué avec le code $LASTEXITCODE." }

$deadline = [DateTimeOffset]::UtcNow.AddMinutes(3)
do {
    $health = & docker inspect --format '{{.State.Health.Status}}' forge-dotnet-sql-lab 2>$null
    if ($LASTEXITCODE -eq 0 -and $health -eq 'healthy') {
        $client = [System.Net.Sockets.TcpClient]::new()
        try {
            $connected = $client.ConnectAsync('127.0.0.1', $bridgePort).Wait(1000) -and $client.Connected
        }
        catch { $connected = $false }
        finally { $client.Dispose() }
        if ($connected) {
            Write-Output 'SqlLab is healthy. No secret was displayed.'
            exit 0
        }
    }

    if ($LASTEXITCODE -eq 0 -and $health -eq 'unhealthy') {
        throw 'SqlLab became unhealthy. Inspect redacted logs without displaying the secret.'
    }

    Start-Sleep -Seconds 3
} while ([DateTimeOffset]::UtcNow -lt $deadline)

throw 'SqlLab and its loopback test bridge did not become healthy within three minutes.'
