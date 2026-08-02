[CmdletBinding()]
param(
    [switch]$SkipDockerTests
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$catalogRoot = Join-Path $repositoryRoot 'content\reference'
$exerciseRoot = Join-Path $catalogRoot 'exercises'
$expectedIds = @(
    'csharp-price-conversion-001',
    'csharp-shipping-decision-001',
    'csharp-loop-range-sum-001',
    'csharp-method-multiples-001',
    'csharp-array-differences-001',
    'csharp-list-distinct-001',
    'csharp-dictionary-stock-001',
    'csharp-string-frequency-001',
    'csharp-date-business-days-001',
    'csharp-date-expiry-001'
)

function Assert-Condition {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

Push-Location $repositoryRoot
try {
    & powershell -ExecutionPolicy Bypass -File scripts/validate-content.ps1 content/reference
    if ($LASTEXITCODE -ne 0) {
        throw "La validation du catalogue a échoué avec le code ${LASTEXITCODE}."
    }

    $manifests = @{}
    foreach ($exerciseId in $expectedIds) {
        $directory = Join-Path $exerciseRoot $exerciseId
        Assert-Condition -Condition (Test-Path -LiteralPath $directory -PathType Container) -Message "Exercice absent : ${exerciseId}."

        $manifestPath = Join-Path $directory 'exercise.json'
        $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
        $manifests[$exerciseId] = $manifest
        $hintCount = ($manifest.hints | Measure-Object).Count
        $hintLevels = ($manifest.hints | ForEach-Object { $_.level }) -join ','
        $mistakeCount = ($manifest.commonMistakes | Measure-Object).Count
        $reviewCardCount = ($manifest.reviewCards | Measure-Object).Count
        Assert-Condition -Condition ($manifest.id -ceq $exerciseId) -Message "Identifiant incohérent : ${exerciseId}."
        Assert-Condition -Condition ($manifest.version -eq 1) -Message "Version initiale inattendue : ${exerciseId}."
        Assert-Condition -Condition ($hintCount -eq 4) -Message "Quatre indices sont obligatoires : ${exerciseId}."
        Assert-Condition -Condition ($hintLevels -ceq '1,2,3,4') -Message "Les indices doivent etre H1-H4 : ${exerciseId}."
        Assert-Condition -Condition ($mistakeCount -ge 3) -Message "Les erreurs fréquentes sont insuffisantes : ${exerciseId}."
        Assert-Condition -Condition ($reviewCardCount -ge 2) -Message "Les cartes de révision sont insuffisantes : ${exerciseId}."
        Assert-Condition -Condition (-not [string]::IsNullOrWhiteSpace($manifest.interviewQuestionId)) -Message "Question d'entretien absente : ${exerciseId}."

        foreach ($relativePath in @(
            'statement.md',
            'explanation.md',
            'review-cards.md',
            'starter\Submission.cs',
            'solution\Submission.cs',
            'solution\README.md',
            'tests\runner.json',
            'tests\visible\cases.json',
            'tests\hidden\cases.json'
        )) {
            $path = Join-Path $directory $relativePath
            Assert-Condition -Condition ((Test-Path -LiteralPath $path -PathType Leaf) -and (Get-Item -LiteralPath $path).Length -gt 0) -Message "Fichier obligatoire absent ou vide : $exerciseId/${relativePath}."
        }

        $visible = Get-Content -Raw -LiteralPath (Join-Path $directory 'tests\visible\cases.json') | ConvertFrom-Json
        $hidden = Get-Content -Raw -LiteralPath (Join-Path $directory 'tests\hidden\cases.json') | ConvertFrom-Json
        $visibleCount = ($visible.cases | Measure-Object).Count
        $hiddenCount = ($hidden.cases | Measure-Object).Count
        $namedHiddenCount = ($hidden.cases | Where-Object { $_.name -like 'Hidden_*' } | Measure-Object).Count
        Assert-Condition -Condition ($visibleCount -eq 3) -Message "Trois cas visibles sont attendus : ${exerciseId}."
        Assert-Condition -Condition ($hiddenCount -eq 4) -Message "Quatre cas cachés sont attendus : ${exerciseId}."
        Assert-Condition -Condition ($namedHiddenCount -eq 4) -Message "Les cas cachés sont mal identifiés : ${exerciseId}."

        $interviewPath = Join-Path $catalogRoot ("interviews\{0}.json" -f $manifest.interviewQuestionId)
        Assert-Condition -Condition (Test-Path -LiteralPath $interviewPath -PathType Leaf) -Message "Question d'entretien introuvable : ${exerciseId}."
    }

    foreach ($exerciseId in $expectedIds) {
        $variantId = [string]$manifests[$exerciseId].variantId
        Assert-Condition -Condition ($manifests.ContainsKey($variantId)) -Message "Variante hors lot S1-S2 : ${exerciseId}."
        Assert-Condition -Condition ([string]($manifests[$variantId].variantId) -ceq $exerciseId) -Message "Variante non réciproque : ${exerciseId}."
    }

    if (-not $SkipDockerTests) {
        & dotnet test tests/ForgeDotNet.IntegrationTests/ForgeDotNet.IntegrationTests.csproj `
            --configuration Release `
            --no-build `
            --filter 'Category=InitialCSharpContent'
        if ($LASTEXITCODE -ne 0) {
            throw "Les preuves Docker du contenu initial ont échoué avec le code ${LASTEXITCODE}."
        }
    }

    Write-Output "CONTENU C# INITIAL VALIDE : $($expectedIds.Count) exercice(s)."
}
finally {
    Pop-Location
}
