<#
.SYNOPSIS
    Fusionne un lot de cartes de révision dans la banque du catalogue.

.DESCRIPTION
    La banque est le seul fichier de contenu qui alimente la composante de rétention espacée de la
    politique de maîtrise. Elle s'écrit par lots — un domaine à la fois — plutôt qu'en un seul bloc,
    ce qui rend chaque ajout relisible.

    Le script refuse d'écrire plutôt que de produire une banque douteuse :

      - un identifiant déjà présent dans la banque ;
      - un identifiant non déclaré par le manifeste de l'exercice qu'il revendique ;
      - un énoncé déjà utilisé par une autre carte ;
      - une carte dont l'option attendue ne figure pas parmi ses options, ou dont deux options
        partagent un identifiant ou un libellé.

    Les cartes sont écrites triées par identifiant : deux fusions successives produisent le même
    fichier quel que soit l'ordre des lots.

.PARAMETER BatchPath
    Fichier JSON contenant un tableau « cards » au format de la banque.

.PARAMETER ContentRoot
    Racine du contenu. Par défaut content/ du dépôt.

.PARAMETER WhatIf
    Rapporte ce qui serait écrit sans modifier la banque.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory)][string] $BatchPath,
    [string] $ContentRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if (-not $ContentRoot) { $ContentRoot = Join-Path $repositoryRoot 'content' }
$catalogRoot = Join-Path $ContentRoot 'reference'
$bankPath = Join-Path $catalogRoot 'reviews/exercise-review-cards.json'
if (-not (Test-Path -LiteralPath $bankPath -PathType Leaf)) {
    throw "Banque de cartes introuvable : $bankPath"
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

# Une carte porte soit un exercice, soit un scénario SQL : la pratique du domaine SQL passe par des
# scénarios, et sans eux sa composante de rétention resterait vide en permanence.
function Get-DeclaredCardIds {
    param([string] $ItemId, [string] $ItemKind)

    $manifestPath = if ($ItemKind -eq 'sql-scenario') {
        Join-Path (Split-Path $catalogRoot -Parent) "sql/$ItemId/scenario.json"
    }
    else {
        Join-Path $catalogRoot "exercises/$ItemId/exercise.json"
    }

    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Élément inconnu : $ItemId ($ItemKind)"
    }

    return @((Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json).reviewCards)
}

$bank = Get-Content -LiteralPath $bankPath -Raw | ConvertFrom-Json
$batch = Get-Content -LiteralPath $BatchPath -Raw | ConvertFrom-Json

$existing = [System.Collections.Generic.List[object]]::new()
foreach ($card in @($bank.cards)) { $existing.Add($card) }

$knownIds = [System.Collections.Generic.HashSet[string]]::new([string[]] @($existing | ForEach-Object { $_.id }), [System.StringComparer]::Ordinal)
$knownQuestions = [System.Collections.Generic.HashSet[string]]::new([string[]] @($existing | ForEach-Object { $_.question }), [System.StringComparer]::Ordinal)

$added = 0
foreach ($card in @($batch.cards)) {
    if (-not $knownIds.Add([string] $card.id)) {
        throw "Carte déjà présente dans la banque : $($card.id)"
    }
    if (-not $knownQuestions.Add([string] $card.question)) {
        throw "Énoncé déjà utilisé par une autre carte : $($card.id)"
    }

    $kind = if ($card.PSObject.Properties.Name -contains 'itemKind') { [string] $card.itemKind } else { 'exercise' }
    $declared = Get-DeclaredCardIds -ItemId ([string] $card.exerciseId) -ItemKind $kind
    if ([string] $card.id -notin $declared) {
        throw "$($card.id) : non déclarée par $($card.exerciseId)."
    }

    $options = @($card.options)
    if ($options.Count -lt 3) {
        throw "$($card.id) : trois options au minimum."
    }
    if (@($options | ForEach-Object { $_.id } | Sort-Object -Unique).Count -ne $options.Count) {
        throw "$($card.id) : deux options partagent un identifiant."
    }
    if (@($options | ForEach-Object { $_.text } | Sort-Object -Unique).Count -ne $options.Count) {
        throw "$($card.id) : deux options partagent un libellé."
    }
    if ([string] $card.correctOptionId -notin @($options | ForEach-Object { [string] $_.id })) {
        throw "$($card.id) : l'option attendue ne figure pas parmi les options."
    }

    $existing.Add($card)
    $added++
}

$ordered = @($existing | Sort-Object { [string] $_.id })

$builder = [System.Text.StringBuilder]::new()
$null = $builder.AppendLine('{')
$null = $builder.AppendLine('  "schemaVersion": 1,')
$null = $builder.AppendLine("  `"id`": $(ConvertTo-Json -InputObject ([string] $bank.id) -Compress),")
$null = $builder.AppendLine("  `"version`": $([int] $bank.version),")
$null = $builder.AppendLine("  `"title`": $(ConvertTo-Json -InputObject ([string] $bank.title) -Compress),")
$null = $builder.AppendLine('  "cards": [')

for ($index = 0; $index -lt $ordered.Count; $index++) {
    $card = $ordered[$index]
    $null = $builder.AppendLine('    {')
    $null = $builder.AppendLine("      `"id`": $(ConvertTo-Json -InputObject ([string] $card.id) -Compress),")
    $null = $builder.AppendLine("      `"exerciseId`": $(ConvertTo-Json -InputObject ([string] $card.exerciseId) -Compress),")
    if ($card.PSObject.Properties.Name -contains 'itemKind') {
        $null = $builder.AppendLine("      `"itemKind`": $(ConvertTo-Json -InputObject ([string] $card.itemKind) -Compress),")
    }
    $null = $builder.AppendLine("      `"domain`": $(ConvertTo-Json -InputObject ([string] $card.domain) -Compress),")
    $null = $builder.AppendLine("      `"question`": $(ConvertTo-Json -InputObject ([string] $card.question) -Compress),")
    $null = $builder.AppendLine("      `"correctOptionId`": $(ConvertTo-Json -InputObject ([string] $card.correctOptionId) -Compress),")
    $null = $builder.AppendLine('      "options": [')

    $options = @($card.options)
    for ($optionIndex = 0; $optionIndex -lt $options.Count; $optionIndex++) {
        $option = $options[$optionIndex]
        $separator = if ($optionIndex -lt $options.Count - 1) { ',' } else { '' }
        $null = $builder.AppendLine(
            "        { `"id`": $(ConvertTo-Json -InputObject ([string] $option.id) -Compress), " +
            "`"text`": $(ConvertTo-Json -InputObject ([string] $option.text) -Compress) }$separator")
    }

    $null = $builder.AppendLine('      ]')
    $null = $builder.AppendLine($(if ($index -lt $ordered.Count - 1) { '    },' } else { '    }' }))
}

$null = $builder.AppendLine('  ],')
$null = $builder.AppendLine("  `"license`": $(ConvertTo-Json -InputObject ([string] $bank.license) -Compress)")
$null = $builder.AppendLine('}')

$content = $builder.ToString() -replace "`r`n", "`n"

# On n'écrit jamais un JSON qu'on n'a pas su relire.
$check = [System.Text.Json.JsonDocument]::Parse($content)
try { $null = $check.RootElement.GetProperty('cards').GetArrayLength() } finally { $check.Dispose() }

if ($PSCmdlet.ShouldProcess($bankPath, "Fusionner $added carte(s)")) {
    [System.IO.File]::WriteAllText($bankPath, ($content -replace "`n", "`r`n"), $utf8NoBom)
}

Write-Output "Cartes ajoutées : $added ; total : $($ordered.Count)"
