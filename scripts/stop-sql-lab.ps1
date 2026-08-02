[CmdletBinding()]
param([switch]$PurgeSecret)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    & docker compose --profile sql-lab --profile sql-lab-test down --remove-orphans
    if ($LASTEXITCODE -ne 0) { throw "docker compose down a échoué avec le code $LASTEXITCODE." }

    if ($PurgeSecret) {
        $secretPath = Join-Path $repositoryRoot '.secrets\sql-lab-sa-password.txt'
        if (Test-Path -LiteralPath $secretPath) {
            $resolved = (Get-Item -LiteralPath $secretPath).FullName
            if (-not $resolved.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Refus de supprimer un secret hors du dépôt local.'
            }

            Remove-Item -LiteralPath $resolved -Force
            Write-Output 'Secret SqlLab local supprimé.'
        }
    }
}
finally {
    Pop-Location
}
