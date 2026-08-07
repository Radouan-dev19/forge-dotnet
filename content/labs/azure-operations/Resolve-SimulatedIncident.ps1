[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$rows=Import-Csv (Join-Path $PSScriptRoot 'telemetry/simulated-incident.csv')
if($rows.Count -ne 4){throw 'Le jeu de télémétrie simulé doit contenir exactement quatre observations.'}
if($rows.correlationId | Where-Object {$_ -notmatch '^forge-fake-corr-[0-9]{3}$'}){throw 'Un identifiant de corrélation simulé est invalide.'}
$errors=@($rows | Where-Object {[int]$_.status -ge 500})
$ordered=@($rows.durationMs | ForEach-Object {[int]$_} | Sort-Object)
$p95=$ordered[$ordered.Count-1]
if($errors.Count -ne 2 -or $p95 -ne 1100){throw "Diagnostic inattendu : errors=$($errors.Count), p95=$p95."}
Write-Output 'INCIDENT SIMULÉ RÉSOLU : 2 erreurs corrélées, p95 borné à 1100 ms, aucune donnée personnelle.'
