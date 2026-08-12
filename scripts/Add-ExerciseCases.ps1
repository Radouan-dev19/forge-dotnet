<#
.SYNOPSIS
    Ajoute des cas de test à des exercices publiés, sans toucher aux cas existants.

.DESCRIPTION
    Cent vingt-cinq des cent trente-cinq exercices publiés ne portent que deux cas visibles et deux
    cas cachés, contre trois et quatre pour les dix exercices relus à la main. Le nombre de cas
    cachés détermine directement la capacité à réfuter une implémentation fausse ou codée en dur :
    une couverture réduite de moitié rend la maîtrise mesurée deux fois moins fiable.

    Ce script applique un fichier de complément et insère les nouveaux cas **par splice textuel**.
    Les octets des cas existants sont préservés : un aller-retour par ConvertTo-Json reformaterait
    les deux cent cinquante fichiers et, plus grave, transformerait un montant « 120.50 » en
    « 120.5 ».

    Les nouveaux cas sont émis avec des tableaux compacts, plus lisibles en revue qu'un argument
    étalé sur douze lignes. Le fichier reste du JSON valide, et le script le revérifie après écriture.

.PARAMETER SupplementPath
    Fichier JSON décrivant les cas à ajouter :

        {
          "algo-binary-search-001": {
            "visible": [ { "name": "...", "message": "...", "arguments": [...],
                           "expected": ..., "expectedException": null,
                           "argumentsUnchanged": false } ],
            "hidden":  [ ... ]
          }
        }

.PARAMETER ContentRoot
    Racine du contenu. Par défaut content/ du dépôt.

.PARAMETER WhatIf
    Rapporte les fichiers qui seraient modifiés sans rien écrire.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory)][string] $SupplementPath,
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

function ConvertTo-CompactJson {
    # JsonElement.WriteTo recopie le jeton numérique d'origine : « 120.50 » ne devient pas « 120.5 ».
    param([System.Text.Json.JsonElement] $Element)
    $stream = [System.IO.MemoryStream]::new()
    try {
        $options = [System.Text.Json.JsonWriterOptions]::new()
        $options.Indented = $false
        $writer = [System.Text.Json.Utf8JsonWriter]::new($stream, $options)
        try { $Element.WriteTo($writer); $writer.Flush() } finally { $writer.Dispose() }
        return [System.Text.Encoding]::UTF8.GetString($stream.ToArray())
    }
    finally { $stream.Dispose() }
}

function Format-Case {
    <#
        Émet un objet de cas à l'indentation du fichier généré : accolades à dix-huit espaces,
        propriétés à vingt-deux.
    #>
    param([System.Text.Json.JsonElement] $Case)

    $name = $Case.GetProperty('name').GetString()
    $message = $Case.GetProperty('message').GetString()
    $arguments = ConvertTo-CompactJson $Case.GetProperty('arguments')
    $expectedElement = [System.Text.Json.JsonElement]::new()
    $expected = if ($Case.TryGetProperty('expected', [ref] $expectedElement)) {
        ConvertTo-CompactJson $expectedElement
    } else { 'null' }
    $exceptionElement = [System.Text.Json.JsonElement]::new()
    $exception = if ($Case.TryGetProperty('expectedException', [ref] $exceptionElement) -and
                     $exceptionElement.ValueKind -eq [System.Text.Json.JsonValueKind]::String) {
        ConvertTo-CompactJson $exceptionElement
    } else { 'null' }
    $unchangedElement = [System.Text.Json.JsonElement]::new()
    $unchanged = if ($Case.TryGetProperty('argumentsUnchanged', [ref] $unchangedElement) -and
                     $unchangedElement.ValueKind -eq [System.Text.Json.JsonValueKind]::True) {
        'true'
    } else { 'false' }

    $nameJson = ConvertTo-Json -InputObject $name -Compress
    $messageJson = ConvertTo-Json -InputObject $message -Compress

    # Le contrat du runner exige exactement l'un des deux : un résultat attendu OU une exception
    # attendue. Émettre « expected: null » à côté d'une exception fait refuser le cas.
    $outcome = if ($exception -ne 'null') {
        "                      `"expectedException`":  $exception,"
    }
    else {
        "                      `"expected`":  $expected,"
    }

    return @"
                  {
                      "name":  $nameJson,
                      "message":  $messageJson,
                      "arguments":  $arguments,
$outcome
                      "argumentsUnchanged":  $unchanged
                  }
"@ -replace "`r`n", "`n"
}

function Add-Cases {
    param([string] $Path, [System.Text.Json.JsonElement] $Cases)

    $text = ([System.IO.File]::ReadAllText($Path) -replace "`r`n", "`n")

    # Contrôle d'unicité : rejouer le script ne doit jamais dupliquer un cas.
    $existing = [System.Text.Json.JsonDocument]::Parse($text)
    try {
        $names = @($existing.RootElement.GetProperty('cases').EnumerateArray() |
            ForEach-Object { $_.GetProperty('name').GetString() })
    }
    finally { $existing.Dispose() }

    $blocks = @()
    foreach ($case in $Cases.EnumerateArray()) {
        $name = $case.GetProperty('name').GetString()
        if ($names -contains $name) {
            Write-Verbose "Cas déjà présent, ignoré : $name dans $Path"
            continue
        }
        $blocks += (Format-Case $case)
    }

    if ($blocks.Count -eq 0) { return $false }

    # Le tableau « cases » se ferme par une ligne de quatorze espaces suivie d'un crochet.
    $closing = $text.LastIndexOf("`n              ]")
    if ($closing -lt 0) { throw "Structure de cases.json non reconnue : $Path" }

    $inserted = $text.Substring(0, $closing) + ",`n" + ($blocks -join ",`n").TrimEnd("`n") +
                $text.Substring($closing)

    # Revérification : on n'écrit jamais un JSON qu'on n'a pas su relire.
    $check = [System.Text.Json.JsonDocument]::Parse($inserted)
    try { $null = $check.RootElement.GetProperty('cases').GetArrayLength() } finally { $check.Dispose() }

    if ($PSCmdlet.ShouldProcess($Path, 'Ajouter des cas de test')) {
        [System.IO.File]::WriteAllText($Path, ($inserted -replace "`n", "`r`n"), $utf8NoBom)
    }

    return $true
}

$supplement = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($SupplementPath))
$changed = 0
try {
    foreach ($entry in $supplement.RootElement.EnumerateObject()) {
        $exerciseId = $entry.Name
        $directory = Join-Path $exercisesRoot $exerciseId
        if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
            throw "Exercice inconnu : $exerciseId"
        }

        foreach ($visibility in 'visible', 'hidden') {
            $element = [System.Text.Json.JsonElement]::new()
            if (-not $entry.Value.TryGetProperty($visibility, [ref] $element)) { continue }
            $path = Join-Path $directory "tests/$visibility/cases.json"
            if (Add-Cases -Path $path -Cases $element) { $changed++ }
        }
    }
}
finally { $supplement.Dispose() }

Write-Output "Fichiers de cas modifiés : $changed"
