<#
.SYNOPSIS
    Rétablit les exemples d'entrée/sortie publiés des exercices et des scénarios SQL.

.DESCRIPTION
    Les générateurs de contenu échappaient par erreur leurs sous-expressions PowerShell dans un
    here-string : `$(Convert-JsonCompact ...)` et `$((... -join ', '))` ont donc été écrits
    littéralement au lieu d'être évalués. Cent vingt-cinq énoncés d'exercice et vingt-huit
    énoncés SQL affichaient ainsi un gabarit au lieu d'un exemple.

    Ce script ne réécrit pas la prose : il reconstruit uniquement l'exemple, à partir de la
    vérité de terrain déjà présente dans le dépôt.

      - exercices : premier cas de tests/visible/cases.json (arguments et résultat attendu) ;
      - scénarios SQL : colonnes déclarées par scenario.json (expectedResult.columns).

    Les valeurs numériques sont recopiées telles quelles — System.Text.Json réécrit le jeton
    d'origine — afin qu'un montant publié « 2.50 » ne devienne pas « 2,5 ».

    L'opération est déterministe et idempotente : la rejouer ne produit aucun changement.

.PARAMETER ContentRoot
    Racine du contenu. Par défaut content/ du dépôt.

.PARAMETER WhatIf
    Rapporte les fichiers qui seraient modifiés sans rien écrire.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $ContentRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if (-not $ContentRoot) { $ContentRoot = Join-Path $repositoryRoot 'content' }
if (-not (Test-Path -LiteralPath $ContentRoot -PathType Container)) {
    throw "Racine de contenu introuvable : $ContentRoot"
}

# UTF-8 sans BOM, comme le reste du contenu versionné.
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Read-ContentFile {
    <#
        Toutes les comparaisons se font en LF. Sans cette normalisation, une substitution de ligne
        consomme le retour chariot et le script se croirait modifié à chaque exécution.
    #>
    param([string] $Path)
    return ([System.IO.File]::ReadAllText($Path) -replace "`r`n", "`n")
}

function Write-ContentFile {
    param([string] $Path, [string] $Text)
    # .editorconfig impose CRLF dans la copie de travail : normaliser pour éviter un diff parasite.
    $normalized = ($Text -replace "`r`n", "`n") -replace "`n", "`r`n"
    [System.IO.File]::WriteAllText($Path, $normalized, $utf8NoBom)
}

function ConvertTo-CompactJson {
    <#
        Compacte un JsonElement sans réécrire ses nombres : JsonElement.WriteTo recopie le jeton
        numérique d'origine, contrairement à un aller-retour via ConvertTo-Json.
    #>
    param([System.Text.Json.JsonElement] $Element)

    $stream = [System.IO.MemoryStream]::new()
    try {
        $options = [System.Text.Json.JsonWriterOptions]::new()
        $options.Indented = $false
        $writer = [System.Text.Json.Utf8JsonWriter]::new($stream, $options)
        try {
            $Element.WriteTo($writer)
            $writer.Flush()
        }
        finally { $writer.Dispose() }
        return [System.Text.Encoding]::UTF8.GetString($stream.ToArray())
    }
    finally { $stream.Dispose() }
}

function Get-FirstVisibleCase {
    param([string] $ExerciseDirectory)

    $casesPath = Join-Path $ExerciseDirectory 'tests/visible/cases.json'
    if (-not (Test-Path -LiteralPath $casesPath -PathType Leaf)) { return $null }

    $document = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($casesPath))
    try {
        $cases = $document.RootElement.GetProperty('cases')
        if ($cases.GetArrayLength() -eq 0) { return $null }
        $case = $cases[0]

        $input = ConvertTo-CompactJson $case.GetProperty('arguments')

        $exception = $null
        $exceptionElement = [System.Text.Json.JsonElement]::new()
        if ($case.TryGetProperty('expectedException', [ref] $exceptionElement) -and
            $exceptionElement.ValueKind -eq [System.Text.Json.JsonValueKind]::String) {
            $exception = $exceptionElement.GetString()
        }

        $output = if ($exception) { "exception $exception" }
                  else { ConvertTo-CompactJson $case.GetProperty('expected') }

        return [pscustomobject]@{ Input = $input; Output = $output }
    }
    finally { $document.Dispose() }
}

$changed = [System.Collections.Generic.List[string]]::new()

# --- Exercices : énoncé et manifeste ------------------------------------------------------------
$exercisesRoot = Join-Path $ContentRoot 'reference/exercises'
if (Test-Path -LiteralPath $exercisesRoot -PathType Container) {
    foreach ($manifest in Get-ChildItem -LiteralPath $exercisesRoot -Filter 'exercise.json' -Recurse -File) {
        $directory = $manifest.Directory.FullName
        $example = Get-FirstVisibleCase -ExerciseDirectory $directory
        if (-not $example) {
            Write-Warning "Aucun cas visible exploitable : $($manifest.FullName)"
            continue
        }

        $statementPath = Join-Path $directory 'statement.md'
        if (Test-Path -LiteralPath $statementPath -PathType Leaf) {
            $statement = Read-ContentFile -Path $statementPath
            $line = "Exemple : entrée ``$($example.Input)``, sortie ``$($example.Output)``."
            $repaired = [regex]::Replace(
                $statement,
                '(?m)^Exemple\s*:.*$',
                { $line },
                [System.Text.RegularExpressions.RegexOptions]::None)
            if ($repaired -ne $statement) {
                if ($PSCmdlet.ShouldProcess($statementPath, 'Rétablir l''exemple publié')) {
                    Write-ContentFile -Path $statementPath -Text $repaired
                }
                $changed.Add($statementPath)
            }
        }

        # Le manifeste porte le même exemple : il alimente la page d'activité.
        $manifestText = Read-ContentFile -Path $manifest.FullName
        $document = [System.Text.Json.JsonDocument]::Parse($manifestText)
        try {
            $examples = $document.RootElement.GetProperty('examples')
            $currentInput = $examples[0].GetProperty('input').GetString()
            $currentOutput = $examples[0].GetProperty('output').GetString()
        }
        finally { $document.Dispose() }

        if ($currentInput -ne $example.Input -or $currentOutput -ne $example.Output) {
            $encodedInput = ConvertTo-Json -InputObject $example.Input -Compress
            $encodedOutput = ConvertTo-Json -InputObject $example.Output -Compress
            $repairedManifest = [regex]::Replace(
                $manifestText,
                '("examples"\s*:\s*\[\s*\{\s*"input"\s*:\s*)"(?:[^"\\]|\\.)*"(\s*,\s*"output"\s*:\s*)"(?:[^"\\]|\\.)*"',
                { param($match) "$($match.Groups[1].Value)$encodedInput$($match.Groups[2].Value)$encodedOutput" })
            if ($repairedManifest -eq $manifestText) {
                Write-Warning "Bloc « examples » non reconnu : $($manifest.FullName)"
            }
            else {
                if ($PSCmdlet.ShouldProcess($manifest.FullName, 'Rétablir l''exemple du manifeste')) {
                    Write-ContentFile -Path $manifest.FullName -Text $repairedManifest
                }
                $changed.Add($manifest.FullName)
            }
        }
    }
}

# --- Scénarios SQL : colonnes attendues dans l'énoncé -------------------------------------------
$sqlRoot = Join-Path $ContentRoot 'sql'
if (Test-Path -LiteralPath $sqlRoot -PathType Container) {
    foreach ($manifest in Get-ChildItem -LiteralPath $sqlRoot -Filter 'scenario.json' -Recurse -File) {
        $directory = $manifest.Directory.FullName
        $statementPath = Join-Path $directory 'statement.md'
        if (-not (Test-Path -LiteralPath $statementPath -PathType Leaf)) { continue }

        $document = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($manifest.FullName))
        try {
            $columns = @($document.RootElement.GetProperty('expectedResult').GetProperty('columns').EnumerateArray() |
                ForEach-Object { $_.GetString() })
        }
        finally { $document.Dispose() }

        if ($columns.Count -eq 0) {
            Write-Warning "Aucune colonne attendue déclarée : $($manifest.FullName)"
            continue
        }

        $statement = Read-ContentFile -Path $statementPath
        $joined = $columns -join ', '
        $repaired = [regex]::Replace($statement, '\$\(\([^()]*-join[^()]*\)\)', { $joined })
        if ($repaired -ne $statement) {
            if ($PSCmdlet.ShouldProcess($statementPath, 'Rétablir les colonnes attendues')) {
                Write-ContentFile -Path $statementPath -Text $repaired
            }
            $changed.Add($statementPath)
        }
    }
}

Write-Output "Fichiers modifiés : $($changed.Count)"
foreach ($path in $changed) {
    Write-Output ([System.IO.Path]::GetRelativePath($repositoryRoot, $path).Replace('\', '/'))
}
