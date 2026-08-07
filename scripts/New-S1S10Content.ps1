[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$EffectiveScriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) { Join-Path (Get-Location) 'scripts' } else { $PSScriptRoot }
$RepositoryRoot = Split-Path -Parent $EffectiveScriptRoot
$ContentRoot = Join-Path $RepositoryRoot 'content'
$CatalogRoot = Join-Path $ContentRoot 'reference'
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Write-TextFile {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Content)
    # U+02BC est utilisé dans les littéraux du script, car Windows PowerShell 5.1
    # interprète les apostrophes typographiques U+2019 comme des délimiteurs.
    $Content = $Content.Replace([char]0x02BC, [char]0x2019)
    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, ($Content.Trim() + [Environment]::NewLine), $Utf8NoBom)
}

function Write-JsonFile {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)]$Value)
    Write-TextFile -Path $Path -Content ($Value | ConvertTo-Json -Depth 32)
}

function Convert-TypeName {
    param([Parameter(Mandatory)][string]$RunnerType)
    switch ($RunnerType) {
        'bool' { 'bool' }
        'date' { 'System.DateOnly' }
        'decimal' { 'decimal' }
        'dictionary<string,int>' { 'System.Collections.Generic.Dictionary<string, int>' }
        'int' { 'int' }
        'int[]' { 'int[]' }
        'list<int>' { 'System.Collections.Generic.List<int>' }
        'string' { 'string' }
        default { throw "Type runner inconnu : $RunnerType" }
    }
}

function Convert-JsonCompact {
    param($Value)
    return ($Value | ConvertTo-Json -Depth 16 -Compress)
}

$LessonRows = @(
    'csharp-control-methods-001§1§Contrôle et méthodes prévisibles§csharp.control§Une branche traduit une règle métier et une méthode nommée isole une responsabilité. Les gardes traitent dʼabord les entrées invalides, puis le chemin nominal reste lisible.§Une fonction de tarif valide le total, choisit express ou standard, puis retourne un montant sans modifier ses entrées.§Mélanger lecture console, calcul et affichage rend les cas limites difficiles à tester.',
    'csharp-io-debugger-001§1§Entrées locales et premiers breakpoints§debugging.breakpoints§Une entrée externe est du texte non fiable. Il faut la convertir explicitement et observer les valeurs au breakpoint avant dʼaccuser la ligne qui échoue.§TryParse sépare la validation du calcul ; un breakpoint conditionnel permet dʼarrêter seulement sur la valeur qui reproduit le ticket.§Utiliser Parse sans message dʼerreur transforme une donnée attendue invalide en crash opaque.',
    'collections-arrays-001§2§Tableaux et listes : choisir la bonne forme§csharp.collections§Un tableau exprime une taille fixe tandis quʼune liste exprime une collection qui évolue. Le choix doit rendre les mutations intentionnelles.§Une transformation retourne un nouveau tableau de différences et ne modifie pas le tableau reçu.§Ajouter ou retirer pendant une énumération invalide lʼitérateur et masque souvent un problème de responsabilité.',
    'strings-dates-001§2§Chaînes, dates et culture explicite§csharp.dates§Une chaîne est immuable et une date métier ne doit pas dépendre du fuseau de la machine. DateOnly convient aux jours sans heure.§Comparer deux DateOnly puis avancer avec AddDays évite les conversions implicites en heure locale.§Comparer des dates formatées comme texte ou appliquer ToLower sans culture explicite produit des résultats fragiles.',
    'edge-cases-001§2§Concevoir les cas limites avant la boucle§logic.edge-cases§Vide, borne incluse, doublon et valeur absente font partie du contrat, pas dʼune correction tardive. Une table de cas précède lʼimplémentation.§Pour compter des jours ouvrés, la table couvre même jour, week-end, ordre inversé et intervalle traversant deux semaines.§Tester seulement lʼexemple nominal autorise les erreurs de borne et les réponses codées en dur.',
    'oop-encapsulation-001§3§Classes et invariants encapsulés§csharp.encapsulation§Une classe utile protège un invariant à chaque construction et mutation. Ses méthodes portent le vocabulaire métier au lieu dʼexposer des champs modifiables.§Une réservation refuse une quantité négative et ne laisse jamais le stock devenir inférieur à zéro.§Un setter public sur chaque propriété déplace les règles chez tous les appelants et permet des états impossibles.',
    'oop-interfaces-composition-001§3§Interfaces ciblées et composition§csharp.composition§Une petite interface décrit une capacité observable. La composition assemble ces capacités sans imposer une hiérarchie artificielle.§Un calculateur de remise reçoit une politique et un arrondisseur ; chaque dépendance peut être testée séparément.§Une classe de base omnisciente couple des règles indépendantes et rend les variantes difficiles à raisonner.',
    'csharp-exceptions-nullable-001§3§Exceptions et nullable sans ambiguïté§csharp.nullable§Une valeur absente attendue se modélise ; une violation de contrat se refuse. Le type nullable et lʼexception ne sont pas deux orthographes du même état.§Une recherche retourne null quand lʼabsence est normale, alors quʼune quantité négative déclenche ArgumentOutOfRangeException.§Attraper Exception puis retourner zéro efface la cause et fabrique une donnée valide.',
    'generics-delegates-001§4§Génériques et delegates utiles§csharp.generics§Un générique conserve le type lorsque lʼalgorithme ne dépend pas dʼun type concret. Un delegate injecte un comportement court et explicite.§Une transformation applique une fonction à chaque entier et produit une nouvelle collection sans connaître la formule.§Employer object puis caster partout reporte les erreurs à lʼexécution et obscurcit le contrat.',
    'linq-lambdas-001§4§LINQ : requêtes lisibles et évaluations maîtrisées§csharp.linq§Une chaîne LINQ décrit filtrage, projection et agrégation. Il faut savoir quand elle est évaluée et éviter les énumérations répétées.§Where filtre les montants positifs, Select les normalise et ToArray fige le résultat une seule fois.§Cacher des effets de bord dans une lambda rend le nombre et lʼordre des appels difficiles à prévoir.',
    'files-json-001§4§Fichiers et JSON local robustes§csharp.files§Le chemin, lʼencodage et le schéma sont des entrées. Lire localement exige une taille bornée, UTF-8 explicite et un diagnostic de ligne exploitable.§Un import lit chaque ligne, valide les champs, accumule les rejets puis écrit un rapport JSON déterministe.§Désérialiser directement vers un domaine sans valider les champs confond syntaxe valide et donnée métier valide.',
    'algo-reformulation-complexity-001§5§Reformuler et estimer avant de coder§algorithm.complexity§Une reformulation précise entrées, sortie, invariants et bornes. La complexité compare la croissance des opérations, pas une durée isolée.§Deux boucles successives sur n éléments restent O(n), tandis quʼune boucle imbriquée complète est O(n²).§Annoncer O(1) parce que le jeu de test est petit confond mesure actuelle et croissance.',
    'algo-search-001§5§Recherche linéaire et binaire§algorithm.search§La recherche linéaire accepte toute séquence. La recherche binaire exige une séquence triée et réduit un intervalle avec des bornes prouvées.§Avec gauche inférieur ou égal à droite, le milieu est testé puis une borne est déplacée au-delà du milieu.§Appliquer une recherche binaire à des données non triées peut réussir sur quelques exemples et rester incorrect.',
    'algo-simple-sorts-001§5§Tris simples et invariants de boucle§algorithm.sorting§Les tris insertion, sélection et bulles illustrent des invariants différents. Ils servent à expliquer correction et coût, pas à remplacer Array.Sort en production.§Après chaque tour du tri par sélection, le préfixe est trié et contient les plus petits éléments.§Échanger au mauvais indice peut conserver les mêmes valeurs tout en laissant lʼordre incorrect.',
    'structures-stacks-queues-001§6§Piles et files par intention§structures.linear§Une pile traite le dernier arrivé en premier ; une file traite le premier arrivé en premier. Le nom de la structure documente lʼordre attendu.§Une pile vérifie les parenthèses imbriquées ; une file conserve lʼordre des travaux reçus.§Employer une liste avec des insertions en tête sans analyser le coût cache une opération O(n).',
    'structures-dictionaries-recursion-001§6§Dictionnaires et récursivité bornée§structures.recursion§Un dictionnaire échange mémoire contre accès par clé. Une récursion utile définit un cas de base, réduit strictement le problème et reste bornée.§Le calcul du PGCD remplace le couple par diviseur et reste jusquʼau reste nul.§Une récursion qui rappelle les mêmes arguments ne progresse jamais et finit par épuiser la pile.',
    'structures-trees-001§6§Arbres minimaux et parcours§structures.trees§Un arbre relie un nœud à des enfants sans cycle. Profondeur, hauteur et ordre de parcours doivent être définis avant lʼalgorithme.§Un parcours en largeur utilise une file ; un parcours en profondeur peut utiliser une pile ou une récursion bornée.§Confondre index de tableau et identité de nœud donne des résultats corrects seulement pour un arbre complet.',
    'debug-stacktraces-breakpoints-001§7§Lire une stack trace et cibler un breakpoint§debugging.stacktrace§La première ligne applicative utile situe le symptôme, pas forcément la cause. Un breakpoint conditionnel teste une hypothèse précise.§Conserver la stack trace avec throw permet de remonter au véritable appelant après journalisation locale.§Utiliser throw exception recrée le point de départ et détruit une partie de la preuve.',
    'debug-data-inspection-001§7§Inspecter les données sans les modifier§debugging.data§Watch et Locals servent à comparer attendu et réel. Une expression dʼobservation ne doit pas modifier la collection étudiée.§Copier un tableau avant un tri exploratoire préserve lʼentrée et permet de comparer les deux états.§Appeler une méthode mutante depuis Watch change le programme que lʼon cherche à comprendre.',
    'async-fundamentals-001§7§Async simple, ordre et annulation§csharp.async§Une Task représente un travail futur. await conserve les exceptions et lʼordre logique sans bloquer un thread avec Wait ou Result.§Une méthode accepte CancellationToken, le transmet et laisse OperationCanceledException signaler lʼannulation attendue.§Lancer une tâche sans lʼattendre peut perdre son exception et terminer avant son effet.',
    'sql-relational-constraints-001§8§Modèle relationnel et contraintes§sql.relational-model§Une clé primaire identifie, une clé étrangère relie et une contrainte protège une règle au plus près des données. Les types reflètent le domaine.§Orders référence Customers et interdit un total négatif ; une ligne orpheline est rejetée par la base.§Compter sur lʼinterface seule permet à un autre chemin dʼécriture de créer des données incohérentes.',
    'sql-select-filters-001§8§SELECT et filtres déterministes§sql.select§Une requête sélectionne seulement les colonnes utiles, exprime les nulls et ajoute un ordre lorsque lʼordre fait partie du résultat.§WHERE filtre avant projection et ORDER BY OrderId rend la pagination de démonstration stable.§SELECT étoile couple le résultat aux changements de schéma et transfère des colonnes inutiles.',
    'sql-joins-001§8§Jointures et cardinalités§sql.joins§Une jointure combine des lignes selon une relation. INNER exclut les absences ; LEFT conserve le côté gauche et exige un traitement explicite de null.§Un LEFT JOIN suivi dʼun filtre sur la table droite dans WHERE peut devenir involontairement un INNER JOIN.§Joindre sans comprendre la cardinalité multiplie les lignes et fausse les agrégats.',
    'sql-aggregations-subqueries-001§9§Agrégations et sous-requêtes§sql.aggregation§GROUP BY fixe le grain du résultat. Une sous-requête corrélée doit être examinée pour son coût et sa sémantique par ligne.§HAVING filtre les groupes après COUNT tandis que WHERE filtre les lignes avant regroupement.§Sélectionner une colonne ni agrégée ni groupée produit une requête invalide ou ambiguë.',
    'sql-cte-transactions-001§9§CTE et transactions atomiques§sql.transactions§Une CTE nomme une étape de requête ; une transaction groupe des effets qui doivent réussir ou échouer ensemble.§Débiter un stock et créer une ligne de commande appartiennent à la même transaction et sont annulés ensemble en cas dʼerreur.§Commencer une transaction sans gérer lʼéchec laisse croire à une atomicité que le code nʼassure pas.',
    'sql-isolation-001§9§Isolation et anomalies observables§sql.isolation§Le niveau dʼisolation choisit quelles anomalies concurrentes sont possibles. Il faut raisonner sur lectures sales, non répétables et fantômes.§Deux sessions contrôlées démontrent une lecture répétée avant de choisir un niveau plus strict.§Ajouter NOLOCK partout échange la cohérence contre des anomalies sans décision métier.',
    'sql-index-plans-001§10§Index et plans sans coûts fragiles§sql.indexes§Un index sert un motif de filtre et dʼordre ; il coûte en écriture et en espace. Un plan se vérifie par propriétés stables, jamais par un coût exact.§Un index sur CustomerId puis OrderDate soutient le filtre client et lʼordre chronologique.§Créer un index par requête sans mesurer les écritures produit redondance et maintenance inutile.',
    'sql-pagination-001§10§Pagination stable et bornée§sql.pagination§Une pagination exige un ordre total. OFFSET convient à certains écrans ; une clé de continuation évite de rescanner un grand préfixe.§Le couple OrderDate, OrderId départage les égalités et permet une pagination par clé stable.§Paginer sans ORDER BY rend les pages non reproductibles et peut dupliquer ou perdre des lignes.',
    'ef-core-data-access-001§10§EF Core : tracking, chargement et concurrence§efcore.data-access§DbContext représente une unité de travail courte. Le tracking sert aux mutations ; AsNoTracking et les projections servent aux lectures ciblées.§Une projection calcule le résumé dans SQL et évite de charger chaque collection, supprimant le N+1.§Conserver un DbContext global mélange unités de travail, mémoire suivie et conflits concurrents.'
)

$LessonIdsByWeek = @{}
$previousLessonId = 'reference-types-001'
foreach ($row in $LessonRows) {
    $parts = $row -split '§', 7
    $id = $parts[0]
    $week = [int]$parts[1]
    $title = $parts[2]
    $skill = $parts[3]
    $concept = $parts[4]
    $example = $parts[5]
    $mistake = $parts[6]
    if (-not $LessonIdsByWeek.ContainsKey($week)) { $LessonIdsByWeek[$week] = New-Object System.Collections.Generic.List[string] }
    $LessonIdsByWeek[$week].Add($id)
    $directory = Join-Path $CatalogRoot "curriculum/lessons/$id"
    $manifest = [ordered]@{
        schemaVersion = 1; id = $id; version = 1; title = $title; week = $week
        skills = @([ordered]@{ id = $skill; weight = 1.0 })
        prerequisites = @($previousLessonId); estimatedMinutes = 70
        objectives = @("Expliquer puis appliquer $title sur un cas métier borné")
        sections = @('intuition','explanation','example','counterExample','check','guided','independent','debugging','interview','summary','reviewCards','masteryTest')
        markdownPath = 'lesson.md'; license = 'CC-BY-4.0'
    }
    Write-JsonFile (Join-Path $directory 'lesson.json') $manifest
    $markdown = @"
# $title

## Objectif observable

À la fin de la leçon, vous pourrez expliquer le compromis principal, appliquer la règle sur une entrée nouvelle et écrire un test qui distingue le comportement correct dʼune erreur plausible.

## Prérequis

Relire la leçon précédente `$previousLessonId` et savoir exécuter un exemple local sans réseau.

## Intuition

$concept

## Explication

$concept La règle doit rester visible dans le nom des opérations, les bornes et les erreurs. Avant de coder, notez lʼentrée, la sortie, les invariants et ce qui doit être refusé.

## Exemple commenté

$example Lʼexemple est volontairement petit : changez une borne et une valeur absente pour vérifier que le raisonnement, et non la donnée mémorisée, produit le résultat.

## Contre-exemple et erreur fréquente

$mistake Le contre-exemple doit être reproduit par un test qui échoue avant correction et réussit après.

## Vérification de compréhension

Expliquez en deux phrases la précondition, lʼinvariant et le cas limite principal. Si lʼun des trois manque, revenez à lʼexplication avant de poursuivre.

:::quiz
id=$id-check
question=Quelle preuve montre que la règle de cette leçon est comprise ?
option=Répéter uniquement lʼexemple mot pour mot
option=Prédire puis tester un cas nominal, une borne et une erreur plausible
option=Lire la solution sans écrire de test
correct=1
success=Correct : la variation des données et la borne distinguent une règle comprise dʼun exemple mémorisé.
retry=Revenez au contrat, à lʼinvariant et au contre-exemple, puis choisissez la preuve qui pourrait réellement échouer.
:::

## Exercice guidé

1. Écrivez trois cas : nominal, borne et entrée invalide ou absente.
2. Prédisez chaque résultat sans exécuter.
3. Implémentez la règle minimale.
4. Comparez les résultats et nommez toute hypothèse incorrecte.

## Exercice autonome

Transposez la règle à un petit domaine de commandes. Conservez une signature testable, refusez les états impossibles et justifiez la complexité en fonction du volume dʼentrée.

## Débogage

Reproduisez dʼabord le contre-exemple. Placez un breakpoint à la première divergence, inspectez les données sans les modifier, puis consignez symptôme, hypothèse, preuve, cause, correction et test de non-régression.

## Entretien

Présentez le compromis à voix haute en cinq minutes : définition, exemple, erreur fréquente, méthode de test et situation où vous choisiriez une autre approche.

## Résumé

- Le contrat et les bornes précèdent lʼimplémentation.
- Une règle utile est observable par un test qui pourrait échouer.
- Une erreur nʼest corrigée quʼaprès reproduction et preuve.

## Cartes de révision

- Question : quel invariant protège cette technique ? Réponse attendue : le candidat doit citer lʼinvariant décrit dans lʼexplication.
- Question : quel test réfute lʼerreur fréquente ? Réponse attendue : un cas limite qui échoue avant la correction.

## Test de maîtrise

Sans relire, résolvez une variante avec une borne différente, écrivez un test nominal et deux cas limites, puis expliquez la complexité et la preuve de non-régression. Cette auto-évaluation ne crée aucune maîtrise automatique.
"@
    Write-TextFile (Join-Path $directory 'lesson.md') $markdown
    $previousLessonId = $id
}

$LessonIdsByWeek[1].Insert(0, 'reference-types-001')

# Les exercices sont décrits par des lignes compactes puis matérialisés avec leurs artefacts privés.
$ExerciseRows = @(
    'reference-total-001§1§Additionner deux montants§csharp§1§csharp.types§AddAmounts§decimal:first,decimal:second§decimal§return first + second;§[[[10,5.5],15.5],[[0,2.25],2.25]]§[[[0.01,0.02],0.03],[[999.99,0.01],1000]]§Additionner les deux valeurs decimal sans conversion binaire.§O(1) en temps et O(1) en espace',
    'reference-total-002§1§Appliquer une remise bornée§csharp§2§csharp.control§ApplyDiscount§decimal:total,decimal:rate§decimal§if (total < 0m || rate < 0m || rate > 1m) throw new System.ArgumentOutOfRangeException(); return decimal.Round(total * (1m - rate), 2, System.MidpointRounding.AwayFromZero);§[[[20,0.1],18],[[0,0.5],0]]§[[[10.05,0.5],5.03],[[10,-0.1],"!ArgumentOutOfRangeException"]]§Valider total et taux puis arrondir une seule fois le net.§O(1) en temps et O(1) en espace',
    'csharp-input-normalize-001§1§Normaliser une saisie locale§csharp§1§csharp.conversions§NormalizeInput§string:value§string§if (value is null) throw new System.ArgumentNullException(nameof(value)); return value.Trim();§[[["  Ada  "],"Ada"],[["commande"],"commande"]]§[[["   "],""],[[" x y "],"x y"]]§Retirer seulement les espaces de bord sans modifier le contenu interne.§O(n) en temps et O(n) pour la chaîne produite',
    'csharp-temperature-band-001§1§Classer une température§csharp§1§csharp.control§TemperatureBand§int:celsius§string§if (celsius < 0) return "gel"; if (celsius < 20) return "frais"; return "chaud";§[[[-1],"gel"],[[12],"frais"]]§[[[0],"frais"],[[20],"chaud"]]§Traiter les bornes zéro et vingt comme des décisions explicites.§O(1) en temps et O(1) en espace',
    'csharp-even-counter-001§1§Compter les entiers pairs§csharp§1§csharp.methods§CountEven§int[]:values§int§if (values is null) throw new System.ArgumentNullException(nameof(values)); int count = 0; foreach (int value in values) if (value % 2 == 0) count++; return count;§[[[[1,2,4]],2],[[[]],0]]§[[[[-2,-1,0]],2],[[[3,5,7]],0]]§Parcourir chaque valeur une fois et compter aussi zéro et les pairs négatifs.§O(n) en temps et O(1) en espace',
    'csharp-clamp-value-001§1§Borner une valeur§csharp§2§logic.edge-cases§Clamp§int:value,int:minimum,int:maximum§int§if (minimum > maximum) throw new System.ArgumentException("Bornes inversées."); if (value < minimum) return minimum; if (value > maximum) return maximum; return value;§[[[5,1,10],5],[[0,1,10],1]]§[[[11,1,10],10],[[2,4,3],"!ArgumentException"]]§Refuser les bornes inversées puis traiter inférieur, supérieur et intervalle.§O(1) en temps et O(1) en espace',
    'csharp-array-positive-sum-001§2§Sommer les valeurs positives§csharp§1§csharp.arrays§PositiveSum§int[]:values§int§if (values is null) throw new System.ArgumentNullException(nameof(values)); int sum = 0; foreach (int value in values) if (value > 0) sum = checked(sum + value); return sum;§[[[[1,-2,3]],4],[[[]],0]]§[[[[-1,-2]],0],[[[5,0,4]],9]]§Ignorer zéro et les négatifs sans modifier le tableau reçu.§O(n) en temps et O(1) en espace',
    'csharp-string-palindrome-001§2§Détecter un palindrome simple§csharp§2§csharp.strings§IsPalindrome§string:value§bool§if (value is null) throw new System.ArgumentNullException(nameof(value)); string text = value.Replace(" ", "", System.StringComparison.Ordinal).ToLowerInvariant(); for (int left = 0, right = text.Length - 1; left < right; left++, right--) if (text[left] != text[right]) return false; return true;§[[["radar"],true],[["chat"],false]]§[[["Esope reste ici et se repose"],true],[[""],true]]§Comparer les caractères symétriques après une normalisation annoncée.§O(n) en temps et O(n) en espace',
    'csharp-date-span-001§2§Compter des jours inclusifs§csharp§2§csharp.dates§InclusiveDays§date:start,date:end§int§if (end < start) return 0; return end.DayNumber - start.DayNumber + 1;§[[["2026-07-01","2026-07-01"],1],[["2026-07-01","2026-07-03"],3]]§[[["2026-12-31","2027-01-01"],2],[["2026-07-03","2026-07-01"],0]]§Compter les deux bornes et retourner zéro pour un intervalle inversé.§O(1) en temps et O(1) en espace',
    'csharp-dictionary-default-001§2§Lire un stock absent§csharp§2§csharp.dictionaries§StockFor§dictionary<string,int>:stock,string:key§int§if (stock is null) throw new System.ArgumentNullException(nameof(stock)); if (string.IsNullOrWhiteSpace(key)) return 0; return stock.TryGetValue(key, out int value) ? value : 0;§[[[{"pen":3},"pen"],3],[[{"pen":3},"book"],0]]§[[[{},"pen"],0],[[{"pen":3}," "],0]]§Distinguer une clé absente dʼune exception et ne jamais muter le dictionnaire.§O(1) amorti en temps et O(1) en espace',
    'csharp-order-status-001§3§Traduire un état de commande§csharp§1§csharp.encapsulation§StatusLabel§int:status§string§return status switch { 0 => "draft", 1 => "paid", 2 => "shipped", _ => "unknown" };§[[[0],"draft"],[[2],"shipped"]]§[[[1],"paid"],[[99],"unknown"]]§Rendre le cas inconnu explicite au lieu dʼinventer un état valide.§O(1) en temps et O(1) en espace',
    'csharp-customer-name-001§3§Normaliser un nom client§csharp§1§csharp.nullable§NormalizeCustomer§string:name§string§if (string.IsNullOrWhiteSpace(name)) return "(inconnu)"; return name.Trim().ToUpperInvariant();§[[[" ada "],"ADA"],[[""],"(inconnu)"]]§[[["   "],"(inconnu)"],[["Zoë"],"ZOË"]]§Traiter absence et blanc avant toute déréférence ou conversion.§O(n) en temps et O(n) en espace',
    'csharp-stock-reservation-001§3§Vérifier une réservation de stock§csharp§2§csharp.encapsulation§CanReserve§int:stock,int:requested§bool§if (stock < 0 || requested < 0) return false; return requested <= stock;§[[[5,3],true],[[5,6],false]]§[[[0,0],true],[[-1,0],false]]§Préserver lʼinvariant stock positif et accepter exactement la quantité disponible.§O(1) en temps et O(1) en espace',
    'csharp-vip-discount-001§3§Appliquer une politique VIP§csharp§2§csharp.composition§NetTotal§decimal:total,bool:isVip§decimal§if (total < 0m) throw new System.ArgumentOutOfRangeException(nameof(total)); decimal rate = isVip ? 0.10m : 0m; return decimal.Round(total * (1m - rate), 2, System.MidpointRounding.AwayFromZero);§[[[100,true],90],[[100,false],100]]§[[[10.05,true],9.05],[[-1,false],"!ArgumentOutOfRangeException"]]§Choisir la politique puis arrondir au point métier annoncé.§O(1) en temps et O(1) en espace',
    'csharp-nullable-fallback-001§3§Choisir une valeur de repli§csharp§1§csharp.nullable§Fallback§string:value§string§return string.IsNullOrWhiteSpace(value) ? "n/a" : value.Trim();§[[[" ok "],"ok"],[[""],"n/a"]]§[[["   "],"n/a"],[["0"],"0"]]§Une absence attendue reçoit un repli explicite, distinct dʼune erreur métier.§O(n) en temps et O(n) en espace',
    'csharp-positive-quantity-001§3§Refuser une quantité négative§csharp§2§csharp.exceptions§RequireQuantity§int:value§int§if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value)); return value;§[[[3],3],[[0],0]]§[[[-1],"!ArgumentOutOfRangeException"],[[100],100]]§Lever une exception de contrat seulement pour une valeur négative.§O(1) en temps et O(1) en espace',
    'csharp-line-total-001§3§Composer un total de ligne§csharp§2§csharp.composition§LineTotal§decimal:unitPrice,int:quantity§decimal§if (unitPrice < 0m || quantity < 0) throw new System.ArgumentOutOfRangeException(); return decimal.Round(unitPrice * quantity, 2, System.MidpointRounding.AwayFromZero);§[[[2.5,3],7.5],[[0,4],0]]§[[[1.005,2],2.01],[[2,-1],"!ArgumentOutOfRangeException"]]§Valider les deux invariants puis multiplier avant lʼunique arrondi.§O(1) en temps et O(1) en espace',
    'csharp-payment-fee-001§3§Calculer des frais par interface§csharp§2§csharp.interfaces§PaymentFee§decimal:amount,bool:isCard§decimal§if (amount < 0m) throw new System.ArgumentOutOfRangeException(nameof(amount)); decimal rate = isCard ? 0.015m : 0m; return decimal.Round(amount * rate, 2, System.MidpointRounding.AwayFromZero);§[[[100,true],1.5],[[100,false],0]]§[[[10.1,true],0.15],[[-1,true],"!ArgumentOutOfRangeException"]]§La branche représente deux implémentations dʼune petite politique de frais.§O(1) en temps et O(1) en espace',
    'csharp-age-group-001§3§Classer un âge valide§csharp§1§csharp.control§AgeGroup§int:age§string§if (age < 0) return "invalid"; if (age < 18) return "minor"; if (age < 65) return "adult"; return "senior";§[[[17],"minor"],[[18],"adult"]]§[[[-1],"invalid"],[[65],"senior"]]§Tester chaque frontière et conserver un état invalide distinct.§O(1) en temps et O(1) en espace',
    'csharp-copy-sanitized-001§3§Copier sans exposer la mutation§csharp§2§csharp.encapsulation§SanitizeCopy§int[]:values§int[]§if (values is null) throw new System.ArgumentNullException(nameof(values)); int[] copy = new int[values.Length]; for (int i = 0; i < values.Length; i++) copy[i] = System.Math.Max(0, values[i]); return copy;§[[[[-1,2]],[0,2]],[[[]],[]]]§[[[[3,-4,5]],[3,0,5]],[[[0]],[0]]]§Retourner une nouvelle collection et préserver strictement lʼentrée.§O(n) en temps et O(n) en espace',
    'csharp-generic-maximum-001§4§Trouver un maximum générique illustré§csharp§2§csharp.generics§MaximumOrZero§int[]:values§int§if (values is null || values.Length == 0) return 0; int max = values[0]; foreach (int value in values) if (value > max) max = value; return max;§[[[[1,5,2]],5],[[[]],0]]§[[[[-5,-2]],-2],[[[7]],7]]§Initialiser depuis une donnée réelle et définir explicitement le cas vide.§O(n) en temps et O(1) en espace',
    'csharp-delegate-double-001§4§Transformer avec un comportement injecté§csharp§2§csharp.delegates§DoubleAll§int[]:values§int[]§if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Array.ConvertAll(values, value => checked(value * 2));§[[[[1,2]],[2,4]],[[[]],[]]]§[[[[-2,0]],[ -4,0]],[[[7]],[14]]]§Appliquer le même delegate à chaque élément sans modifier lʼentrée.§O(n) en temps et O(n) en espace',
    'csharp-lambda-threshold-001§4§Compter avec une lambda§csharp§2§csharp.lambdas§CountAtLeast§int[]:values,int:minimum§int§if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.Count(values, value => value >= minimum);§[[[[1,3,5],3],2],[[[],0],0]]§[[[[-1,0,1],0],2],[[[4,4],4],2]]§La lambda exprime le prédicat et la borne reste inclusive.§O(n) en temps et O(1) en espace',
    'csharp-linq-even-sum-001§4§Agréger les pairs avec LINQ§csharp§2§csharp.linq§EvenSum§int[]:values§int§if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.Sum(System.Linq.Enumerable.Where(values, value => value % 2 == 0));§[[[[1,2,4]],6],[[[]],0]]§[[[[-2,-1,2]],0],[[[3,5]],0]]§Filtrer puis agréger sans énumérer la séquence plusieurs fois.§O(n) en temps et O(1) en espace',
    'csharp-linq-top-three-001§4§Sélectionner les trois plus grands§csharp§3§csharp.linq§TopThree§int[]:values§int[]§if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(values, value => value), 3));§[[[[1,5,3,4]],[5,4,3]],[[[2]],[2]]]§[[[[]],[]],[[[3,3,1]],[3,3,1]]]§Ordonner de façon descendante puis borner sans supprimer les doublons.§O(n log n) en temps et O(n) en espace',
    'csharp-csv-field-count-001§4§Compter des champs CSV bornés§csharp§1§csharp.files§FieldCount§string:line§int§if (string.IsNullOrEmpty(line)) return 0; return line.Split(",", System.StringSplitOptions.None).Length;§[[["a,b,c"],3],[[""],0]]§[[["a,,c"],3],[["single"],1]]§Conserver les champs vides ; ce micro-exercice nʼannonce pas gérer tout RFC 4180.§O(n) en temps et O(n) en espace',
    'csharp-file-extension-001§4§Normaliser une extension locale§csharp§1§csharp.files§ExtensionOf§string:path§string§if (string.IsNullOrWhiteSpace(path)) return ""; return System.IO.Path.GetExtension(path).ToLowerInvariant();§[[["report.JSON"],".json"],[["readme"],""]]§[[["archive.tar.gz"],".gz"],[[" "],""]]§Utiliser Path et retourner uniquement la dernière extension normalisée.§O(n) en temps et O(n) en espace',
    'csharp-distinct-sorted-001§4§Figer des valeurs distinctes triées§csharp§2§csharp.linq§DistinctSorted§int[]:values§int[]§if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Distinct(values), value => value));§[[[[3,1,3]],[1,3]],[[[]],[]]]§[[[[-1,0,-1]],[-1,0]],[[[2,2,2]],[2]]]§Dédupliquer avant de trier et matérialiser exactement une fois.§O(n log n) en temps et O(n) en espace',
    'csharp-average-001§4§Calculer une moyenne décimale§csharp§2§csharp.linq§AverageOrZero§int[]:values§decimal§if (values is null || values.Length == 0) return 0m; long sum = 0; foreach (int value in values) sum += value; return (decimal)sum / values.Length;§[[[[1,2,3]],2],[[[]],0]]§[[[[-1,1]],0],[[[1,2]],1.5]]§Utiliser une somme élargie et définir la collection vide.§O(n) en temps et O(1) en espace',
    'csharp-filter-minimum-001§4§Filtrer une collection§csharp§2§csharp.linq§AtLeast§int[]:values,int:minimum§int[]§if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(values, value => value >= minimum));§[[[[1,3,2],2],[3,2]],[[[],0],[]]]§[[[[-1,0,1],0],[0,1]],[[[2,2],2],[2,2]]]§Préserver lʼordre dʼentrée et inclure la borne.§O(n) en temps et O(n) en espace',
    'csharp-absolute-map-001§4§Projeter des valeurs absolues§csharp§2§csharp.lambdas§AbsoluteAll§int[]:values§int[]§if (values is null) throw new System.ArgumentNullException(nameof(values)); return System.Array.ConvertAll(values, value => System.Math.Abs(value));§[[[[-2,0,3]],[2,0,3]],[[[]],[]]]§[[[[-1,-4]],[1,4]],[[[5]],[5]]]§Projeter vers un nouveau tableau et ne pas muter la source.§O(n) en temps et O(n) en espace',
    'csharp-word-histogram-001§4§Construire un histogramme de mots§csharp§3§csharp.dictionaries§WordHistogram§string:text§dictionary<string,int>§var result = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase); if (string.IsNullOrWhiteSpace(text)) return result; foreach (string word in text.Split(" ", System.StringSplitOptions.RemoveEmptyEntries)) result[word] = result.TryGetValue(word, out int count) ? count + 1 : 1; return result;§[[["chat chien chat"],{"chat":2,"chien":1}],[[""],{}]]§[[["A a"],{"A":2}],[["un"],{"un":1}]]§Compter sans sensibilité à la casse et ignorer les séparateurs vides.§O(n) en temps et O(k) en espace',
    'csharp-sequence-copy-001§4§Matérialiser une séquence une fois§csharp§1§csharp.generics§CopyValues§list<int>:values§list<int>§if (values is null) throw new System.ArgumentNullException(nameof(values)); return new System.Collections.Generic.List<int>(values);§[[[[1,2]],[1,2]],[[[]],[]]]§[[[[-1,0]],[-1,0]],[[[7]],[7]]]§Créer une copie indépendante qui conserve ordre et doublons.§O(n) en temps et O(n) en espace',
    'csharp-json-number-count-001§4§Compter des nombres JSON simples§csharp§3§csharp.json§JsonNumberCount§string:json§int§if (string.IsNullOrWhiteSpace(json)) return 0; using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json); if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return 0; int count = 0; foreach (System.Text.Json.JsonElement item in document.RootElement.EnumerateArray()) if (item.ValueKind == System.Text.Json.JsonValueKind.Number) count++; return count;§[[["[1,2,\"x\"]"],2],[["[]"],0]]§[[["{\"x\":1}"],0],[["[0,-1,2.5]"],3]]§Parser le JSON et compter seulement les éléments numériques dʼun tableau racine.§O(n) en temps et O(n) en espace',
    'algo-linear-search-001§5§Rechercher linéairement§algorithm§1§algorithm.search§IndexOf§int[]:values,int:target§int§if (values is null) throw new System.ArgumentNullException(nameof(values)); for (int i = 0; i < values.Length; i++) if (values[i] == target) return i; return -1;§[[[[4,2,7],2],1],[[[],1],-1]]§[[[[1,1],1],0],[[[3],2],-1]]§Retourner la première position ou moins un après un parcours complet.§O(n) en temps et O(1) en espace',
    'algo-binary-search-001§5§Rechercher dans un tableau trié§algorithm§2§algorithm.search§BinarySearch§int[]:values,int:target§int§int left = 0, right = values.Length - 1; while (left <= right) { int middle = left + (right - left) / 2; if (values[middle] == target) return middle; if (values[middle] < target) left = middle + 1; else right = middle - 1; } return -1;§[[[[1,3,5,7],5],2],[[[],1],-1]]§[[[[1,3,5,7],1],0],[[[1,3,5,7],6],-1]]§Réduire un intervalle fermé et déplacer au-delà du milieu testé.§O(log n) en temps et O(1) en espace',
    'algo-bubble-sort-001§5§Trier par bulles§algorithm§2§algorithm.sorting§BubbleSort§int[]:values§int[]§int[] result = (int[])values.Clone(); for (int end = result.Length - 1; end > 0; end--) for (int i = 0; i < end; i++) if (result[i] > result[i + 1]) (result[i], result[i + 1]) = (result[i + 1], result[i]); return result;§[[[[3,1,2]],[1,2,3]],[[[]],[]]]§[[[[-1,2,-1]],[-1,-1,2]],[[[1]],[1]]]§Copier lʼentrée puis pousser le maximum vers la fin à chaque passage.§O(n²) en temps et O(n) en espace pour la copie',
    'algo-selection-sort-001§5§Trier par sélection§algorithm§2§algorithm.sorting§SelectionSort§int[]:values§int[]§int[] result = (int[])values.Clone(); for (int start = 0; start < result.Length; start++) { int min = start; for (int i = start + 1; i < result.Length; i++) if (result[i] < result[min]) min = i; (result[start], result[min]) = (result[min], result[start]); } return result;§[[[[3,1,2]],[1,2,3]],[[[]],[]]]§[[[[2,2,1]],[1,2,2]],[[[-1]],[ -1]]]§Maintenir un préfixe trié contenant les plus petites valeurs.§O(n²) en temps et O(n) en espace pour la copie',
    'algo-insertion-sort-001§5§Trier par insertion§algorithm§2§algorithm.sorting§InsertionSort§int[]:values§int[]§int[] result = (int[])values.Clone(); for (int i = 1; i < result.Length; i++) { int current = result[i], j = i - 1; while (j >= 0 && result[j] > current) { result[j + 1] = result[j]; j--; } result[j + 1] = current; } return result;§[[[[3,1,2]],[1,2,3]],[[[]],[]]]§[[[[1,2,3]],[1,2,3]],[[[0,-1]],[ -1,0]]]§Insérer chaque valeur dans un préfixe déjà trié.§O(n²) au pire et O(n) en espace pour la copie',
    'algo-minimum-index-001§5§Trouver lʼindice minimal§algorithm§1§algorithm.search§MinimumIndex§int[]:values§int§if (values is null || values.Length == 0) return -1; int min = 0; for (int i = 1; i < values.Length; i++) if (values[i] < values[min]) min = i; return min;§[[[[3,1,2]],1],[[[]],-1]]§[[[[1,1]],0],[[[-2,-3]],1]]§Conserver le premier minimum et définir le tableau vide.§O(n) en temps et O(1) en espace',
    'algo-maximum-value-001§5§Calculer le maximum signé§algorithm§1§algorithm.search§Maximum§int[]:values§int§if (values is null || values.Length == 0) return 0; int max = values[0]; for (int i = 1; i < values.Length; i++) if (values[i] > max) max = values[i]; return max;§[[[[3,1,2]],3],[[[]],0]]§[[[[-5,-2]],-2],[[[7]],7]]§Initialiser depuis le premier élément pour respecter les tableaux négatifs.§O(n) en temps et O(1) en espace',
    'algo-unique-count-001§5§Compter les valeurs distinctes§algorithm§2§algorithm.complexity§UniqueCount§int[]:values§int§if (values is null) throw new System.ArgumentNullException(nameof(values)); return new System.Collections.Generic.HashSet<int>(values).Count;§[[[[1,1,2]],2],[[[]],0]]§[[[[-1,-1,0]],2],[[[3,2,1]],3]]§Utiliser un ensemble et compter zéro, négatifs et doublons.§O(n) attendu en temps et O(n) en espace',
    'algo-merge-sorted-001§5§Fusionner deux tableaux triés§algorithm§3§algorithm.sorting§MergeSorted§int[]:left,int[]:right§int[]§int[] result = new int[left.Length + right.Length]; int i = 0, j = 0, k = 0; while (i < left.Length || j < right.Length) result[k++] = j >= right.Length || (i < left.Length && left[i] <= right[j]) ? left[i++] : right[j++]; return result;§[[[[1,3],[2,4]],[1,2,3,4]],[[[],[1]],[1]]]§[[[[-1,2],[-2,3]],[-2,-1,2,3]],[[[1,1],[1]],[1,1,1]]]§Avancer exactement lʼindex de la valeur consommée et conserver les doublons.§O(n+m) en temps et O(n+m) en espace',
    'algo-rotate-left-001§5§Faire tourner un tableau§algorithm§2§algorithm.arrays§RotateLeft§int[]:values,int:offset§int[]§if (values.Length == 0) return System.Array.Empty<int>(); int shift = ((offset % values.Length) + values.Length) % values.Length; int[] result = new int[values.Length]; for (int i = 0; i < values.Length; i++) result[i] = values[(i + shift) % values.Length]; return result;§[[[[1,2,3],1],[2,3,1]],[[[],4],[]]]§[[[[1,2,3],-1],[3,1,2]],[[[1,2],4],[1,2]]]§Normaliser le décalage, y compris négatif, avant lʼindex modulo.§O(n) en temps et O(n) en espace',
    'algo-prefix-sums-001§5§Construire des sommes préfixes§algorithm§2§algorithm.arrays§PrefixSums§int[]:values§int[]§int[] result = new int[values.Length]; int sum = 0; for (int i = 0; i < values.Length; i++) { sum = checked(sum + values[i]); result[i] = sum; } return result;§[[[[1,2,3]],[1,3,6]],[[[]],[]]]§[[[[-1,2]],[-1,1]],[[[0,0]],[0,0]]]§Chaque case contient la somme du préfixe se terminant à cet index.§O(n) en temps et O(n) en espace',
    'algo-pair-sum-001§5§Détecter une paire de somme cible§algorithm§3§algorithm.search§HasPairSum§int[]:values,int:target§bool§var seen = new System.Collections.Generic.HashSet<int>(); foreach (int value in values) { if (seen.Contains(target - value)) return true; seen.Add(value); } return false;§[[[[2,7,3],9],true],[[[1],2],false]]§[[[[3,3],6],true],[[[-1,4],3],true]]§Chercher le complément avant dʼajouter la valeur courante pour exiger deux positions.§O(n) attendu en temps et O(n) en espace',
    'algo-gcd-001§5§Calculer un PGCD itératif§algorithm§2§algorithm.complexity§GreatestCommonDivisor§int:left,int:right§int§left = System.Math.Abs(left); right = System.Math.Abs(right); while (right != 0) { int remainder = left % right; left = right; right = remainder; } return left;§[[[18,12],6],[[7,5],1]]§[[[-18,12],6],[[0,5],5]]§Réduire strictement le couple par le reste et normaliser les signes.§O(log min(a,b)) en temps et O(1) en espace',
    'algo-halving-steps-001§5§Compter des réductions logarithmiques§algorithm§2§algorithm.complexity§HalvingSteps§int:value§int§if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value)); int steps = 0; while (value > 1) { value /= 2; steps++; } return steps;§[[[8],3],[[1],0]]§[[[7],2],[[-1],"!ArgumentOutOfRangeException"]]§Diviser par deux jusquʼà la borne et compter les réductions réellement faites.§O(log n) en temps et O(1) en espace',
    'structures-stack-reverse-001§6§Inverser avec une pile§algorithm§1§structures.stack§ReverseText§string:text§string§if (text is null) throw new System.ArgumentNullException(nameof(text)); var stack = new System.Collections.Generic.Stack<char>(text); return new string(stack.ToArray());§[[["abc"],"cba"],[[""],""]]§[[["été"],"été"],[["a b"],"b a"]]§La pile restitue les caractères dans lʼordre inverse sans perdre les espaces.§O(n) en temps et O(n) en espace',
    'structures-balanced-parentheses-001§6§Valider des parenthèses§algorithm§2§structures.stack§Balanced§string:text§bool§int depth = 0; foreach (char character in text) { if (character == 40) depth++; else if (character == 41 && --depth < 0) return false; } return depth == 0;§[[["(a(b))"],true],[["(()"],false]]§[[[")("],false],[["text"],true]]§Refuser une fermeture sans ouverture et exiger une profondeur finale nulle.§O(n) en temps et O(1) en espace',
    'structures-window-sums-001§6§Calculer des fenêtres glissantes§algorithm§3§structures.queue§WindowSums§int[]:values,int:size§int[]§if (size <= 0 || size > values.Length) return System.Array.Empty<int>(); int[] result = new int[values.Length - size + 1]; int sum = 0; for (int i = 0; i < values.Length; i++) { sum += values[i]; if (i >= size) sum -= values[i - size]; if (i >= size - 1) result[i - size + 1] = sum; } return result;§[[[[1,2,3,4],2],[3,5,7]],[[[1],1],[1]]]§[[[[1,2],3],[]],[[[1,2],0],[]]]§Ajouter lʼentrant, retirer le sortant puis publier chaque fenêtre complète.§O(n) en temps et O(n) en espace résultat',
    'structures-frequency-map-001§6§Indexer des fréquences entières§algorithm§2§structures.dictionary§Frequencies§int[]:values§dictionary<string,int>§var result = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal); foreach (int value in values) { string key = value.ToString(System.Globalization.CultureInfo.InvariantCulture); result[key] = result.TryGetValue(key, out int count) ? count + 1 : 1; } return result;§[[[[1,1,2]],{"1":2,"2":1}],[[[]],{}]]§[[[[-1,-1]],{"-1":2}],[[[0]],{"0":1}]]§Convertir les clés avec la culture invariante et incrémenter une seule entrée.§O(n) attendu en temps et O(k) en espace',
    'structures-factorial-001§6§Définir un cas de base récursif§algorithm§2§structures.recursion§Factorial§int:value§int§if (value < 0 || value > 12) throw new System.ArgumentOutOfRangeException(nameof(value)); return value <= 1 ? 1 : checked(value * Factorial(value - 1));§[[[0],1],[[5],120]]§[[[1],1],[[-1],"!ArgumentOutOfRangeException"]]§Le cas de base couvre zéro et chaque appel réduit strictement la valeur.§O(n) en temps et O(n) en pile',
    'structures-recursive-sum-001§6§Sommer récursivement un tableau§algorithm§2§structures.recursion§RecursiveSum§int[]:values§int§return Sum(values, 0); static int Sum(int[] items, int index) => index == items.Length ? 0 : checked(items[index] + Sum(items, index + 1));§[[[[1,2,3]],6],[[[]],0]]§[[[[-1,1]],0],[[[7]],7]]§Lʼindex atteint la longueur et progresse de un à chaque appel.§O(n) en temps et O(n) en pile',
    'structures-tree-node-count-001§6§Compter des nœuds présents§algorithm§1§structures.trees§NodeCount§int[]:heapValues§int§int count = 0; foreach (int value in heapValues) if (value != 0) count++; return count;§[[[[1,2,3]],3],[[[]],0]]§[[[[1,0,2]],2],[[[0,0]],0]]§Dans cette représentation pédagogique, zéro signifie explicitement nœud absent.§O(n) en temps et O(1) en espace',
    'structures-tree-height-001§6§Calculer une hauteur depuis les parents§algorithm§3§structures.trees§TreeHeight§int[]:parents§int§int height = 0; for (int node = 0; node < parents.Length; node++) { int depth = 1, current = node, guard = 0; while (parents[current] != -1) { current = parents[current]; depth++; if (++guard > parents.Length) return -1; } if (depth > height) height = depth; } return height;§[[[[-1,0,0]],2],[[[]],0]]§[[[[-1,0,1]],3],[[[1,0]],-1]]§Suivre les parents jusquʼà la racine et refuser un cycle par une garde bornée.§O(n²) au pire et O(1) en espace',
    'structures-next-greater-001§6§Trouver le prochain élément supérieur§algorithm§3§structures.stack§NextGreater§int[]:values§int[]§int[] result = new int[values.Length]; System.Array.Fill(result, -1); var stack = new System.Collections.Generic.Stack<int>(); for (int i = 0; i < values.Length; i++) { while (stack.Count > 0 && values[i] > values[stack.Peek()]) result[stack.Pop()] = values[i]; stack.Push(i); } return result;§[[[[2,1,3]],[3,3,-1]],[[[]],[]]]§[[[[3,2,1]],[-1,-1,-1]],[[[1,2,2]],[2,-1,-1]]]§La pile conserve les indices encore sans réponse et chaque indice en sort une fois.§O(n) en temps et O(n) en espace',
    'structures-queue-rotate-001§6§Faire tourner une file§algorithm§2§structures.queue§RotateQueue§int[]:values,int:count§int[]§if (values.Length == 0) return System.Array.Empty<int>(); var queue = new System.Collections.Generic.Queue<int>(values); int turns = ((count % values.Length) + values.Length) % values.Length; for (int i = 0; i < turns; i++) queue.Enqueue(queue.Dequeue()); return queue.ToArray();§[[[[1,2,3],1],[2,3,1]],[[[],2],[]]]§[[[[1,2,3],-1],[3,1,2]],[[[1,2],4],[1,2]]]§Normaliser le nombre de rotations et préserver lʼordre FIFO.§O(n+k) en temps et O(n) en espace',
    'structures-first-unique-001§6§Trouver le premier caractère unique§algorithm§2§structures.dictionary§FirstUnique§string:text§string§var counts = new System.Collections.Generic.Dictionary<char, int>(); foreach (char c in text) counts[c] = counts.TryGetValue(c, out int count) ? count + 1 : 1; foreach (char c in text) if (counts[c] == 1) return c.ToString(); return "";§[[["swiss"],"w"],[["aabb"],""]]§[[["x"],"x"],[[""],""]]§Compter puis reparcourir dans lʼordre dʼorigine pour choisir le premier.§O(n) attendu en temps et O(k) en espace',
    'structures-parenthesis-depth-001§6§Mesurer une profondeur maximale§algorithm§2§structures.stack§MaximumDepth§string:text§int§int depth = 0, maximum = 0; foreach (char c in text) { if (c == 40) { depth++; maximum = System.Math.Max(maximum, depth); } else if (c == 41 && --depth < 0) return -1; } return depth == 0 ? maximum : -1;§[[["(a(b))"],2],[["text"],0]]§[[["(()"],-1],[[")("],-1]]§Retourner moins un pour toute structure déséquilibrée, sinon la profondeur maximale.§O(n) en temps et O(1) en espace',
    'structures-postfix-001§6§Évaluer une expression postfixée§algorithm§3§structures.stack§EvaluatePostfix§string:expression§int§var stack = new System.Collections.Generic.Stack<int>(); foreach (string token in expression.Split(" ", System.StringSplitOptions.RemoveEmptyEntries)) { if (int.TryParse(token, out int value)) stack.Push(value); else { int right = stack.Pop(), left = stack.Pop(); stack.Push(token == "+" ? left + right : left * right); } } return stack.Pop();§[[["2 3 +"],5],[["2 3 * 4 +"],10]]§[[["5"],5],[["1 2 + 3 *"],9]]§Dépiler droite puis gauche et limiter ce contrat pédagogique à plus et multiplication.§O(n) en temps et O(n) en espace',
    'structures-ancestor-count-001§6§Compter les ancêtres dʼun nœud§algorithm§2§structures.trees§AncestorCount§int[]:parents,int:node§int§if (node < 0 || node >= parents.Length) return -1; int count = 0, current = node; while (parents[current] != -1) { current = parents[current]; if (current < 0 || current >= parents.Length || ++count > parents.Length) return -1; } return count;§[[[[-1,0,1],2],2],[[[-1],0],0]]§[[[[-1,0],3],-1],[[[1,0],0],-1]]§Valider chaque parent et borner le parcours pour détecter un cycle.§O(h) en temps et O(1) en espace',
    'structures-run-length-001§6§Compter les groupes consécutifs§algorithm§2§structures.linear§RunCount§string:text§int§if (string.IsNullOrEmpty(text)) return 0; int runs = 1; for (int i = 1; i < text.Length; i++) if (text[i] != text[i - 1]) runs++; return runs;§[[["aaabb"],2],[[""],0]]§[[["abc"],3],[["aaaa"],1]]§Incrémenter seulement lors dʼun changement par rapport au caractère précédent.§O(n) en temps et O(1) en espace',
    'debug-safe-divide-001§7§Prévenir une division invalide§csharp§1§debugging.data§SafeDivide§int:dividend,int:divisor§int§if (divisor == 0) return 0; return dividend / divisor;§[[[8,2],4],[[8,0],0]]§[[[-9,3],-3],[[1,2],0]]§Traiter le diviseur nul et conserver la division entière annoncée.§O(1) en temps et O(1) en espace',
    'debug-log-errors-001§7§Compter des erreurs dans un journal§csharp§2§debugging.data§ErrorCount§string:log§int§if (string.IsNullOrEmpty(log)) return 0; int count = 0, index = 0; while ((index = log.IndexOf("ERROR", index, System.StringComparison.Ordinal)) >= 0) { count++; index += 5; } return count;§[[["INFO ERROR ERROR"],2],[[""],0]]§[[["error"],0],[["ERRORINFO"],1]]§Chercher le marqueur exact et avancer après chaque occurrence.§O(n) en temps et O(1) en espace',
    'debug-stack-origin-001§7§Extraire la première frame applicative§csharp§2§debugging.stacktrace§FirstFrame§string:trace§string§if (string.IsNullOrWhiteSpace(trace)) return ""; foreach (string line in trace.Split(''\n'')) { string trimmed = line.Trim(); if (trimmed.StartsWith("at Forge.", System.StringComparison.Ordinal)) return trimmed; } return "";§[[["at System.X\nat Forge.Order.Run"],"at Forge.Order.Run"],[[""],""]]§[[["at Forge.A\nat Forge.B"],"at Forge.A"],[["at System.X"],""]]§Ignorer les frames système et conserver la première frame applicative dans lʼordre.§O(n) en temps et O(n) en espace',
    'debug-retry-delay-001§7§Borner un délai de retry§csharp§2§debugging.async§RetryDelay§int:attempt§int§if (attempt < 0) throw new System.ArgumentOutOfRangeException(nameof(attempt)); int power = System.Math.Min(attempt, 5); return 100 * (1 << power);§[[[0],100],[[3],800]]§[[[10],3200],[[-1],"!ArgumentOutOfRangeException"]]§Appliquer un backoff déterministe borné sans attente réelle dans lʼexercice.§O(1) en temps et O(1) en espace',
    'debug-median-001§7§Inspecter une médiane sans muter§csharp§2§debugging.data§Median§int[]:values§decimal§if (values is null || values.Length == 0) return 0m; int[] copy = (int[])values.Clone(); System.Array.Sort(copy); int middle = copy.Length / 2; return copy.Length % 2 == 1 ? copy[middle] : ((decimal)copy[middle - 1] + copy[middle]) / 2m;§[[[[3,1,2]],2],[[[1,3]],2]]§[[[[]],0],[[[-1,2,9,4]],3]]§Trier une copie puis traiter séparément longueurs paires et impaires.§O(n log n) en temps et O(n) en espace',
    'debug-anomaly-count-001§7§Compter des anomalies de seuil§csharp§2§debugging.breakpoints§AnomalyCount§int[]:values,int:threshold§int§int count = 0; foreach (int value in values) if (System.Math.Abs((long)value) > threshold) count++; return count;§[[[[1,9,-10],8],2],[[[],2],0]]§[[[[8,-8],8],0],[[[-9],8],1]]§La borne est strictement dépassée et Abs utilise long pour Int32.MinValue.§O(n) en temps et O(1) en espace',
    'debug-correlation-count-001§7§Suivre un identifiant de corrélation§csharp§2§debugging.data§CorrelationCount§string:log,string:correlationId§int§if (string.IsNullOrEmpty(log) || string.IsNullOrEmpty(correlationId)) return 0; int count = 0, index = 0; while ((index = log.IndexOf(correlationId, index, System.StringComparison.Ordinal)) >= 0) { count++; index += correlationId.Length; } return count;§[[["id=abc id=abc","abc"],2],[["id=abc","xyz"],0]]§[[["aaaa","aa"],2],[["","a"],0]]§Compter les occurrences non chevauchantes et respecter la casse du journal.§O(n) en temps et O(1) en espace',
    'debug-distinct-events-001§7§Dédupliquer des événements§csharp§2§debugging.data§DistinctEventCount§string:events§int§if (string.IsNullOrWhiteSpace(events)) return 0; return new System.Collections.Generic.HashSet<string>(events.Split(",", System.StringSplitOptions.RemoveEmptyEntries), System.StringComparer.Ordinal).Count;§[[["A,B,A"],2],[[""],0]]§[[["A,a"],2],[["A,,B"],2]]§Définir la casse et ignorer les segments vides sans inventer de normalisation.§O(n) attendu en temps et O(n) en espace',
    'debug-chronological-001§7§Vérifier lʼordre de timestamps§csharp§2§debugging.data§IsChronological§int[]:timestamps§bool§for (int i = 1; i < timestamps.Length; i++) if (timestamps[i] < timestamps[i - 1]) return false; return true;§[[[[1,2,2]],true],[[[2,1]],false]]§[[[[]],true],[[[-1,0]],true]]§Autoriser les égalités et détecter la première inversion sans trier lʼentrée.§O(n) en temps et O(1) en espace',
    'debug-state-machine-001§7§Rejouer des transitions bornées§csharp§3§debugging.data§FinalState§int[]:events§int§int state = 0; foreach (int value in events) { if (value == 1 && state == 0) state = 1; else if (value == 2 && state == 1) state = 2; else if (value == 3) state = 0; } return state;§[[[[1,2]],2],[[[2]],0]]§[[[[1,3]],0],[[[1,1,2]],2]]§Appliquer seulement les transitions autorisées et ignorer les événements hors état.§O(n) en temps et O(1) en espace',
    'debug-awaited-total-001§7§Agréger des résultats attendus§csharp§2§csharp.async§AwaitedTotal§int[]:completedResults§int§if (completedResults is null) throw new System.ArgumentNullException(nameof(completedResults)); int total = 0; foreach (int result in completedResults) total = checked(total + result); return total;§[[[[1,2,3]],6],[[[]],0]]§[[[[-1,1]],0],[[[5]],5]]§Lʼexercice isole lʼagrégation après await ; aucun travail lancé ne doit être oublié.§O(n) en temps et O(1) en espace',
    'debug-copy-before-sort-001§7§Préserver les données observées§csharp§2§debugging.breakpoints§SortedCopy§int[]:values§int[]§if (values is null) throw new System.ArgumentNullException(nameof(values)); int[] copy = (int[])values.Clone(); System.Array.Sort(copy); return copy;§[[[[3,1]],[1,3]],[[[]],[]]]§[[[[-1,2,0]],[-1,0,2]],[[[1]],[1]]]§Trier uniquement une copie afin que Watch et lʼappelant conservent lʼétat initial.§O(n log n) en temps et O(n) en espace'
)

$ExerciseIdsByWeek = @{}
$exerciseSpecs = New-Object System.Collections.Generic.List[object]
foreach ($row in $ExerciseRows) {
    $parts = $row -split '§', 14
    if ($parts.Count -ne 14) { throw "Ligne dʼexercice invalide : $($parts[0])" }
    $rawParameters = $parts[7].Replace('dictionary<string,int>', 'dictionary<string~int>')
    $parameters = @($rawParameters -split ',' | ForEach-Object {
        $parameterParts = $_ -split ':', 2
        [pscustomobject]@{ RunnerType = $parameterParts[0].Replace('~', ','); Name = $parameterParts[1] }
    })
    $spec = [pscustomobject]@{
        Id = $parts[0]; Week = [int]$parts[1]; Title = $parts[2]; Kind = $parts[3]
        Difficulty = [int]$parts[4]; Skill = $parts[5]; Method = $parts[6]
        Parameters = $parameters; ReturnType = $parts[8]; Body = $parts[9]
        Visible = @($parts[10] | ConvertFrom-Json); Hidden = @($parts[11] | ConvertFrom-Json)
        Rule = $parts[12]; Complexity = $parts[13]
    }
    $exerciseSpecs.Add($spec)
    if (-not $ExerciseIdsByWeek.ContainsKey($spec.Week)) { $ExerciseIdsByWeek[$spec.Week] = New-Object System.Collections.Generic.List[string] }
    $ExerciseIdsByWeek[$spec.Week].Add($spec.Id)
}

for ($index = 0; $index -lt $exerciseSpecs.Count; $index++) {
    $spec = $exerciseSpecs[$index]
    $variantIndex = if (($index % 2) -eq 0) {
        if ($index + 1 -lt $exerciseSpecs.Count) { $index + 1 } else { 0 }
    } else { $index - 1 }
    $variantId = $exerciseSpecs[$variantIndex].Id
    $directory = Join-Path $CatalogRoot "exercises/$($spec.Id)"
    $lessonPrerequisite = [string]$LessonIdsByWeek[$spec.Week][0]
    $interviewId = if ($spec.Id -like 'reference-total-*') { 'reference-interview-001' } else { "interview-$($spec.Id)" }
    $parameterDeclarations = @($spec.Parameters | ForEach-Object { "$(Convert-TypeName $_.RunnerType) $($_.Name)" }) -join ', '
    $runnerTypes = @($spec.Parameters | ForEach-Object { $_.RunnerType })
    $returnDeclaration = Convert-TypeName $spec.ReturnType
    $solutionSource = "public static class Submission`n{`n    public static $returnDeclaration $($spec.Method)($parameterDeclarations)`n    {`n        $($spec.Body)`n    }`n}"
    $starterSource = "public static class Submission`n{`n    public static $returnDeclaration $($spec.Method)($parameterDeclarations)`n    {`n        throw new System.NotImplementedException(`"À implémenter par lʼapprenant.`");`n    }`n}"

    $firstVisible = $spec.Visible[0]
    $manifest = [ordered]@{
        schemaVersion = 1; id = $spec.Id; version = $(if ($spec.Id -like 'reference-total-*') { 2 } else { 1 })
        title = $spec.Title; kind = $spec.Kind; difficulty = $spec.Difficulty; skills = @($spec.Skill)
        prerequisites = @($lessonPrerequisite); estimatedMinutes = 30; statement = 'statement.md'
        constraints = @('Conserver exactement la signature publique', $spec.Rule, 'Ne pas modifier les entrées reçues sauf mention explicite')
        examples = @([ordered]@{ input = (Convert-JsonCompact $firstVisible[0]); output = (Convert-JsonCompact $firstVisible[1]) })
        reflectionFields = @('reformulation','inputs','expectedOutput','edgeCases','hypothesis','plan')
        starterPath = 'starter/'; visibleTestsPath = 'tests/visible/'; hiddenTestsPath = 'tests/hidden/'
        hints = @(
            [ordered]@{ level = 1; kind = 'socratic'; content = "Quel invariant de « $($spec.Title) » doit rester vrai pour les cas limites ?" },
            [ordered]@{ level = 2; kind = 'location'; content = "Concentrez la décision dans la méthode $($spec.Method), sans état global." },
            [ordered]@{ level = 3; kind = 'strategy'; content = $spec.Rule },
            [ordered]@{ level = 4; kind = 'partial-pseudocode'; content = 'valider les bornes ; parcourir ou décider ; retourner un nouveau résultat déterministe' }
        )
        solution = [ordered]@{ path = 'solution/'; unlock = [ordered]@{ seriousAttempts = 2; minimumDelayMinutes = 10 } }
        explanation = 'explanation.md'; complexity = $spec.Complexity
        commonMistakes = @('Coder uniquement les exemples visibles', 'Oublier une borne ou modifier une entrée', 'Annoncer une complexité sans compter les opérations dominantes')
        variantId = $variantId; reviewCards = @("card-$($spec.Id)-rule", "card-$($spec.Id)-edge")
        interviewQuestionId = $interviewId; license = 'CC-BY-4.0'
    }
    Write-JsonFile (Join-Path $directory 'exercise.json') $manifest
    Write-TextFile (Join-Path $directory 'statement.md') @"
# $($spec.Title)

Implémentez `Submission.$($spec.Method)` avec la signature fournie dans `starter/Submission.cs`.

$($spec.Rule) Le résultat doit être déterministe, hors ligne et ne doit pas modifier les entrées. Avant de coder, écrivez le cas nominal, une borne et un cas qui réfute une réponse codée en dur.

Exemple : entrée `$(Convert-JsonCompact $firstVisible[0])`, sortie `$(Convert-JsonCompact $firstVisible[1])`.
"@
    Write-TextFile (Join-Path $directory 'explanation.md') @"
# Explication

$($spec.Rule) La solution de référence sépare la validation de lʼopération principale et ne dépend dʼaucun état externe. Sa complexité est **$($spec.Complexity)**. Les cas cachés changent valeurs, bornes et tailles afin quʼune constante mémorisée ne puisse pas réussir.
"@
    Write-TextFile (Join-Path $directory 'review-cards.md') @"
# Cartes de révision

## card-$($spec.Id)-rule

**Question :** Quelle règle gouverne $($spec.Title.ToLowerInvariant()) ?  
**Réponse attendue :** $($spec.Rule)

## card-$($spec.Id)-edge

**Question :** Quelle preuve minimale faut-il conserver ?  
**Réponse attendue :** Un test de borne qui échoue avec une implémentation codée sur lʼexemple.
"@
    Write-TextFile (Join-Path $directory 'starter/Submission.cs') $starterSource
    Write-TextFile (Join-Path $directory 'solution/Submission.cs') $solutionSource
    Write-TextFile (Join-Path $directory 'solution/README.md') "# Choix`n`n$($spec.Rule) Complexité : $($spec.Complexity)."
    Write-JsonFile (Join-Path $directory 'tests/runner.json') ([ordered]@{
        schemaVersion = 1; suiteId = "$($spec.Id).v$($manifest.version)"; exerciseId = $spec.Id
        exerciseVersion = $manifest.version; typeName = 'Submission'; methodName = $spec.Method
        parameterTypes = $runnerTypes; returnType = $spec.ReturnType
    })

    foreach ($visibility in @('visible','hidden')) {
        $rawCases = if ($visibility -eq 'visible') { $spec.Visible } else { $spec.Hidden }
        $cases = New-Object System.Collections.Generic.List[object]
        for ($caseIndex = 0; $caseIndex -lt $rawCases.Count; $caseIndex++) {
            $pair = $rawCases[$caseIndex]
            $expected = $pair[1]
            $case = [ordered]@{
                name = "$(if ($visibility -eq 'visible') {'Visible'} else {'Hidden'})_Case$($caseIndex + 1)"
                message = "$($spec.Title) — cas $($caseIndex + 1) $visibility incorrect."
                arguments = @($pair[0]); expected = $null; expectedException = $null; argumentsUnchanged = $false
            }
            if ($expected -is [string] -and $expected.StartsWith('!')) {
                $case.expectedException = $expected.Substring(1)
                $case.Remove('expected')
            }
            else { $case.expected = $expected }
            $cases.Add($case)
        }
        Write-JsonFile (Join-Path $directory "tests/$visibility/cases.json") ([ordered]@{ schemaVersion = 1; cases = $cases.ToArray() })
    }

    if ($spec.Id -notlike 'reference-total-*') {
        $interviewPath = Join-Path $CatalogRoot "interviews/$interviewId.json"
        Write-JsonFile $interviewPath ([ordered]@{
            schemaVersion = 1; id = $interviewId; version = 1; title = $spec.Title
            level = $(if ($spec.Difficulty -ge 3) { 'intermediate' } else { 'junior' }); durationMinutes = 5
            skills = @($spec.Skill); question = "Comment résoudriez-vous « $($spec.Title) » et comment prouveriez-vous les bornes ?"
            observableCriteria = @('Le contrat et une borne sont explicitement reformulés', 'La complexité et un test de réfutation sont justifiés')
            modelAnswer = "$($spec.Rule) La preuve comprend un cas nominal, une borne et un cas différent des exemples ; $($spec.Complexity)."
            commonMistakes = @('Réciter le code sans invariant ni méthode de test')
            variants = @("Changer une borne ou le volume tout en conservant la même règle de $($spec.Title.ToLowerInvariant()).")
            license = 'CC-BY-4.0'
        })
    }
}

$InitialExercises = @{
    '1' = @('csharp-price-conversion-001','csharp-shipping-decision-001','csharp-loop-range-sum-001','csharp-method-multiples-001')
    '2' = @('csharp-array-differences-001','csharp-list-distinct-001','csharp-dictionary-stock-001','csharp-string-frequency-001','csharp-date-business-days-001','csharp-date-expiry-001')
}
foreach ($week in $InitialExercises.Keys) {
    foreach ($id in $InitialExercises[$week]) { $ExerciseIdsByWeek[[int]$week].Add($id) }
}

Write-JsonFile (Join-Path $CatalogRoot 'interviews/reference-interview-001.json') ([ordered]@{
    schemaVersion = 1; id = 'reference-interview-001'; version = 2; title = 'Choisir et calculer un montant'
    level = 'junior'; durationMinutes = 5; skills = @('csharp.types')
    question = 'Pourquoi decimal et un arrondi explicite sont-ils adaptés à un total financier en C# ?'
    observableCriteria = @('La représentation décimale et le moment de lʼarrondi sont distingués', 'Un cas de demi-centime et une méthode de test sont proposés')
    modelAnswer = 'Decimal évite lʼapproximation binaire des fractions décimales usuelles. Je valide les bornes, calcule sans arrondi intermédiaire injustifié, puis applique le mode métier au point annoncé et le vérifie avec un demi-centime.'
    commonMistakes = @('Dire seulement que decimal est plus précis sans parler de règle métier')
    variants = @('Comparer lʼarrondi par ligne à lʼarrondi unique du total de facture')
    license = 'CC-BY-4.0'
})

$modules = New-Object System.Collections.Generic.List[object]
for ($week = 1; $week -le 10; $week++) {
    $lessonIds = $LessonIdsByWeek[$week].ToArray()
    $exerciseIds = @()
    if ($week -le 7) { $exerciseIds = @($ExerciseIdsByWeek[$week].ToArray() | Sort-Object) }
    # Le schéma v1 exige au moins un exercice par module ; les semaines SQL référencent
    # un exercice dʼalgorithmique de préparation, sans le compter dans leurs volumes SQL.
    if ($exerciseIds.Count -eq 0) { $exerciseIds = @('debug-safe-divide-001') }
    $modulePrerequisites = New-Object System.Collections.Generic.List[string]
    if ($week -gt 1) { $modulePrerequisites.Add("week-$($week - 1)") }
    $modules.Add([ordered]@{
        id = "week-$week"; title = "Semaine $week"; weeks = @($week)
        prerequisites = $modulePrerequisites.ToArray()
        lessonIds = $lessonIds; exerciseIds = $exerciseIds
    })
}
Write-JsonFile (Join-Path $CatalogRoot 'curriculum/forge-reference.json') ([ordered]@{
    schemaVersion = 1; id = 'forge-reference'; version = 2
    title = 'Forge.NET — fondamentaux S1 à S10'
    description = 'Parcours local autonome couvrant C#, algorithmique, débogage, SQL et EF Core des semaines 1 à 10.'
    weeks = 10; modules = $modules.ToArray(); license = 'CC-BY-4.0'
})

$ProjectRows = @(
    'project-collections-library-001§2§Bibliothèque de collections§2§8§csharp.collections§Construire une bibliothèque locale de transformations sans mutation avec tests de tableaux vides, doublons et dates.',
    'project-order-import-001§4§Import de commandes CSV vers JSON§3§12§csharp.files§Lire un CSV local borné, distinguer lignes valides et rejetées, puis produire un rapport JSON déterministe.',
    'project-promotions-engine-001§5§Moteur de promotions§3§12§algorithm.complexity§Composer des règles de promotion, expliquer leur priorité et prouver les bornes sans réponse codée en dur.',
    'project-log-analyzer-001§7§Analyseur de journaux§3§10§debugging.data§Analyser des logs assainis, grouper les erreurs et produire un rapport sans modifier les données observées.',
    'project-orders-database-001§10§Base commandes mini-ERP§4§16§efcore.data-access§Modéliser commandes et clients, écrire les requêtes S8–S10 et démontrer reset, tracking, N+1 et concurrence.'
)
$projectIds = @($ProjectRows | ForEach-Object { ($_ -split '§', 7)[0] })
for ($index = 0; $index -lt $ProjectRows.Count; $index++) {
    $parts = $ProjectRows[$index] -split '§', 7
    $id = $parts[0]; $week = [int]$parts[1]; $title = $parts[2]; $difficulty = [int]$parts[3]
    $hours = [int]$parts[4]; $skill = $parts[5]; $brief = $parts[6]
    $variantId = $projectIds[($index + 1) % $projectIds.Count]
    Write-JsonFile (Join-Path $CatalogRoot "projects/$id.json") ([ordered]@{
        schemaVersion = 1; id = $id; version = 1; title = $title; difficulty = $difficulty; weeks = @($week)
        skills = @($skill); prerequisites = @([string]$LessonIdsByWeek[$week][0]); estimatedHours = $hours
        briefPath = "$id.md"
        milestones = @(
            [ordered]@{ id = 'contract'; title = 'Contrat et cas'; evidence = 'Un document fixe entrées, sorties, erreurs et cas limites.'; acceptanceCriteria = @('Trois cas utiles sont prédits avant implémentation') },
            [ordered]@{ id = 'implementation'; title = 'Implémentation'; evidence = 'Le dépôt compile et les tests démontrent le comportement.'; acceptanceCriteria = @('Le scénario nominal et les bornes sont verts sans réseau') },
            [ordered]@{ id = 'defense'; title = 'Défense'; evidence = 'Une présentation explique décisions, complexité et défaut corrigé.'; acceptanceCriteria = @('Une limite et une amélioration sont reconnues explicitement') }
        )
        rubric = @(
            [ordered]@{ criterion = 'Exactitude observable'; weight = 0.5; observableEvidence = 'Les sorties et erreurs correspondent au contrat pour les cas fournis et nouveaux.' },
            [ordered]@{ criterion = 'Qualité des preuves'; weight = 0.3; observableEvidence = 'Les tests couvrent nominal, borne et non-régression nommée.' },
            [ordered]@{ criterion = 'Explication autonome'; weight = 0.2; observableEvidence = 'La défense relie choix, complexité et compromis sans dépendance externe.' }
        )
        solutionPolicy = 'no-complete-solution-before-submission'
        commonMistakes = @('Commencer par le code sans contrat', 'Masquer un rejet ou un test rouge')
        variantIds = @($variantId); license = 'CC-BY-4.0'
    })
    Write-TextFile (Join-Path $CatalogRoot "projects/$id.md") @"
# $title

$brief

Le livrable comprend le contrat, le code écrit par lʼapprenant, des tests utiles, un journal dʼun défaut reproduit puis corrigé et une défense de dix minutes. Toute assistance est déclarée. Aucune solution complète nʼest fournie avant soumission.
"@
}

$DebugRows = @(
    'debug-input-whitespace-001§1§Saisie non normalisée§debugging.breakpoints§Normalize§string:value§string§return value;§return value.Trim();§[[[" Ada "],"Ada"],[["ok"],"ok"]]§[[["  "],""],[[" x y "],"x y"]]§La valeur conserve des espaces de bord.',
    'debug-array-empty-001§2§Moyenne dʼun tableau vide§debugging.data§Average§int[]:values§decimal§int sum = 0; foreach (int value in values) sum += value; return (decimal)sum / values.Length;§if (values.Length == 0) return 0m; int sum = 0; foreach (int value in values) sum += value; return (decimal)sum / values.Length;§[[[[1,3]],2],[[[]],0]]§[[[[2]],2],[[[-1,1]],0]]§La division utilise une longueur nulle.',
    'debug-string-boundary-001§2§Dernier caractère hors limite§debugging.breakpoints§LastCharacter§string:text§string§return text[text.Length].ToString();§return string.IsNullOrEmpty(text) ? "" : text[text.Length - 1].ToString();§[[["abc"],"c"],[[""],""]]§[[["x"],"x"],[["a b"],"b"]]§Lʼindex égal à Length sort de la chaîne.',
    'debug-parse-exception-001§3§Conversion qui interrompt le lot§debugging.stacktrace§ParseOrZero§string:text§int§return int.Parse(text, System.Globalization.CultureInfo.InvariantCulture);§return int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int value) ? value : 0;§[[["42"],42],[["invalid"],0]]§[[["-3"],-3],[[""],0]]§Parse lève une exception pour une absence attendue.',
    'debug-nullable-display-001§3§Nom absent déréférencé§debugging.null-reference§DisplayName§string:name§string§return name.Trim().ToUpperInvariant();§return string.IsNullOrWhiteSpace(name) ? "(inconnu)" : name.Trim().ToUpperInvariant();§[[[" ada "],"ADA"],[[""],"(inconnu)"]]§[[["   "],"(inconnu)"],[["Zoë"],"ZOË"]]§La normalisation précède le traitement de lʼabsence.',
    'debug-json-count-001§4§Comptage JSON décalé§debugging.data§NumberCount§string:json§int§using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json); return document.RootElement.GetArrayLength() - 1;§using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json); return document.RootElement.GetArrayLength();§[[["[1,2]"],2],[["[]"],0]]§[[["[1]"],1],[["[1,2,3]"],3]]§Une soustraction artificielle retire toujours le dernier élément.',
    'debug-deferred-count-001§4§Filtre LINQ à mauvaise borne§debugging.data§PositiveCount§int[]:values§int§return System.Linq.Enumerable.Count(values, value => value >= 0);§return System.Linq.Enumerable.Count(values, value => value > 0);§[[[[1,0,-1]],1],[[[]],0]]§[[[[0,0]],0],[[[2,3]],2]]§Le prédicat inclut zéro alors que le contrat exige strictement positif.',
    'debug-binary-boundary-001§5§Recherche binaire sans dernière case§debugging.breakpoints§Contains§int[]:values,int:target§bool§int left = 0, right = values.Length - 1; while (left < right) { int middle = (left + right) / 2; if (values[middle] == target) return true; if (values[middle] < target) left = middle + 1; else right = middle - 1; } return false;§int left = 0, right = values.Length - 1; while (left <= right) { int middle = left + (right - left) / 2; if (values[middle] == target) return true; if (values[middle] < target) left = middle + 1; else right = middle - 1; } return false;§[[[[1,3,5],5],true],[[[1,3,5],2],false]]§[[[[1],1],true],[[[],1],false]]§La condition de boucle exclut le dernier candidat.',
    'debug-sort-swap-001§5§Tri dans le mauvais sens§debugging.data§SortCopy§int[]:values§int[]§int[] result = (int[])values.Clone(); System.Array.Sort(result); System.Array.Reverse(result); return result;§int[] result = (int[])values.Clone(); System.Array.Sort(result); return result;§[[[[3,1,2]],[1,2,3]],[[[]],[]]]§[[[[2,1]],[1,2]],[[[-1,2,0]],[-1,0,2]]]§La correction attend un ordre ascendant mais le code inverse le résultat trié.',
    'debug-duplicate-count-001§5§Doublons comptés deux fois§debugging.data§UniqueCount§int[]:values§int§int count = 0; for (int i = 0; i < values.Length; i++) for (int j = i; j < values.Length; j++) if (values[i] == values[j]) count++; return count;§return new System.Collections.Generic.HashSet<int>(values).Count;§[[[[1,1,2]],2],[[[]],0]]§[[[[3,3,3]],1],[[[1,2,3]],3]]§La double boucle compte des paires au lieu des valeurs distinctes.',
    'debug-stack-order-001§6§Pile lue dans le mauvais ordre§debugging.data§Reverse§string:text§string§var stack = new System.Collections.Generic.Stack<char>(text); char[] result = stack.ToArray(); System.Array.Reverse(result); return new string(result);§var stack = new System.Collections.Generic.Stack<char>(text); return new string(stack.ToArray());§[[["abc"],"cba"],[[""],""]]§[[["ab"],"ba"],[["x"],"x"]]§Une seconde inversion annule lʼordre LIFO observé.',
    'debug-queue-window-001§6§Fenêtre qui oublie le sortant§debugging.data§WindowTotal§int[]:values,int:size§int§int sum = 0; for (int i = 0; i < values.Length; i++) sum += values[i]; return sum;§if (size <= 0) return 0; int sum = 0; for (int i = System.Math.Max(0, values.Length - size); i < values.Length; i++) sum += values[i]; return sum;§[[[[1,2,3],2],5],[[[1],3],1]]§[[[[1,2,3],1],3],[[[],2],0]]§Le calcul additionne toute lʼhistorique au lieu de la fenêtre finale.',
    'debug-recursion-base-001§6§Récursion sans base zéro§debugging.stacktrace§Factorial§int:value§int§return value == 1 ? 1 : value * Factorial(value - 1);§if (value < 0 || value > 12) throw new System.ArgumentOutOfRangeException(nameof(value)); return value <= 1 ? 1 : value * Factorial(value - 1);§[[[0],1],[[5],120]]§[[[1],1],[[3],6]]§Le cas de base oublie zéro et la récursion ne progresse plus vers une sortie.',
    'debug-async-last-result-001§7§Dernière tâche ignorée§debugging.async§Total§int[]:completed§int§int sum = 0; for (int i = 0; i < completed.Length - 1; i++) sum += completed[i]; return sum;§int sum = 0; foreach (int value in completed) sum += value; return sum;§[[[[1,2,3]],6],[[[]],0]]§[[[[5]],5],[[[-1,1]],0]]§La borne de boucle omet systématiquement le dernier résultat attendu.',
    'debug-cancellation-state-001§7§État annulé présenté comme réussi§debugging.async§Outcome§int:code§string§return code >= 0 ? "succeeded" : "failed";§return code switch { 0 => "succeeded", 1 => "cancelled", _ => "failed" };§[[[0],"succeeded"],[[1],"cancelled"]]§[[[-1],"failed"],[[2],"failed"]]§Le code dʼannulation est regroupé à tort avec le succès.',
    'debug-stacktrace-origin-001§7§Origine remplacée par la dernière frame§debugging.stacktrace§Origin§string:trace§string§string[] lines = trace.Split(''\n''); return lines.Length == 0 ? "" : lines[lines.Length - 1].Trim();§foreach (string line in trace.Split(''\n'')) { string value = line.Trim(); if (value.StartsWith("at Forge.", System.StringComparison.Ordinal)) return value; } return "";§[[["at System.X\nat Forge.A\nat Forge.B"],"at Forge.A"],[[""],""]]§[[["at Forge.One"],"at Forge.One"],[["at System.X"],""]]§La dernière frame nʼest pas la première origine applicative utile.',
    'debug-data-mutation-001§7§Observation qui trie les données§debugging.data§SortedCopy§int[]:values§int[]§System.Array.Sort(values); return values;§int[] copy = (int[])values.Clone(); System.Array.Sort(copy); return copy;§[[[[3,1]],[1,3]],[[[]],[]]]§[[[[-1,2,0]],[-1,0,2]],[[[1]],[1]]]§Le tri en place modifie la preuve que lʼon cherche à observer.'
)

foreach ($row in $DebugRows) {
    $parts = $row -split '§', 13
    $id = $parts[0]; $week = [int]$parts[1]; $title = $parts[2]; $skill = $parts[3]; $method = $parts[4]
    $parameters = @($parts[5] -split ',' | ForEach-Object { $p = $_ -split ':',2; [pscustomobject]@{ RunnerType=$p[0]; Name=$p[1] } })
    $returnType = $parts[6]; $brokenBody = $parts[7]; $correctionBody = $parts[8]
    $visible = @($parts[9] | ConvertFrom-Json); $hidden = @($parts[10] | ConvertFrom-Json); $symptom = $parts[11]
    # La treizième colonne est optionnelle pour garder le DSL lisible.
    if ([string]::IsNullOrWhiteSpace($symptom)) { throw "Symptôme DebugLab absent : $id" }
    $directory = Join-Path $CatalogRoot "debugging/$id"
    $parameterDeclarations = @($parameters | ForEach-Object { "$(Convert-TypeName $_.RunnerType) $($_.Name)" }) -join ', '
    $returnDeclaration = Convert-TypeName $returnType
    Write-JsonFile (Join-Path $directory 'scenario.json') ([ordered]@{
        schemaVersion=1; id=$id; version=1; title=$title; difficulty=$(if($week -ge 5){3}else{2}); skills=@($skill)
        prerequisites=@([string]$LessonIdsByWeek[$week][0]); estimatedMinutes=40; brokenRepositoryPath='broken/'
        ticketPath='ticket.md'; expectedBehavior="Le résultat doit respecter tous les cas du contrat sans mutation cachée. $symptom"
        logsPath='logs.txt'; checklist=@('Reproduire avec le cas de borne','Placer un breakpoint avant la divergence','Comparer entrée et valeur intermédiaire','Ajouter le cas de non-régression')
        observationQuestions=@('Quelle est la première valeur qui diverge du résultat prédit ?','Quelle condition ou mutation explique causalement ce symptôme ?')
        correctionPath='correction/'; regressionTestPath='regression-test.md'
        journalFields=@('symptom','context','hypotheses','evidence','cause','fix','test','prevention'); license='CC-BY-4.0'
    })
    Write-TextFile (Join-Path $directory 'ticket.md') "# Ticket`n`n$title. $symptom Reproduire avant toute correction et préserver les données observées."
    Write-TextFile (Join-Path $directory 'logs.txt') "Event=UnexpectedResult Level=Warning Scenario=$id Week=$week Actual=MISMATCH Expected=CONTRACT CorrelationId=$id"
    Write-TextFile (Join-Path $directory 'regression-test.md') "# Non-régression`n`nExécuter les quatre cas du runner, dont la borne cachée. Le test doit échouer sur `broken/Submission.cs`, réussir sur `correction/Submission.cs` et vérifier lʼabsence de mutation pour les tableaux."
    Write-TextFile (Join-Path $directory 'broken/Submission.cs') "public static class Submission`n{`n    public static $returnDeclaration $method($parameterDeclarations)`n    {`n        $brokenBody`n    }`n}"
    Write-TextFile (Join-Path $directory 'correction/Submission.cs') "public static class Submission`n{`n    public static $returnDeclaration $method($parameterDeclarations)`n    {`n        $correctionBody`n    }`n}"
    Write-JsonFile (Join-Path $directory 'tests/rubric.json') ([ordered]@{schemaVersion=1;scenarioId=$id;criteria=@(
        [ordered]@{id='cause';label='La cause relie la divergence à la règle fautive.';journalField='cause';requiredTerms=@('borne','condition','mutation');minimumMatches=1},
        [ordered]@{id='evidence';label='La preuve cite entrée, attendu et réel.';journalField='evidence';requiredTerms=@('entrée','attendu','réel');minimumMatches=2},
        [ordered]@{id='test';label='Le test de non-régression cible la borne.';journalField='test';requiredTerms=@('borne','échoue','réussit');minimumMatches=2}
    )})
    Write-JsonFile (Join-Path $directory 'tests/runner.json') ([ordered]@{schemaVersion=1;suiteId=$id;exerciseId=$id;exerciseVersion=1;typeName='Submission';methodName=$method;parameterTypes=@($parameters|ForEach-Object{$_.RunnerType});returnType=$returnType})
    foreach ($visibility in @('visible','hidden')) {
        $rawCases = if($visibility -eq 'visible'){$visible}else{$hidden}; $cases=New-Object System.Collections.Generic.List[object]
        for($caseIndex=0;$caseIndex -lt $rawCases.Count;$caseIndex++){
            $pair=$rawCases[$caseIndex];$cases.Add([ordered]@{name="$(if($visibility -eq 'visible'){'Visible'}else{'Hidden'})_Case$($caseIndex+1)";message="$title — cas $($caseIndex+1).";arguments=@($pair[0]);expected=$pair[1];expectedException=$null;argumentsUnchanged=($parameters.RunnerType -contains 'int[]')})
        }
        Write-JsonFile (Join-Path $directory "tests/$visibility/cases.json") ([ordered]@{schemaVersion=1;cases=$cases.ToArray()})
    }
}

$SqlRows = @(
    'sql-active-customers-001§8§Projeter les clients actifs§1§sql.select,sql.filter§SELECT CustomerId, Name FROM dbo.Customers WHERE IsActive = 1 ORDER BY CustomerId;§CustomerId,Name§[["1","Ada"],["2","Lin"],["4","Noa"]]§select-filter§true',
    'sql-order-projection-001§8§Sélectionner les colonnes de commande§1§sql.select§SELECT OrderId, Total FROM dbo.Orders ORDER BY OrderId;§OrderId,Total§[["1","25"],["2","40"],["3","70"],["4","15"],["5","100"]]§projection§true',
    'sql-open-orders-001§8§Filtrer les commandes ouvertes§1§sql.filter§SELECT OrderId, CustomerId FROM dbo.Orders WHERE Status = N''Open'' ORDER BY OrderId;§OrderId,CustomerId§[["2","1"],["5","2"]]§filter§true',
    'sql-orders-date-range-001§8§Filtrer une plage de dates§2§sql.filter,sql.dates§SELECT OrderId, OrderDate FROM dbo.Orders WHERE OrderDate >= ''2026-02-01'' AND OrderDate < ''2026-03-01'' ORDER BY OrderId;§OrderId,OrderDate§[["2","2026-02-10T00:00:00.0000000"],["3","2026-02-15T00:00:00.0000000"]]§date-range§true',
    'sql-paid-customer-join-001§8§Joindre paiements et clients§2§sql.join§SELECT o.OrderId, c.Name FROM dbo.Orders o JOIN dbo.Customers c ON c.CustomerId = o.CustomerId WHERE o.Status = N''Paid'' ORDER BY o.OrderId;§OrderId,Name§[["1","Ada"],["3","Lin"]]§inner-join§true',
    'sql-customer-order-count-001§8§Conserver les clients sans commande§2§sql.left-join§SELECT c.CustomerId, COUNT(o.OrderId) AS OrderCount FROM dbo.Customers c LEFT JOIN dbo.Orders o ON o.CustomerId = c.CustomerId GROUP BY c.CustomerId ORDER BY c.CustomerId;§CustomerId,OrderCount§[["1","2"],["2","2"],["3","1"],["4","0"]]§left-join§true',
    'sql-order-product-join-001§8§Joindre lignes et produits§2§sql.join,sql.cardinality§SELECT p.Name, l.Quantity FROM dbo.OrderLines l JOIN dbo.Products p ON p.ProductId = l.ProductId WHERE l.OrderId = 1 ORDER BY p.ProductId;§Name,Quantity§[["Pen","2"],["Book","2"]]§multi-join§true',
    'sql-customers-without-orders-001§8§Trouver les clients sans commande§2§sql.left-join,sql.null§SELECT c.CustomerId, c.Name FROM dbo.Customers c LEFT JOIN dbo.Orders o ON o.CustomerId = c.CustomerId WHERE o.OrderId IS NULL ORDER BY c.CustomerId;§CustomerId,Name§[["4","Noa"]]§anti-join§true',
    'sql-order-total-band-001§8§Classer les totaux par CASE§2§sql.case§SELECT OrderId, CASE WHEN Total < 30 THEN N''small'' WHEN Total < 80 THEN N''medium'' ELSE N''large'' END AS Band FROM dbo.Orders ORDER BY OrderId;§OrderId,Band§[["1","small"],["2","medium"],["3","medium"],["4","small"],["5","large"]]§case§true',
    'sql-distinct-cities-001§8§Lister les villes distinctes§1§sql.distinct§SELECT DISTINCT City FROM dbo.Customers ORDER BY City;§City§[["Lille"],["Lyon"],["Paris"]]§distinct§true',
    'sql-top-orders-001§8§Borner les plus grosses commandes§2§sql.ordering§SELECT TOP (2) OrderId, Total FROM dbo.Orders ORDER BY Total DESC, OrderId;§OrderId,Total§[["5","100"],["3","70"]]§top-order§true',
    'sql-union-labels-001§8§Combiner deux vocabulaires§2§sql.set§SELECT Label FROM (SELECT City AS Label FROM dbo.Customers UNION SELECT Category FROM dbo.Products) s ORDER BY Label;§Label§[["Home"],["Lille"],["Lyon"],["Office"],["Paris"]]§union§true',
    'sql-product-category-pairs-001§8§Auto-joindre des produits par catégorie§3§sql.join,sql.cardinality§SELECT a.ProductId AS LeftId, b.ProductId AS RightId FROM dbo.Products a JOIN dbo.Products b ON b.Category = a.Category AND b.ProductId > a.ProductId ORDER BY a.ProductId, b.ProductId;§LeftId,RightId§[["1","2"],["3","4"]]§self-join§true',
    'sql-status-count-001§9§Compter par statut§1§sql.aggregate§SELECT Status, COUNT_BIG(*) AS OrderCount FROM dbo.Orders GROUP BY Status ORDER BY Status;§Status,OrderCount§[["Cancelled","1"],["Open","2"],["Paid","2"]]§group-count§true',
    'sql-customer-having-001§9§Filtrer les groupes par HAVING§2§sql.aggregate§SELECT CustomerId, COUNT_BIG(*) AS OrderCount FROM dbo.Orders GROUP BY CustomerId HAVING COUNT_BIG(*) >= 2 ORDER BY CustomerId;§CustomerId,OrderCount§[["1","2"],["2","2"]]§having§true',
    'sql-customer-revenue-001§9§Agréger le revenu client§2§sql.aggregate§SELECT CustomerId, SUM(Total) AS Revenue FROM dbo.Orders GROUP BY CustomerId ORDER BY CustomerId;§CustomerId,Revenue§[["1","65"],["2","170"],["3","15"]]§sum§true',
    'sql-orders-above-average-001§9§Comparer à une sous-requête scalaire§2§sql.subquery§SELECT OrderId, Total FROM dbo.Orders WHERE Total > (SELECT AVG(Total) FROM dbo.Orders) ORDER BY OrderId;§OrderId,Total§[["3","70"],["5","100"]]§scalar-subquery§true',
    'sql-active-open-exists-001§9§Filtrer avec EXISTS§2§sql.subquery§SELECT c.CustomerId, c.Name FROM dbo.Customers c WHERE c.IsActive = 1 AND EXISTS (SELECT 1 FROM dbo.Orders o WHERE o.CustomerId = c.CustomerId AND o.Status = N''Open'') ORDER BY c.CustomerId;§CustomerId,Name§[["1","Ada"],["2","Lin"]]§exists§true',
    'sql-monthly-cte-001§9§Nommer une agrégation mensuelle§2§sql.cte§WITH Monthly AS (SELECT CONVERT(char(7), OrderDate, 126) AS MonthKey, SUM(Total) AS Total FROM dbo.Orders GROUP BY CONVERT(char(7), OrderDate, 126)) SELECT MonthKey, Total FROM Monthly ORDER BY MonthKey;§MonthKey,Total§[["2026-01","25"],["2026-02","110"],["2026-03","115"]]§monthly-cte-aggregation§true',
    'sql-running-total-001§9§Calculer un cumul fenêtré§3§sql.window§SELECT OrderId, SUM(Total) OVER (ORDER BY OrderId ROWS UNBOUNDED PRECEDING) AS RunningTotal FROM dbo.Orders ORDER BY OrderId;§OrderId,RunningTotal§[["1","25"],["2","65"],["3","135"],["4","150"],["5","250"]]§window§true',
    'sql-open-count-by-customer-001§9§Agréger conditionnellement§2§sql.aggregate§SELECT c.CustomerId, SUM(CASE WHEN o.Status = N''Open'' THEN 1 ELSE 0 END) AS OpenCount FROM dbo.Customers c LEFT JOIN dbo.Orders o ON o.CustomerId = c.CustomerId GROUP BY c.CustomerId ORDER BY c.CustomerId;§CustomerId,OpenCount§[["1","1"],["2","1"],["3","0"],["4","0"]]§conditional-aggregate§true',
    'sql-customer-maximum-001§9§Corréler le maximum de chaque client§3§sql.subquery§SELECT o.CustomerId, o.Total FROM dbo.Orders o WHERE o.Total = (SELECT MAX(i.Total) FROM dbo.Orders i WHERE i.CustomerId = o.CustomerId) ORDER BY o.CustomerId;§CustomerId,Total§[["1","40"],["2","100"],["3","15"]]§correlated-subquery§true',
    'sql-composite-access-001§10§Cibler client puis date§2§sql.indexes§SELECT OrderId, OrderDate FROM dbo.Orders WHERE CustomerId = 1 AND OrderDate > ''2026-01-15'' ORDER BY OrderDate, OrderId;§OrderId,OrderDate§[["2","2026-02-10T00:00:00.0000000"]]§composite-index§true',
    'sql-covering-read-001§10§Projeter une lecture couverte§2§sql.indexes§SELECT OrderId, Total FROM dbo.Orders WHERE Status = N''Paid'' ORDER BY OrderId;§OrderId,Total§[["1","25"],["3","70"]]§covering-index§true',
    'sql-keyset-total-001§10§Paginer par clé totale§3§sql.pagination§SELECT TOP (2) OrderId, Total FROM dbo.Orders WHERE Total > 70 OR (Total = 70 AND OrderId > 3) ORDER BY Total, OrderId;§OrderId,Total§[["5","100"]]§keyset§true',
    'sql-row-number-page-001§10§Paginer avec ROW_NUMBER§3§sql.pagination§WITH Ranked AS (SELECT OrderId, ROW_NUMBER() OVER (ORDER BY OrderDate, OrderId) AS Position FROM dbo.Orders) SELECT OrderId, Position FROM Ranked WHERE Position BETWEEN 2 AND 4 ORDER BY Position;§OrderId,Position§[["2","2"],["3","3"],["4","4"]]§row-number§true',
    'sql-date-seek-001§10§Continuer après une clé composée§3§sql.pagination§SELECT OrderId, OrderDate FROM dbo.Orders WHERE OrderDate > ''2026-02-10'' OR (OrderDate = ''2026-02-10'' AND OrderId > 2) ORDER BY OrderDate, OrderId;§OrderId,OrderDate§[["3","2026-02-15T00:00:00.0000000"],["4","2026-03-01T00:00:00.0000000"],["5","2026-03-05T00:00:00.0000000"]]§date-seek§true',
    'sql-concurrency-candidates-001§10§Lire un jeton de concurrence pédagogique§2§sql.concurrency§SELECT OrderId, DataVersion FROM dbo.Orders WHERE Status = N''Open'' ORDER BY OrderId;§OrderId,DataVersion§[["2","1"],["5","3"]]§concurrency-token§true'
)

$SqlDataset = @"
CREATE TABLE dbo.Customers (CustomerId int PRIMARY KEY, Name nvarchar(80) NOT NULL UNIQUE, City nvarchar(80) NOT NULL, IsActive bit NOT NULL);
CREATE TABLE dbo.Products (ProductId int PRIMARY KEY, Name nvarchar(80) NOT NULL, Category nvarchar(40) NOT NULL, Price decimal(10,2) NOT NULL CHECK (Price >= 0), Stock int NOT NULL CHECK (Stock >= 0));
CREATE TABLE dbo.Orders (OrderId int PRIMARY KEY, CustomerId int NOT NULL REFERENCES dbo.Customers(CustomerId), OrderDate date NOT NULL, Status nvarchar(20) NOT NULL, Total decimal(10,2) NOT NULL CHECK (Total >= 0), DataVersion int NOT NULL);
CREATE TABLE dbo.OrderLines (OrderLineId int PRIMARY KEY, OrderId int NOT NULL REFERENCES dbo.Orders(OrderId), ProductId int NOT NULL REFERENCES dbo.Products(ProductId), Quantity int NOT NULL CHECK (Quantity > 0), UnitPrice decimal(10,2) NOT NULL CHECK (UnitPrice >= 0));
INSERT dbo.Customers VALUES (1,N'Ada',N'Paris',1),(2,N'Lin',N'Lyon',1),(3,N'Sam',N'Paris',0),(4,N'Noa',N'Lille',1);
INSERT dbo.Products VALUES (1,N'Pen',N'Office',2.50,100),(2,N'Book',N'Office',10,20),(3,N'Mug',N'Home',8,0),(4,N'Lamp',N'Home',30,5);
INSERT dbo.Orders VALUES (1,1,'2026-01-10',N'Paid',25,1),(2,1,'2026-02-10',N'Open',40,1),(3,2,'2026-02-15',N'Paid',70,2),(4,3,'2026-03-01',N'Cancelled',15,1),(5,2,'2026-03-05',N'Open',100,3);
INSERT dbo.OrderLines VALUES (1,1,1,2,2.50),(2,1,2,2,10),(3,2,4,1,30),(4,3,2,7,10),(5,4,3,1,8),(6,5,4,3,30);
"@
$SqlReset = @"
DROP TABLE IF EXISTS dbo.OrderLines;
DROP TABLE IF EXISTS dbo.Orders;
DROP TABLE IF EXISTS dbo.Products;
DROP TABLE IF EXISTS dbo.Customers;
$SqlDataset
"@
foreach ($row in $SqlRows) {
    $parts = $row -split '§', 11
    $id=$parts[0];$week=[int]$parts[1];$title=$parts[2];$difficulty=[int]$parts[3];$skills=@($parts[4]-split ',')
    $query=$parts[5];$columns=@($parts[6]-split ',');$expected=@($parts[7]|ConvertFrom-Json);$family=$parts[8];$ordered=[bool]::Parse($parts[9])
    $directory=Join-Path $ContentRoot "sql/$id"
    Write-JsonFile (Join-Path $directory 'scenario.json') ([ordered]@{
        schemaVersion=1;id=$id;version=1;title=$title;difficulty=$difficulty;skills=$skills;prerequisites=@();estimatedMinutes=35
        image='mcr.microsoft.com/mssql/server@sha256:ba4c8329f48fb8f02e1416be6a930ebfd71268caee78aa985f3af4315e457c89'
        datasetPath='dataset.sql';visibleSchemaPath='schema.sql';permissions=@('select');resetScriptPath='reset.sql';statementPath='statement.md'
        expectedResult=[ordered]@{ordered=$ordered;columns=$columns;numericTolerance=0.01};timeoutSeconds=5;maxRows=20
        effectAssertions=@('Les quatre clients, cinq commandes et six lignes restent inchangés après rollback')
        solutionPath='solution.md';license='CC-BY-4.0'
    })
    Write-TextFile (Join-Path $directory 'schema.sql') 'Customers(CustomerId PK, Name UNIQUE, City, IsActive); Products(ProductId PK, Name, Category, Price CHECK, Stock CHECK); Orders(OrderId PK, CustomerId FK, OrderDate, Status, Total CHECK, DataVersion); OrderLines(OrderLineId PK, OrderId FK, ProductId FK, Quantity CHECK, UnitPrice CHECK).'
    Write-TextFile (Join-Path $directory 'dataset.sql') $SqlDataset
    Write-TextFile (Join-Path $directory 'reset.sql') $SqlReset
    Write-TextFile (Join-Path $directory 'statement.md') "# $title`n`nÉcrivez une requête bornée qui retourne exactement les colonnes `$(($columns -join ', '))`. Lʼordre annoncé est significatif. Nʼutilisez ni objet serveur, ni référence inter-base, ni donnée externe."
    Write-TextFile (Join-Path $directory 'solution.md') "# Solution expliquée`n`n``````sql`n$query`n```````n`nLa requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte."
    $queryJson = $query | ConvertTo-Json -Compress
    $contractJson = @"
{
  "week": $week,
  "family": "$family",
  "mode": "sql",
  "datasetRevision": "s1-s10-orders-v1",
  "expectedRows": $($parts[7]),
  "equivalentQuery": $queryJson,
  "negativeQuery": "SELECT CustomerId FROM dbo.Customers WHERE 1 = 0;",
  "dirtySql": "UPDATE dbo.Orders SET Total = 0 WHERE OrderId = 5;",
  "resetProbeSql": "SELECT Total FROM dbo.Orders WHERE OrderId = 5;",
  "resetExpected": "100.00",
  "planIndex": null,
  "stabilityMutationSql": null
}
"@
    Write-TextFile (Join-Path $directory 'tests/contract.json') $contractJson
}

$ExamDefinitions = @(
    [pscustomobject]@{
        Directory='reference-csharp-foundations-v1'; Id='reference-csharp-foundations-v1'; Title='Examen 1 — fondamentaux C# S1–S2'; Duration=90; Draw=8
        Candidates=@('reference-total-001','reference-total-002','csharp-price-conversion-001','csharp-shipping-decision-001','csharp-loop-range-sum-001','csharp-method-multiples-001','csharp-input-normalize-001','csharp-temperature-band-001','csharp-even-counter-001','csharp-clamp-value-001','csharp-array-differences-001','csharp-list-distinct-001','csharp-array-positive-sum-001','csharp-string-palindrome-001','csharp-date-span-001','csharp-dictionary-default-001')
    },
    [pscustomobject]@{
        Directory='csharp-modern-v1'; Id='csharp-modern-v1'; Title='Examen 2 — C# moderne et mini-projet S3–S4'; Duration=90; Draw=8
        Candidates=@('csharp-order-status-001','csharp-customer-name-001','csharp-stock-reservation-001','csharp-vip-discount-001','csharp-nullable-fallback-001','csharp-positive-quantity-001','csharp-line-total-001','csharp-payment-fee-001','csharp-age-group-001','csharp-copy-sanitized-001','csharp-generic-maximum-001','csharp-delegate-double-001','csharp-lambda-threshold-001','csharp-linq-even-sum-001','csharp-linq-top-three-001','csharp-json-number-count-001')
    },
    [pscustomobject]@{
        Directory='algorithm-debug-v1'; Id='algorithm-debug-v1'; Title='Examen 3 — algorithmique et débogage S5–S7'; Duration=120; Draw=8
        Candidates=@('algo-linear-search-001','algo-binary-search-001','algo-bubble-sort-001','algo-selection-sort-001','algo-insertion-sort-001','algo-merge-sorted-001','algo-rotate-left-001','algo-prefix-sums-001','algo-pair-sum-001','algo-gcd-001','structures-balanced-parentheses-001','structures-window-sums-001','structures-factorial-001','structures-tree-height-001','debug-median-001','debug-state-machine-001')
    }
)
foreach ($exam in $ExamDefinitions) {
    Write-JsonFile (Join-Path $ContentRoot "exams/$($exam.Directory)/exam.json") ([ordered]@{
        schemaVersion=1;id=$exam.Id;version=1;title=$exam.Title;durationMinutes=$exam.Duration
        drawCount=$exam.Draw;passingScore=80;eligibleExerciseIds=$exam.Candidates
    })
}
