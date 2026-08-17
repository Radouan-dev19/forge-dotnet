<#
.SYNOPSIS
Construit l'image du bac à sable d'exécution et rend la référence immuable à configurer.

.DESCRIPTION
Sans cette image, le produit ne peut valider aucun exercice : le mode Manual rend un résultat
« indisponible » sans aucun test exécuté, et la politique de maîtrise refuse une preuve qui ne
rapporte aucun test. L'installation lit, planifie et révise, mais ne valide rien.

La construction n'était documentée nulle part, et son contexte n'est pas la racine du dépôt mais
« src/ForgeDotNet.CodeRunner/Container » — le Dockerfile y copie « NuGet.Config » et « RunnerHost/ »
depuis la racine du contexte. Une construction lancée depuis le dépôt échoue sur un fichier
introuvable, sans indiquer lequel des deux emplacements est le bon.

Le runner exige une référence par empreinte sha256 complète, jamais par étiquette : une étiquette
peut être redirigée vers une autre image entre la vérification de politique et l'exécution. Ce script
relit donc l'empreinte après construction et l'affiche telle qu'elle doit être configurée.

.PARAMETER Tag
Étiquette locale donnée à l'image. Elle sert à retrouver l'image, jamais à l'exécuter.

.PARAMETER DockerContext
Contexte Docker utilisé pour la construction et l'inspection.

.EXAMPLE
./scripts/build-code-runner.ps1
#>
[CmdletBinding()]
param(
    [string]$Tag = 'forge-dotnet-runner:local',
    [string]$DockerContext = 'desktop-linux'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$buildContext = Join-Path $repositoryRoot 'src/ForgeDotNet.CodeRunner/Container'

if (-not (Test-Path -LiteralPath (Join-Path $buildContext 'Dockerfile'))) {
    throw "Le contexte de construction est introuvable : $buildContext."
}

Write-Output 'Construction de l''image du bac à sable...'
& docker --context $DockerContext build --tag $Tag $buildContext
if ($LASTEXITCODE -ne 0) {
    throw "docker build a échoué avec le code $LASTEXITCODE."
}

$imageId = (& docker --context $DockerContext image inspect $Tag --format '{{.Id}}')
if ($LASTEXITCODE -ne 0) {
    throw "L'image construite est introuvable sous l'étiquette $Tag."
}

$imageId = $imageId.Trim()

# Même forme que DockerCodeRunnerOptions.ImageReferencePattern : la refuser ici évite un échec de
# démarrage plus tard, dont le message ne pourrait plus dire d'où vient la valeur fautive.
if ($imageId -notmatch '^sha256:[a-f0-9]{64}$') {
    throw "L'empreinte rendue par Docker n'a pas la forme sha256 attendue : $imageId."
}

Write-Output ''
Write-Output "Image construite : $Tag"
Write-Output ''
Write-Output 'Configurez le mode Docker avec cette référence immuable :'
Write-Output ''
Write-Output '  dotnet run --project src/ForgeDotNet.Web `'
Write-Output '    --CodeRunner:Mode Docker `'
Write-Output "    --CodeRunner:Docker:ImageReference $imageId"
Write-Output ''
Write-Output 'Ou, de façon durable, dans src/ForgeDotNet.Web/appsettings.Development.json :'
Write-Output ''
Write-Output '  {'
Write-Output '    "CodeRunner": {'
Write-Output '      "Mode": "Docker",'
Write-Output "      `"Docker`": { `"ImageReference`": `"$imageId`" }"
Write-Output '    }'
Write-Output '  }'
Write-Output ''
Write-Output 'Relancez ce script après toute modification du runner : l''empreinte change, et une'
Write-Output 'référence périmée fait échouer le démarrage plutôt que de servir une ancienne image.'
