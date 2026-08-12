<#
.SYNOPSIS
    Primitives partagées par les échafaudeurs de contenu S1-S24.

.DESCRIPTION
    Ces scripts ont produit soixante-dix leçons dont les sections de raisonnement étaient
    identiques par groupes de vingt-neuf et trente. La structure était valide, la pédagogie
    absente, et rien ne le refusait.

    Deux garde-fous ferment cette porte :

      1. Un fichier déjà présent n'est jamais réécrit sans -Force. Une leçon reprise à la main
         ne peut donc plus être détruite par une réexécution de l'échafaudeur.
      2. Les sections de raisonnement sont émises sous forme de marqueurs « TODO: ». La règle
         d'authenticité unsubstituted-placeholder les refuse, donc un échafaudage non rédigé
         ne peut pas être publié comme du contenu terminé.

    Le mode de défaillance est ainsi inversé : par défaut le contenu généré est refusé, et c'est
    la rédaction humaine qui le rend publiable.
#>

Set-StrictMode -Version Latest

$script:Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Write-TextFile {
    <#
    .PARAMETER Force
        Réécrit un fichier existant. Réservé à une régénération assumée : sans ce commutateur,
        un document déjà repris éditorialement est préservé et signalé.
    #>
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content,
        [switch]$Force
    )

    # U+02BC est utilisé dans les littéraux du script, car Windows PowerShell 5.1
    # interprète les apostrophes typographiques U+2019 comme des délimiteurs.
    $Content = $Content.Replace([char]0x02BC, [char]0x2019)
    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }

    # $script:ForceOverwrite est positionné une fois par l'échafaudeur appelant, ce qui évite
    # d'ajouter -Force sur la centaine de sites d'écriture.
    $overwrite = $Force -or $script:ForceOverwrite
    if (-not $overwrite -and [System.IO.File]::Exists($Path)) {
        Write-Verbose "Conservé (déjà présent) : $Path"
        $script:PreservedFiles += 1
        return
    }

    [System.IO.File]::WriteAllText($Path, ($Content.Trim() + [Environment]::NewLine), $script:Utf8NoBom)
    $script:WrittenFiles += 1
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Value,
        [switch]$Force
    )
    Write-TextFile -Path $Path -Content ($Value | ConvertTo-Json -Depth 32) -Force:$Force
}

function Write-PowerShellFile {
    # Windows PowerShell 5.1 exige la marque d'ordre des octets pour lire un script accentué.
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content,
        [switch]$Force
    )

    $Content = $Content.Replace([char]0x02BC, [char]0x2019)
    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }

    if (-not ($Force -or $script:ForceOverwrite) -and [System.IO.File]::Exists($Path)) {
        Write-Verbose "Conservé (déjà présent) : $Path"
        $script:PreservedFiles += 1
        return
    }

    [System.IO.File]::WriteAllText(
        $Path,
        ($Content.Trim() + [Environment]::NewLine),
        (New-Object System.Text.UTF8Encoding($true)))
    $script:WrittenFiles += 1
}

function Convert-JsonCompact {
    <#
        -InputObject est obligatoire : passer un tableau par le pipeline l'énumère, et la
        sérialisation produit alors une structure aplatie du type
        [{"value":[[1,3,5,7],5],"Count":2},2] au lieu de [[1,3,5,7],5].
    #>
    param($Value)
    return (ConvertTo-Json -InputObject $Value -Depth 16 -Compress)
}

function New-LessonScaffold {
    <#
    .SYNOPSIS
        Émet le squelette Markdown d'une leçon, sections de raisonnement laissées à rédiger.

    .DESCRIPTION
        Les quatorze sections et le bloc quiz sont imposés par SafeMarkdownLessonParser et par
        le schéma v1 : le squelette les respecte. En revanche aucune prose générique n'est
        produite, car c'est précisément ce qui avait été recopié soixante-dix fois.

        Les amorces Concept, Example et Mistake sont propres à la leçon : elles sont conservées
        comme matière de départ, jamais comme contenu final.
    #>
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Concept,
        [Parameter(Mandatory)][string]$Example,
        [Parameter(Mandatory)][string]$Mistake,
        [string]$PreviousLessonId,
        [string]$PrerequisiteNote = 'et savoir exécuter un exemple local sans réseau'
    )

    $prerequisite = if ([string]::IsNullOrWhiteSpace($PreviousLessonId)) {
        "TODO: nommer le ou les prérequis réels de cette leçon, $PrerequisiteNote."
    }
    else {
        "Relire la leçon précédente $PreviousLessonId $PrerequisiteNote."
    }

    return @"
# $Title

## Objectif observable

TODO: énoncer un objectif observable propre à « $Title », vérifiable par une production de lʼapprenant.

## Prérequis

$prerequisite

## Intuition

$Concept

## Explication

TODO: développer lʼintuition ci-dessus en 600 à 1 200 mots propres à cette leçon. Recopier
lʼintuition est refusé par la règle hollow-lesson.

## Exemple commenté

$Example

TODO: montrer cet exemple sous forme de bloc de code C# clôturé, commenté ligne par ligne. Une
leçon sans bloc de code est refusée par la règle hollow-lesson.

## Contre-exemple et erreur fréquente

$Mistake

TODO: donner le code fautif, le symptôme observable, puis la correction et le test qui échoue
avant celle-ci.

## Vérification de compréhension

TODO: poser la question de contrôle propre à cette leçon.

:::quiz
id=$Id-check
question=TODO: rédiger une question portant sur la notion de cette leçon
option=TODO: proposition correcte
option=TODO: erreur plausible
option=TODO: erreur plausible
correct=0
success=TODO: expliquer pourquoi cette réponse est la bonne
retry=TODO: indiquer quoi relire avant de réessayer
:::

## Exercice guidé

TODO: rattacher cette section à un exercice réel de content/reference/exercises et détailler ses étapes.

## Exercice autonome

TODO: décrire une transposition de la règle propre à cette leçon.

## Débogage

TODO: décrire un défaut reproductible lié à cette notion et la méthode dʼinvestigation attendue.

## Entretien

TODO: formuler la question dʼentretien propre à cette notion et ses critères observables.

## Résumé

- TODO: premier point de synthèse propre à cette leçon.
- TODO: deuxième point de synthèse propre à cette leçon.
- TODO: troisième point de synthèse propre à cette leçon.

## Cartes de révision

- Question : TODO. Réponse attendue : TODO.
- Question : TODO. Réponse attendue : TODO.

## Test de maîtrise

TODO: décrire lʼépreuve sans aide propre à cette leçon. Cette auto-évaluation ne crée aucune
maîtrise automatique.
"@
}

function Write-ScaffoldSummary {
    param([Parameter(Mandatory)][string]$ScriptName)
    Write-Output "$ScriptName : $script:WrittenFiles fichier(s) écrit(s), $script:PreservedFiles conservé(s)."
    if ($script:PreservedFiles -gt 0) {
        Write-Output 'Les fichiers conservés existaient déjà. Utiliser -Force pour les régénérer.'
    }
}

$script:WrittenFiles = 0
$script:PreservedFiles = 0
if (-not (Get-Variable -Name ForceOverwrite -Scope Script -ErrorAction SilentlyContinue)) {
    $script:ForceOverwrite = $false
}
