<#
.SYNOPSIS
    Remplace l'indice de niveau 4, les erreurs fréquentes et l'explication d'exercices publiés.

.DESCRIPTION
    Cent vingt-cinq des cent trente-cinq exercices publiés partagent treize formulations d'indice de
    niveau 4 — un seul texte couvre soixante-quinze exercices — et treize jeux d'erreurs fréquentes.
    Le niveau 4 est la dernière marche avant le déverrouillage de la solution : générique, il ne
    débloque personne et envoie l'apprenant vers la solution, ce que docs/MASTERY.md sanctionne par
    un score nul. Le texte cloné convertit donc « bloqué » en « 0 ».

    Ce script applique un fichier de reprise et écrit **par splice textuel** dans exercise.json : les
    octets des autres champs sont préservés, ce qui évite qu'un aller-retour par ConvertTo-Json
    reformate les cent trente-cinq manifestes.

    Trois garde-fous refusent l'écriture plutôt que de produire du contenu douteux :

      - l'indice de niveau 4 ne doit contenir aucune ligne de solution/Submission.cs, blancs
        normalisés : un indice qui recopie la solution supprime le plafond de score à 60 ;
      - le manifeste réécrit doit rester conforme aux bornes du schéma (indice de 10 à 1 000
        caractères, erreurs fréquentes uniques et de 5 à 300 caractères) ;
      - le fichier doit être relisible comme JSON après écriture.

    Le script est idempotent : rejoué avec le même fichier de reprise, il n'écrit rien.

.PARAMETER RepairPath
    Fichier JSON décrivant la reprise :

        {
          "csharp-clamp-value-001": {
            "hint4": "si minimum dépasse maximum lever une erreur ; …",
            "commonMistakes": [ "…", "…" ],
            "constraints": [ "…", "…" ],
            "explanation": "Première phrase conservée.\n\nDeuxième paragraphe."
          }
        }

    « explanation » est le corps de explanation.md, sans le titre « # Explication » que le script
    ajoute lui-même. « constraints » est facultatif : il n'est réécrit que lorsqu'une contrainte
    générique partagée par plus de trois exercices doit devenir propre à celui-ci.

.PARAMETER ContentRoot
    Racine du contenu. Par défaut content/ du dépôt.

.PARAMETER WhatIf
    Rapporte les fichiers qui seraient modifiés sans rien écrire.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory)][string] $RepairPath,
    [string] $ContentRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if (-not $ContentRoot) { $ContentRoot = Join-Path $repositoryRoot 'content' }
$exercisesRoot = Join-Path $ContentRoot 'reference/exercises'
if (-not (Test-Path -LiteralPath $exercisesRoot -PathType Container)) {
    throw "Dossier des exercices introuvable : $exercisesRoot"
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Read-ContentFile {
    # Normalise en LF pour que les expressions régulières ne butent pas sur le retour chariot.
    param([string] $Path)
    return ([System.IO.File]::ReadAllText($Path) -replace "`r`n", "`n")
}

function Write-ContentFile {
    param([string] $Path, [string] $Text)
    [System.IO.File]::WriteAllText($Path, ($Text -replace "`r?`n", "`r`n"), $utf8NoBom)
}

function Assert-HintDoesNotLeakSolution {
    <#
        Un indice qui recopie une ligne de la solution n'est plus un indice : il rend le plafond de
        score à 60 équivalent à un déverrouillage gratuit.
    #>
    param([string] $ExerciseId, [string] $Hint, [string] $SolutionPath)

    if (-not (Test-Path -LiteralPath $SolutionPath -PathType Leaf)) { return }

    $normalizedHint = ($Hint -replace '\s+', ' ').Trim()
    foreach ($line in (Read-ContentFile $SolutionPath) -split "`n") {
        $normalized = ($line -replace '\s+', ' ').Trim()
        # Les lignes de structure — accolades, en-tête de classe — ne portent aucune information.
        if ($normalized.Length -lt 20) { continue }
        if ($normalizedHint.Contains($normalized, [System.StringComparison]::Ordinal)) {
            throw "$ExerciseId : l'indice de niveau 4 recopie une ligne de la solution."
        }
    }
}

function Assert-SchemaBounds {
    param([string] $ExerciseId, [string] $Hint, [string[]] $Mistakes)

    if ($Hint.Length -lt 10 -or $Hint.Length -gt 1000) {
        throw "$ExerciseId : l'indice de niveau 4 doit tenir entre 10 et 1 000 caractères."
    }
    if ($Mistakes.Count -lt 1 -or $Mistakes.Count -gt 20) {
        throw "$ExerciseId : commonMistakes doit compter de 1 à 20 entrées."
    }
    if (@($Mistakes | Sort-Object -Unique).Count -ne $Mistakes.Count) {
        throw "$ExerciseId : commonMistakes doit être sans doublon."
    }
    foreach ($mistake in $Mistakes) {
        if ($mistake.Length -lt 5 -or $mistake.Length -gt 300) {
            throw "$ExerciseId : « $mistake » sort des bornes de 5 à 300 caractères."
        }
    }
}

function Set-Hint4 {
    param([string] $Text, [string] $Hint)

    $pattern = '("level":\s*4,\s*"kind":\s*"partial-pseudocode",\s*"content":\s*)"(?:[^"\\]|\\.)*"'
    $encoded = ConvertTo-Json -InputObject $Hint -Compress
    $replacement = { param($match) $match.Groups[1].Value + $encoded }

    $updated = [regex]::Replace($Text, $pattern, $replacement)
    if ($updated -eq $Text -and $Text -notmatch [regex]::Escape($encoded)) {
        throw 'Bloc de niveau 4 introuvable dans le manifeste.'
    }
    return $updated
}

function Set-JsonStringArray {
    <#
        Réécrit un tableau de chaînes en respectant l'indentation produite par ConvertTo-Json, qui
        aligne la valeur sur la longueur de la clé : le crochet ouvrant se place après « "clé":  »,
        les entrées quatre colonnes plus loin, et le crochet fermant sous les entrées moins quatre.
    #>
    param([string] $Text, [string] $Key, [string[]] $Values, [string] $NextKey)

    $valueColumn = 4 + $Key.Length + 3 + 2
    $items = $Values | ForEach-Object { (' ' * ($valueColumn + 4)) + (ConvertTo-Json -InputObject $_ -Compress) }
    $block = """$Key"":  [" + "`n" + ($items -join ",`n") + "`n" + (' ' * $valueColumn) + ']'

    $pattern = "`"$Key`":\s*\[[\s\S]*?\](?=,\s*`"$NextKey`")"
    if ($Text -notmatch $pattern) { throw "Bloc $Key introuvable dans le manifeste." }
    return [regex]::Replace($Text, $pattern, { $block }, 1)
}

$repair = Get-Content -LiteralPath $RepairPath -Raw | ConvertFrom-Json
$changedManifests = 0
$changedExplanations = 0
$changedStatements = 0

foreach ($property in $repair.PSObject.Properties) {
    $exerciseId = $property.Name
    $entry = $property.Value
    $directory = Join-Path $exercisesRoot $exerciseId
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        throw "Exercice inconnu : $exerciseId"
    }

    $hint = [string] $entry.hint4
    $mistakes = @($entry.commonMistakes)

    Assert-SchemaBounds -ExerciseId $exerciseId -Hint $hint -Mistakes $mistakes
    Assert-HintDoesNotLeakSolution -ExerciseId $exerciseId -Hint $hint `
        -SolutionPath (Join-Path $directory 'solution/Submission.cs')

    $manifestPath = Join-Path $directory 'exercise.json'
    $original = Read-ContentFile $manifestPath
    $updated = Set-Hint4 -Text $original -Hint $hint
    $updated = Set-JsonStringArray -Text $updated -Key 'commonMistakes' -Values $mistakes -NextKey 'variantId'

    # « constraints » n'est réécrit que si la reprise en fournit : la plupart des lots n'y touchent pas.
    if ($entry.PSObject.Properties.Name -contains 'constraints') {
        $constraints = @($entry.constraints)
        if (@($constraints | Sort-Object -Unique).Count -ne $constraints.Count) {
            throw "$exerciseId : constraints doit être sans doublon."
        }
        foreach ($constraint in $constraints) {
            if ($constraint.Length -lt 5 -or $constraint.Length -gt 300) {
                throw "$exerciseId : « $constraint » sort des bornes de 5 à 300 caractères."
            }
        }
        $updated = Set-JsonStringArray -Text $updated -Key 'constraints' -Values $constraints -NextKey 'examples'
    }

    # On n'écrit jamais un JSON qu'on n'a pas su relire.
    $check = [System.Text.Json.JsonDocument]::Parse($updated)
    try {
        $hints = $check.RootElement.GetProperty('hints')
        if ($hints.GetArrayLength() -ne 4) { throw "$exerciseId : le manifeste ne porte plus quatre indices." }
    }
    finally { $check.Dispose() }

    if ($updated -ne $original) {
        if ($PSCmdlet.ShouldProcess($manifestPath, 'Réécrire indice 4 et erreurs fréquentes')) {
            Write-ContentFile -Path $manifestPath -Text $updated
        }
        $changedManifests++
    }

    # Le second paragraphe de l'énoncé — le protocole de preuve — est identique dans soixante-quatre
    # énoncés. Il compte comme paragraphe cloné parce qu'il est isolé, là où les lots plus anciens
    # le fondaient dans une phrase propre à l'exercice.
    if ($entry.PSObject.Properties.Name -contains 'statementProtocol') {
        $statementPath = Join-Path $directory 'statement.md'
        $statement = Read-ContentFile $statementPath
        $protocol = ([string] $entry.statementProtocol).Trim()

        $pattern = '(?m)^Le résultat reste déterministe[^\n]*$'
        if ($statement -match $pattern) {
            $updatedStatement = [regex]::Replace($statement, $pattern, { $protocol }, 1)
            if ($PSCmdlet.ShouldProcess($statementPath, "Réécrire le protocole de l'énoncé")) {
                Write-ContentFile -Path $statementPath -Text $updatedStatement
            }
            $changedStatements++
        }
        elseif ($statement -notmatch [regex]::Escape($protocol)) {
            throw "$exerciseId : paragraphe de protocole introuvable dans l'énoncé."
        }
    }

    $explanationPath = Join-Path $directory 'explanation.md'
    $body = ([string] $entry.explanation).Trim()
    $explanation = "# Explication`n`n" + $body + "`n"
    if ((Read-ContentFile $explanationPath) -ne $explanation) {
        if ($PSCmdlet.ShouldProcess($explanationPath, "Réécrire l'explication")) {
            Write-ContentFile -Path $explanationPath -Text $explanation
        }
        $changedExplanations++
    }
}

Write-Output ("Manifestes modifiés : $changedManifests ; explications modifiées : $changedExplanations ; " +
    "énoncés modifiés : $changedStatements")
