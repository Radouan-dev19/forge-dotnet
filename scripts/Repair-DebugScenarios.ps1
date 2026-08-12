<#
.SYNOPSIS
    Rend diagnosticable un scénario de débogage dont le ticket nommait déjà la cause.

.DESCRIPTION
    Dix-sept des vingt-cinq DebugLabs publiés portent un ticket qui énonce la cause avant que
    l'apprenant commence — « La division utilise une longueur nulle » —, un journal réduit à une
    ligne ne contenant que l'identifiant du scénario et deux constantes, et une rubrique d'évaluation
    identique dont les termes attendus sont « borne, condition, mutation ».

    Les leçons debug-stacktraces-breakpoints-001 et performance-security-incident-001 enseignent une
    méthode en quatre temps : symptôme, hypothèse, preuve, prévention. Sur ces dix-sept scénarios
    l'appareil d'entraînement court-circuite les trois premiers, et l'évaluation ne peut pas voir la
    différence entre un journal diagnostiqué et un journal rempli de mots passe-partout.

    Ce script applique une reprise et écrit **par splice textuel** dans scenario.json : les octets des
    autres champs sont préservés. ticket.md, logs.txt, regression-test.md et tests/rubric.json sont
    réécrits intégralement.

    Il ne touche jamais broken/, correction/, tests/visible/cases.json ni tests/hidden/cases.json :
    le défaut et sa correction restent identiques, donc la vérification DebugLab existante reste
    valide.

    Trois garde-fous refusent l'écriture plutôt que de produire du contenu douteux :

      - le ticket contient un identifiant présent dans la correction et absent du code fautif,
        c'est-à-dire nomme la solution ;
      - les bornes du schéma ou du domaine sont dépassées ;
      - le JSON produit n'est pas relisible.

    Le script est idempotent : rejoué avec le même fichier de reprise, il n'écrit rien.

.PARAMETER RepairPath
    Fichier JSON décrivant la reprise :

        {
          "debug-array-empty-001": {
            "title": "…",                          (facultatif)
            "ticket": "…",                         (corps de ticket.md)
            "logs": [ "Event=… …", "…" ],
            "expectedBehavior": "…",
            "checklist": [ "…", "…", "…" ],
            "observationQuestions": [ "…", "…" ],
            "regressionTest": "…",                 (corps de regression-test.md)
            "rubric": [
              { "id": "…", "label": "…", "journalField": "cause",
                "requiredTerms": [ "…" ], "minimumMatches": 1 }
            ]
          }
        }

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
$debuggingRoot = Join-Path $ContentRoot 'reference/debugging'
if (-not (Test-Path -LiteralPath $debuggingRoot -PathType Container)) {
    throw "Dossier des scénarios de débogage introuvable : $debuggingRoot"
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$allowedJournalFields = @('cause', 'evidence', 'test', 'prevention')

function Read-ContentFile {
    param([string] $Path)
    return ([System.IO.File]::ReadAllText($Path) -replace "`r`n", "`n")
}

function Write-ContentFile {
    param([string] $Path, [string] $Text)
    [System.IO.File]::WriteAllText($Path, ($Text -replace "`r?`n", "`r`n"), $utf8NoBom)
}

function Get-AddedIdentifiers {
    <#
        Identifiants présents dans la correction et absents du code fautif. Les nommer dans le
        ticket revient à donner la solution avant que l'apprenant ait observé quoi que ce soit.
    #>
    param([string] $Directory)

    $brokenPath = Join-Path $Directory 'broken/Submission.cs'
    $correctionPath = Join-Path $Directory 'correction/Submission.cs'
    if (-not (Test-Path -LiteralPath $brokenPath) -or -not (Test-Path -LiteralPath $correctionPath)) {
        return @()
    }

    $broken = [regex]::Matches((Read-ContentFile $brokenPath), '[A-Za-z]{4,}') |
        ForEach-Object { $_.Value.ToLowerInvariant() }
    $correction = [regex]::Matches((Read-ContentFile $correctionPath), '[A-Za-z]{4,}') |
        ForEach-Object { $_.Value.ToLowerInvariant() }

    return @($correction | Sort-Object -Unique | Where-Object { $_ -notin $broken })
}

function Assert-NoSolutionLeak {
    param([string] $ScenarioId, [string] $Text, [string[]] $AddedIdentifiers, [string] $Field)

    $lower = $Text.ToLowerInvariant()
    foreach ($identifier in $AddedIdentifiers) {
        if ($lower -match "\b$([regex]::Escape($identifier))\b") {
            throw "$ScenarioId : « $Field » nomme « $identifier », introduit par la correction."
        }
    }
}

function Assert-SchemaBounds {
    param([string] $ScenarioId, $Entry)

    $behavior = [string] $Entry.expectedBehavior
    if ($behavior.Length -lt 20 -or $behavior.Length -gt 1000) {
        throw "$ScenarioId : expectedBehavior doit tenir entre 20 et 1 000 caractères."
    }

    $checklist = @($Entry.checklist)
    if ($checklist.Count -lt 3 -or $checklist.Count -gt 20) {
        throw "$ScenarioId : checklist doit compter de 3 à 20 entrées."
    }
    if (@($checklist | Sort-Object -Unique).Count -ne $checklist.Count) {
        throw "$ScenarioId : checklist doit être sans doublon."
    }
    foreach ($item in $checklist) {
        if ($item.Length -lt 5 -or $item.Length -gt 300) {
            throw "$ScenarioId : « $item » sort des bornes de 5 à 300 caractères."
        }
    }

    $questions = @($Entry.observationQuestions)
    if ($questions.Count -lt 2 -or $questions.Count -gt 20) {
        throw "$ScenarioId : observationQuestions doit compter de 2 à 20 entrées."
    }
    if (@($questions | Sort-Object -Unique).Count -ne $questions.Count) {
        throw "$ScenarioId : observationQuestions doit être sans doublon."
    }
    foreach ($item in $questions) {
        if ($item.Length -lt 10 -or $item.Length -gt 500) {
            throw "$ScenarioId : « $item » sort des bornes de 10 à 500 caractères."
        }
    }
}

function Assert-RubricRules {
    <#
        Reprend les règles de DebugLabRules.cs : au moins deux critères, un champ de journal connu,
        des termes attendus non vides et un nombre de correspondances exigées atteignable.
    #>
    param([string] $ScenarioId, $Criteria)

    $items = @($Criteria)
    if ($items.Count -lt 2) {
        throw "$ScenarioId : la rubrique doit porter au moins deux critères."
    }

    foreach ($criterion in $items) {
        if ([string]::IsNullOrWhiteSpace($criterion.id) -or [string]::IsNullOrWhiteSpace($criterion.label)) {
            throw "$ScenarioId : un critère de rubrique est incomplet."
        }
        if ($criterion.journalField -notin $allowedJournalFields) {
            throw "$ScenarioId : champ de journal inconnu « $($criterion.journalField) »."
        }

        $terms = @($criterion.requiredTerms)
        if ($terms.Count -lt 1) {
            throw "$ScenarioId : le critère « $($criterion.id) » n'exige aucun terme."
        }
        if ($criterion.minimumMatches -lt 1 -or $criterion.minimumMatches -gt $terms.Count) {
            throw "$ScenarioId : minimumMatches hors bornes pour « $($criterion.id) »."
        }
    }
}

function Set-JsonString {
    param([string] $Text, [string] $Key, [string] $Value)

    $pattern = "(`"$Key`":\s*)`"(?:[^`"\\]|\\.)*`""
    if ($Text -notmatch $pattern) { throw "Champ $Key introuvable dans le manifeste." }
    $encoded = ConvertTo-Json -InputObject $Value -Compress
    return [regex]::Replace($Text, $pattern, { param($match) $match.Groups[1].Value + $encoded }, 1)
}

function Set-JsonStringArray {
    <#
        Respecte l'indentation produite par ConvertTo-Json : le crochet ouvrant s'aligne après
        « "clé":  », les entrées quatre colonnes plus loin, le crochet fermant sous les entrées
        moins quatre.
    #>
    param([string] $Text, [string] $Key, [string[]] $Values, [string] $NextKey)

    $valueColumn = 4 + $Key.Length + 3 + 2
    $items = $Values | ForEach-Object { (' ' * ($valueColumn + 4)) + (ConvertTo-Json -InputObject $_ -Compress) }
    $block = """$Key"":  [" + "`n" + ($items -join ",`n") + "`n" + (' ' * $valueColumn) + ']'

    $pattern = "`"$Key`":\s*\[[\s\S]*?\](?=,\s*`"$NextKey`")"
    if ($Text -notmatch $pattern) { throw "Bloc $Key introuvable dans le manifeste." }
    return [regex]::Replace($Text, $pattern, { $block }, 1)
}

function Format-Rubric {
    param([string] $ScenarioId, $Criteria)

    $blocks = foreach ($criterion in @($Criteria)) {
        $terms = @($criterion.requiredTerms) |
            ForEach-Object { '        ' + (ConvertTo-Json -InputObject $_ -Compress) }

        @"
    {
      "id": $(ConvertTo-Json -InputObject $criterion.id -Compress),
      "label": $(ConvertTo-Json -InputObject $criterion.label -Compress),
      "journalField": $(ConvertTo-Json -InputObject $criterion.journalField -Compress),
      "requiredTerms": [
$($terms -join ",`n")
      ],
      "minimumMatches": $($criterion.minimumMatches)
    }
"@ -replace "`r`n", "`n"
    }

    return @"
{
  "schemaVersion": 1,
  "scenarioId": $(ConvertTo-Json -InputObject $ScenarioId -Compress),
  "criteria": [
$($blocks -join ",`n")
  ]
}
"@ -replace "`r`n", "`n"
}

$repair = Get-Content -LiteralPath $RepairPath -Raw | ConvertFrom-Json
$changedManifests = 0
$changedTickets = 0
$changedLogs = 0
$changedRegressions = 0
$changedRubrics = 0

foreach ($property in $repair.PSObject.Properties) {
    $scenarioId = $property.Name
    $entry = $property.Value
    $directory = Join-Path $debuggingRoot $scenarioId
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        throw "Scénario inconnu : $scenarioId"
    }

    Assert-SchemaBounds -ScenarioId $scenarioId -Entry $entry
    Assert-RubricRules -ScenarioId $scenarioId -Criteria $entry.rubric

    # La règle porte sur le ticket seul : le comportement attendu est le contrat, et il doit pouvoir
    # énoncer la valeur de repli exacte. Le ticket, lui, rapporte un symptôme observé.
    $added = Get-AddedIdentifiers -Directory $directory
    Assert-NoSolutionLeak -ScenarioId $scenarioId -Text ([string] $entry.ticket) `
        -AddedIdentifiers $added -Field 'ticket'

    $manifestPath = Join-Path $directory 'scenario.json'
    $original = Read-ContentFile $manifestPath
    $updated = $original

    if ($entry.PSObject.Properties.Name -contains 'title') {
        $updated = Set-JsonString -Text $updated -Key 'title' -Value ([string] $entry.title)
    }

    $updated = Set-JsonString -Text $updated -Key 'expectedBehavior' -Value ([string] $entry.expectedBehavior)
    $updated = Set-JsonStringArray -Text $updated -Key 'checklist' `
        -Values @($entry.checklist) -NextKey 'observationQuestions'
    $updated = Set-JsonStringArray -Text $updated -Key 'observationQuestions' `
        -Values @($entry.observationQuestions) -NextKey 'correctionPath'

    $check = [System.Text.Json.JsonDocument]::Parse($updated)
    try {
        if ($check.RootElement.GetProperty('journalFields').GetArrayLength() -ne 8) {
            throw "$scenarioId : les huit champs de journal ne sont plus intacts."
        }
    }
    finally { $check.Dispose() }

    if ($updated -ne $original) {
        if ($PSCmdlet.ShouldProcess($manifestPath, 'Réécrire comportement attendu, checklist et questions')) {
            Write-ContentFile -Path $manifestPath -Text $updated
        }
        $changedManifests++
    }

    $ticketPath = Join-Path $directory 'ticket.md'
    $ticket = "# Ticket`n`n" + ([string] $entry.ticket).Trim() + "`n"
    if ((Read-ContentFile $ticketPath) -ne $ticket) {
        if ($PSCmdlet.ShouldProcess($ticketPath, 'Réécrire le ticket')) {
            Write-ContentFile -Path $ticketPath -Text $ticket
        }
        $changedTickets++
    }

    $logsPath = Join-Path $directory 'logs.txt'
    $logs = (@($entry.logs) -join "`n") + "`n"
    if ((Read-ContentFile $logsPath) -ne $logs) {
        if ($PSCmdlet.ShouldProcess($logsPath, 'Réécrire les journaux')) {
            Write-ContentFile -Path $logsPath -Text $logs
        }
        $changedLogs++
    }

    $regressionPath = Join-Path $directory 'regression-test.md'
    $regression = "# Non-régression`n`n" + ([string] $entry.regressionTest).Trim() + "`n"
    if ((Read-ContentFile $regressionPath) -ne $regression) {
        if ($PSCmdlet.ShouldProcess($regressionPath, 'Réécrire le test de non-régression')) {
            Write-ContentFile -Path $regressionPath -Text $regression
        }
        $changedRegressions++
    }

    $rubricPath = Join-Path $directory 'tests/rubric.json'
    $rubric = Format-Rubric -ScenarioId $scenarioId -Criteria $entry.rubric
    $rubricCheck = [System.Text.Json.JsonDocument]::Parse($rubric)
    try { $null = $rubricCheck.RootElement.GetProperty('criteria').GetArrayLength() }
    finally { $rubricCheck.Dispose() }

    if ((Read-ContentFile $rubricPath) -ne $rubric) {
        if ($PSCmdlet.ShouldProcess($rubricPath, "Réécrire la rubrique d'évaluation")) {
            Write-ContentFile -Path $rubricPath -Text $rubric
        }
        $changedRubrics++
    }
}

Write-Output ("Manifestes : $changedManifests ; tickets : $changedTickets ; journaux : $changedLogs ; " +
    "non-régressions : $changedRegressions ; rubriques : $changedRubrics")
