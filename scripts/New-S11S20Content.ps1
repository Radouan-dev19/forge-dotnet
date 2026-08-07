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
    $Content = $Content.Replace([char]0x02BC, [char]0x2019)
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
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
        'int' { 'int' }
        'int[]' { 'int[]' }
        'string' { 'string' }
        default { throw "Type runner inconnu : $RunnerType" }
    }
}

function Convert-JsonCompact {
    param($Value)
    return ($Value | ConvertTo-Json -Depth 16 -Compress)
}

$LessonRows = @(
    'api-http-semantics-001§11§HTTP : méthodes, statuts et représentations§api.http§HTTP décrit une interaction par une méthode, une cible, des en-têtes, un corps et un statut. Le contrat doit distinguer succès, absence, validation et conflit.§Un POST de commande valide retourne 201 avec une localisation ; une lecture absente retourne 404 sans inventer une ressource.§Retourner 200 pour toute issue force le client à interpréter un texte libre et détruit la sémantique du protocole.',
    'api-routing-rest-001§11§Routage REST et ressources stables§api.routing§Une route nomme une ressource, pas une action dʼinterface. La méthode HTTP porte lʼintention et les identifiants restent opaques.§GET /orders/42 lit la commande 42 et DELETE /orders/42 demande sa suppression selon une politique explicite.§Des routes comme /doDeleteOrder mélangent verbe, transport et détail dʼimplémentation.',
    'api-controllers-dtos-001§11§Contrôleurs minces et DTO explicites§api.dto§Le contrôleur traduit HTTP vers un cas dʼusage. Un DTO borne le contrat public et empêche dʼexposer directement les entités persistées.§CreateOrderRequest contient seulement les champs acceptés ; OrderResponse projette seulement les données publiables.§Lier une entité EF au corps autorise une sur-affectation et couple le contrat API au stockage.',
    'api-validation-problem-details-001§12§Validation et erreurs Problem Details§api.validation§La validation syntaxique et structurelle précède le métier. Les erreurs HTTP ont une forme stable et ne divulguent ni pile ni donnée sensible.§Une quantité hors plage reçoit 400 avec un type, un titre et les champs invalides ; un conflit métier reçoit 409.§Attraper toute exception dans chaque action puis retourner son message expose des détails et duplique la politique.',
    'api-di-lifetimes-001§12§Injection de dépendances et durées de vie§api.di§Singleton, scoped et transient décrivent une propriété de durée de vie, pas une préférence esthétique. Une dépendance scoped ne doit jamais être capturée par un singleton.§Un dépôt lié à une requête est scoped ; une horloge sans état peut être singleton ; un petit service sans état peut être transient.§Rendre tous les services singleton partage involontairement état et objets non thread-safe.',
    'api-configuration-secrets-errors-001§12§Configuration, secrets et gestion centrale des erreurs§security.secrets§La configuration non sensible peut être versionnée ; un secret vient dʼun fournisseur externe au dépôt. La gestion centrale journalise un identifiant de corrélation, jamais le secret.§En local, la clé factice de test est injectée par la fabrique ; en exécution, un fichier monté ou un magasin de secrets la fournit.§Placer une clé réelle dans appsettings.json ou la recopier dans une exception la rend durable dans Git et les logs.',
    'api-async-cancellation-001§13§Async et annulation de bout en bout§api.async§Une API asynchrone transmet le CancellationToken jusquʼà la dépendance et laisse lʼannulation attendue interrompre le travail. async ne rend pas un calcul CPU plus rapide.§Une requête annulée transmet RequestAborted au dépôt et nʼest pas remplacée par CancellationToken.None.§Bloquer avec Result ou ignorer le jeton conserve du travail devenu inutile et peut épuiser les threads.',
    'api-pagination-filtering-sorting-001§13§Pagination, filtrage et tri bornés§api.pagination§Une collection publique impose une taille maximale, un ordre total et une liste blanche de tris. Les filtres sont validés avant la requête.§pageSize est borné à 100 et le tri total, date puis id, empêche doublons et pertes entre pages stables.§Accepter nʼimporte quel nom de colonne ou une taille illimitée expose coût, détails internes et déni de service.',
    'api-openapi-contracts-001§13§Contrat OpenAPI vérifiable§api.openapi§OpenAPI décrit opérations, entrées, réponses et sécurité. Le document doit correspondre au comportement observé et rester révisable dans le dépôt.§Le contrat déclare 400, 401, 403, 404 et 201 ; les tests vérifient les mêmes statuts sur lʼAPI.§Un document généré mais jamais comparé aux réponses réelles devient une documentation trompeuse.',
    'security-authentication-001§14§Authentification sans fuite dʼidentité§security.authentication§Lʼauthentification établit une identité à partir dʼune preuve. Un échec public reste générique et la comparaison dʼun secret évite les différences observables inutiles.§Une clé factice injectée en test produit une identité ; une clé absente ou fausse produit le même 401 sans valeur sensible.§Révéler que le compte existe ou journaliser le jeton facilite lʼénumération et le vol de preuve.',
    'security-authorization-roles-policies-001§14§Autorisation par rôles, politiques et ressource§security.authorization§Lʼautorisation décide après authentification et doit vérifier action et ressource. Une politique nommée est testable et centralise la règle.§OrdersWrite exige Operator ; modifier une commande exige aussi propriétaire ou administrateur selon le cas dʼusage.§Masquer un bouton sans protéger le point dʼentrée permet un appel direct non autorisé.',
    'security-owasp-api-001§14§Socle OWASP pour une API locale§security.owasp§Entrées bornées, moindre privilège, erreurs sobres, dépendances suivies et absence de secret dans les logs réduisent les risques courants sans démonstration offensive.§Une liste blanche contrôle le tri, le conteneur est non-root et une réponse 500 ne contient pas la stack trace.§Présenter une concaténation SQL ou une désactivation dʼautorisation comme raccourci acceptable normalise une vulnérabilité.',
    'tests-xunit-aaa-001§15§xUnit et structure Arrange Act Assert§tests.xunit§Un test prépare un état minimal, exécute une action et vérifie un résultat observable. Son nom explique règle, condition et résultat.§Quantity_zero_is_rejected prépare zéro, appelle la règle et vérifie le type dʼerreur sans dépendre dʼun ordre global.§Un test qui reproduit tout le code de production peut rester vert lorsque la même erreur est copiée deux fois.',
    'tests-domain-rules-001§15§Tester les règles du domaine§tests.domain§Une règle pure se teste sans serveur, base ni horloge réelle. Les exemples couvrent nominal, limites, invalides et invariants.§Une théorie teste quantité 1 et 100 comme valides, puis 0 et 101 comme invalides.§Tester seulement le contrôleur rend la cause dʼun échec lente à localiser et laisse les bornes implicites.',
    'tests-boundaries-theories-001§15§Théories et partitions de cas§tests.boundaries§Une théorie réduit la répétition lorsque plusieurs données portent la même règle. Les valeurs sont choisies par partitions et frontières, pas au hasard.§Pour une plage 1 à 100, les cas 0, 1, 100 et 101 distinguent les quatre transitions utiles.§Accumuler vingt données nominales ne compense pas lʼabsence dʼun seul cas de frontière.',
    'tests-doubles-design-001§16§Doubles de test par intention§tests.doubles§Stub fournit une réponse, fake propose une implémentation simplifiée et spy ou mock observe une collaboration. Le double choisi répond à une question précise.§Une fausse horloge fixe la date pour une règle dʼexpiration ; un spy confirme quʼune notification est envoyée une fois après succès.§Mocker chaque méthode interne couple le test à lʼimplémentation et rend un refactoring sans changement métier coûteux.',
    'tests-integration-database-001§16§Intégration et base de test isolée§tests.integration§Un test dʼintégration traverse de vraies frontières avec une ressource dédiée et réinitialisée. Aucun test ne vise la base de progression personnelle.§Chaque exécution crée un nom de base unique, applique le schéma, vérifie le comportement puis supprime la ressource.§Partager une base persistante entre tests rend lʼordre significatif et peut détruire des données hors test.',
    'tests-api-factory-001§16§Tests HTTP avec une fabrique dʼapplication§tests.api§Une fabrique démarre lʼapplication en mémoire avec configuration factice et dépendances contrôlées. Le test parle HTTP plutôt que dʼappeler directement le contrôleur.§Le client anonyme obtient 401 sur POST, le mauvais rôle 403 et lʼopérateur autorisé 201.§Remplacer lʼautorisation par un succès systématique dans tous les tests supprime précisément la preuve recherchée.',
    'quality-regression-refactoring-001§17§Non-régression avant refactoring§quality.regression§Un test de caractérisation fixe le comportement utile avant restructuration. Le refactoring change la forme, puis les preuves confirment le même contrat.§Un défaut de borne reçoit dʼabord un test rouge, puis une correction minimale, puis une extraction de méthode avec tests verts.§Réécrire et corriger simultanément sans preuve rend impossible dʼattribuer un changement de comportement.',
    'quality-static-analysis-001§17§Analyse statique et avertissements traités§quality.analysis§Compilateur, analyzers et format détectent des classes de défauts avant exécution. Un avertissement nʼest supprimé quʼavec une justification locale vérifiable.§Nullable révèle une déréférence possible ; la correction encode lʼabsence dans le contrat au lieu dʼutiliser un opérateur de suppression.§Désactiver une règle au niveau de la solution pour gagner du temps masque aussi les futurs défauts pertinents.',
    'quality-review-diffs-001§17§Revue de code centrée sur le diff§quality.review§Une revue examine contrat, sécurité, tests, lisibilité et portée du changement. Les commentaires distinguent blocage, suggestion et question.§Le reviewer relie une route non protégée à un scénario 403 manquant et demande une preuve reproductible.§Commenter uniquement le style pendant quʼune règle dʼautorisation manque donne une fausse assurance.',
    'git-commits-history-001§18§Commits cohérents et historique lisible§git.commits§Un commit porte une intention, reste compilable et explique pourquoi dans son message. Lʼhistorique facilite revue, bissection et retour ciblé.§Un commit ajoute la validation et ses tests ensemble avec un sujet impératif court.§Mélanger formatage global, fonctionnalité et données personnelles rend la revue et lʼannulation risquées.',
    'git-branches-conflicts-001§18§Branches et conflits compris§git.conflicts§Une branche isole une intention. Un conflit se résout en comprenant les deux changements, puis en exécutant les preuves du résultat combiné.§Deux branches modifient la même règle ; la résolution conserve les deux exigences et un test réfute la perte de lʼune.§Supprimer les marqueurs sans relire le comportement peut produire un fichier compilable mais métier incorrect.',
    'git-pull-requests-versions-001§18§Pull requests, revue et versions§git.pr§Une PR raconte problème, solution, risques et preuves. Une version exprime la compatibilité attendue et se décide à partir du contrat publié.§La PR montre le diff borné, les commandes exécutées et une évolution patch compatible avec le contrat existant.§Déclarer la PR sûre sans citer test, migration ni risque reporte lʼanalyse au reviewer.',
    'docker-images-layers-001§19§Images Docker reproductibles et couches utiles§docker.images§Une image part dʼune base épinglée, sépare restauration et copie du code, puis ne contient que le runtime nécessaire. Le contexte exclut les secrets.§Le Dockerfile multi-stage restaure sur les fichiers projet puis publie avant de copier vers une image runtime épinglée.§Utiliser latest et copier tout le dépôt avant restore rend le résultat variable et invalide le cache à chaque fichier.',
    'docker-runtime-security-001§19§Conteneur non-root et ressources bornées§docker.security§Le runtime applique utilisateur non-root, système en lecture seule, capacités supprimées, no-new-privileges, limites et health check.§Le service monte le secret en lecture seule, écrit seulement dans tmpfs et expose son port sur la boucle locale.§Exécuter root avec le socket Docker monté transforme une erreur applicative en contrôle potentiel de lʼhôte.',
    'docker-compose-networks-volumes-001§19§Compose, réseaux et volumes explicites§docker.compose§Compose documente services, dépendances, réseaux, volumes et secrets. Un réseau interne limite les communications et les données persistantes sont nommées.§LʼAPI seule est publiée sur 127.0.0.1 ; son réseau de données nʼest pas exposé et le secret vient dʼun fichier hors Git.§Publier toutes les bases sur toutes les interfaces augmente la surface sans besoin pédagogique.',
    'ci-pipeline-build-test-001§20§Pipeline CI : restaurer, construire et tester§ci.pipeline§La CI reproduit des commandes déterministes dans un ordre explicite et sʼarrête au premier échec. Les permissions du jeton sont minimales.§Le job restaure, construit sans restore, teste sans build puis vérifie le format et lʼimage.§Masquer un code de sortie ou ajouter continue-on-error fait passer une preuve cassée pour un succès.',
    'ci-artifacts-variables-secrets-001§20§Artefacts, variables et secrets de CI§ci.artifacts§Une variable configure un comportement non sensible ; un secret reste dans le magasin CI et nʼest jamais imprimé. Un artefact porte un nom et une rétention bornés.§Le rapport de tests est téléversé même en échec sans contenir de jeton, avec un nom dérivé dʼun identifiant de run.§Écrire toutes les variables dans les logs pour diagnostiquer peut publier un secret masqué imparfaitement.',
    'ci-deployment-gates-001§20§Livraison simple et portes de déploiement§ci.deployment§Un déploiement dépend de build et tests réussis, cible un environnement protégé et produit une preuve dʼartefact. Cette semaine reste locale et nʼanticipe aucun fournisseur cloud.§Le job de livraison vérifie lʼimage, attend une approbation dʼenvironnement et ne reçoit que les permissions nécessaires.§Déployer depuis une branche non protégée ou reconstruire un autre artefact entre test et livraison brise la traçabilité.'
)

$LessonIdsByWeek = @{}
$previousLessonId = 'ef-core-data-access-001'
foreach ($row in $LessonRows) {
    $parts = $row -split '§', 7
    $id=$parts[0];$week=[int]$parts[1];$title=$parts[2];$skill=$parts[3];$concept=$parts[4];$example=$parts[5];$mistake=$parts[6]
    if (-not $LessonIdsByWeek.ContainsKey($week)) { $LessonIdsByWeek[$week] = New-Object System.Collections.Generic.List[string] }
    $LessonIdsByWeek[$week].Add($id)
    $directory = Join-Path $CatalogRoot "curriculum/lessons/$id"
    Write-JsonFile (Join-Path $directory 'lesson.json') ([ordered]@{
        schemaVersion=1;id=$id;version=1;title=$title;week=$week;skills=@([ordered]@{id=$skill;weight=1.0})
        prerequisites=@($previousLessonId);estimatedMinutes=75;objectives=@("Appliquer $title et justifier une preuve observable sur un cas nouveau")
        sections=@('intuition','explanation','example','counterExample','check','guided','independent','debugging','interview','summary','reviewCards','masteryTest')
        markdownPath='lesson.md';license='CC-BY-4.0'
    })
    Write-TextFile (Join-Path $directory 'lesson.md') @"
# $title

## Objectif observable

À la fin de cette leçon, vous pourrez appliquer la règle à un cas nouveau, expliquer son compromis principal et écrire une preuve qui échoue avec une implémentation plausible mais incorrecte.

## Prérequis

Relire `$previousLessonId`, disposer du dépôt local et exécuter les exemples sans ressource réseau obligatoire.

## Intuition

$concept

## Explication

$concept Commencez par écrire le contrat, les entrées non fiables, la sortie observable et les limites de responsabilité. Une décision dʼarchitecture nʼest retenue que si elle réduit un risque ou rend une preuve plus directe.

## Exemple commenté

$example Modifiez ensuite une borne et un droit pour vérifier que le raisonnement, et non une valeur mémorisée, détermine le résultat.

## Contre-exemple et erreur fréquente

$mistake Reproduisez cette erreur dans un test avant de la corriger ; ne masquez ni exception ni code de sortie.

## Vérification de compréhension

Nommez le contrat public, une entrée hostile ou invalide, le statut ou résultat attendu et la preuve qui distingue autorisation, validation et erreur interne.

:::quiz
id=$id-check
question=Quelle preuve démontre le mieux la compréhension de cette leçon ?
option=Copier uniquement lʼexemple nominal
option=Prédire puis tester succès, frontière et échec pertinent sans exposer de secret
option=Désactiver la règle qui fait échouer la vérification
correct=1
success=Correct : une preuve variée et sûre réfute les erreurs plausibles.
retry=Revenez au contrat, aux frontières et au contre-exemple avant de choisir.
:::

## Exercice guidé

1. Écrivez un scénario nominal, une frontière et un refus.
2. Prédisez statut, corps et effet avant exécution.
3. Implémentez la règle dans le composant responsable.
4. Exécutez la preuve et consignez tout écart sans le masquer.

## Exercice autonome

Transposez la technique au mini-ERP local. Gardez les règles métier hors du transport, bornez les entrées, utilisez seulement des secrets factices et fournissez les commandes de reproduction.

## Débogage

Reproduisez le symptôme, formulez une hypothèse, observez la première divergence sans modifier les données, corrigez la cause puis ajoutez un test de non-régression. Les logs ne contiennent ni corps sensible ni preuve dʼauthentification.

## Entretien

Présentez en cinq minutes le contrat, le compromis, une erreur fréquente, une menace pertinente et la stratégie de tests. Distinguez clairement ce qui est démontré de ce qui reste manuel.

## Résumé

- Le contrat et les frontières précèdent lʼimplémentation.
- La sécurité est vérifiée par des refus observables et des journaux sobres.
- Une livraison nʼest verte que si toutes les commandes applicables réussissent.

## Cartes de révision

- Question : quelle frontière doit être automatisée ? Réponse : celle qui sépare deux comportements publics différents.
- Question : quelle donnée ne doit jamais entrer dans Git ou les logs ? Réponse : toute preuve dʼauthentification réelle.

## Test de maîtrise

Sans relire, réalisez une variante avec une donnée et un droit différents. Écrivez un test nominal, deux refus et une preuve de non-régression, puis défendez le compromis. Cette auto-évaluation ne valide aucune maîtrise automatiquement.
"@
    $previousLessonId = $id
}

$ExerciseRows = @(
    'api-http-status-map-001§11§Choisir un statut HTTP§2§api.http§StatusFor§bool:found,bool:created§int§if (created) return 201; return found ? 200 : 404;§[[[true,false],200],[[true,true],201]]§[[[false,false],404],[[false,true],201]]§Faire primer la création, puis distinguer ressource trouvée et absente.§O(1) en temps et O(1) en espace',
    'api-route-normalize-001§11§Normaliser un segment de route§2§api.routing§NormalizeRoute§string:value§string§if (string.IsNullOrWhiteSpace(value)) return ""; return value.Trim().Trim(''/'').ToLowerInvariant();§[[[" /Orders/ "],"orders"],[["customers"],"customers"]]§[[["///"],""],[["  Health  "],"health"]]§Retirer seulement les séparateurs de bord et normaliser avec une culture invariante.§O(n) en temps et O(n) en espace',
    'api-dto-customer-name-001§11§Projeter un nom de DTO§2§api.dto§CustomerLabel§string:name§string§return string.IsNullOrWhiteSpace(name) ? "(invalide)" : name.Trim();§[[[" Ada "],"Ada"],[[""],"(invalide)"]]§[[["   "],"(invalide)"],[["Lin"],"Lin"]]§Le DTO public reçoit un libellé normalisé sans exposer un objet de persistance.§O(n) en temps et O(n) en espace',
    'api-method-idempotency-001§11§Reconnaître une méthode idempotente§2§api.http§IsIdempotent§string:method§bool§if (string.IsNullOrWhiteSpace(method)) return false; string value = method.Trim().ToUpperInvariant(); return value is "GET" or "PUT" or "DELETE" or "HEAD" or "OPTIONS";§[[["GET"],true],[["POST"],false]]§[[[" put "],true],[["PATCH"],false]]§Comparer une méthode normalisée à la liste explicite des méthodes idempotentes.§O(n) en temps et O(n) en espace',
    'api-location-header-001§11§Construire une localisation de ressource§2§api.http§OrderLocation§int:id§string§if (id <= 0) throw new System.ArgumentOutOfRangeException(nameof(id)); return $"/orders/{id}";§[[[42],"/orders/42"],[[1],"/orders/1"]]§[[[0],"!ArgumentOutOfRangeException"],[[999],"/orders/999"]]§Refuser un identifiant non publié puis construire une route relative stable.§O(1) en temps et O(1) en espace',
    'api-order-validation-001§12§Valider une quantité de commande§2§api.validation§QuantityState§int:quantity§string§if (quantity == 0) return "required"; if (quantity < 0 || quantity > 100) return "range"; return "valid";§[[[1],"valid"],[[0],"required"]]§[[[-1],"range"],[[101],"range"]]§Distinguer absence conventionnelle, plage invalide et valeur acceptée.§O(1) en temps et O(1) en espace',
    'api-di-lifetime-choice-001§12§Choisir une durée de vie DI§2§api.di§LifetimeFor§bool:holdsRequestState,bool:statelessShared§string§if (holdsRequestState) return "scoped"; return statelessShared ? "singleton" : "transient";§[[[true,false],"scoped"],[[false,true],"singleton"]]§[[[false,false],"transient"],[[true,true],"scoped"]]§Lʼétat de requête impose scoped ; un service partagé doit être explicitement sans état.§O(1) en temps et O(1) en espace',
    'api-config-key-001§12§Composer une clé de configuration§2§api.configuration§ConfigKey§string:section,string:key§string§if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(key)) throw new System.ArgumentException("Clé incomplète."); return $"{section.Trim()}:{key.Trim()}";§[[["Authentication","ApiKey"],"Authentication:ApiKey"],[["Logging "," Level"],"Logging:Level"]]§[[["","Key"],"!ArgumentException"],[["Section"," "],"!ArgumentException"]]§Valider les deux segments et conserver le séparateur hiérarchique standard.§O(n) en temps et O(n) en espace',
    'api-secret-redaction-001§12§Masquer une valeur sensible§2§security.secrets§Redact§string:value§string§if (string.IsNullOrEmpty(value)) return ""; return new string(''*'', System.Math.Max(4, value.Length));§[[["fake-key"],"********"],[["abc"],"****"]]§[[[""],""],[["12345"],"*****"]]§Ne conserver aucun caractère du secret et produire au moins quatre marqueurs.§O(n) en temps et O(n) en espace',
    'api-error-status-001§12§Mapper une erreur publique§2§api.errors§ErrorStatus§string:kind§int§return kind?.Trim().ToLowerInvariant() switch { "validation" => 400, "notfound" => 404, "conflict" => 409, "unauthorized" => 401, "forbidden" => 403, _ => 500 };§[[["validation"],400],[["conflict"],409]]§[[[" NotFound "],404],[["database-details"],500]]§Mettre les erreurs connues sur liste blanche et rabattre les détails internes vers 500.§O(n) en temps et O(n) en espace',
    'api-page-size-001§13§Borner une taille de page§2§api.pagination§ClampPageSize§int:requested§int§if (requested <= 0) return 20; return System.Math.Min(requested, 100);§[[[25],25],[[0],20]]§[[[-5],20],[[500],100]]§Appliquer une valeur par défaut positive et un plafond strict de cent.§O(1) en temps et O(1) en espace',
    'api-skip-count-001§13§Calculer un décalage paginé§2§api.pagination§SkipCount§int:page,int:pageSize§int§if (page < 1 || pageSize < 1 || pageSize > 100) throw new System.ArgumentOutOfRangeException(); return checked((page - 1) * pageSize);§[[[1,20],0],[[3,10],20]]§[[[0,20],"!ArgumentOutOfRangeException"],[[2,101],"!ArgumentOutOfRangeException"]]§Valider page et taille avant un calcul vérifié du décalage.§O(1) en temps et O(1) en espace',
    'api-sort-whitelist-001§13§Appliquer une liste blanche de tri§2§api.pagination§NormalizeSort§string:value§string§string sort = value?.Trim().ToLowerInvariant() ?? ""; return sort is "date" or "total" or "status" ? sort : "id";§[[["date"],"date"],[["DROP TABLE"],"id"]]§[[[" Total "],"total"],[[""],"id"]]§Retourner seulement une clé publique autorisée et choisir id par défaut.§O(n) en temps et O(n) en espace',
    'api-filter-term-001§13§Filtrer sans dépendre de la culture§2§api.pagination§ContainsTerm§string:value,string:term§bool§if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(term)) return false; return value.Contains(term.Trim(), System.StringComparison.OrdinalIgnoreCase);§[[["Commande Ada","ada"],true],[["Commande","client"],false]]§[[["","x"],false],[["Open Order"," ORDER "],true]]§Refuser les termes vides puis utiliser une comparaison ordinale insensible à la casse.§O(n*m) au pire et O(n) en espace pour la normalisation',
    'api-cancellation-budget-001§13§Borner un budget dʼannulation§2§api.async§EffectiveTimeout§int:requestedSeconds,int:maximumSeconds§int§if (requestedSeconds <= 0 || maximumSeconds <= 0) throw new System.ArgumentOutOfRangeException(); return System.Math.Min(requestedSeconds, maximumSeconds);§[[[5,30],5],[[60,30],30]]§[[[0,30],"!ArgumentOutOfRangeException"],[[10,1],1]]§Valider les durées puis retenir le budget le plus contraignant.§O(1) en temps et O(1) en espace',
    'security-bearer-header-001§14§Valider le schéma Bearer§3§security.authentication§HasBearerToken§string:header§bool§if (string.IsNullOrWhiteSpace(header)) return false; const string prefix = "Bearer "; return header.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase) && header.Length > prefix.Length && !string.IsNullOrWhiteSpace(header.Substring(prefix.Length));§[[["Bearer fake-token"],true],[["Basic value"],false]]§[[["Bearer "],false],[["bearer local-test"],true]]§Vérifier le schéma et la présence dʼune preuve sans jamais retourner sa valeur.§O(n) en temps et O(1) en espace',
    'security-role-check-001§14§Vérifier un rôle déclaré§3§security.authorization§HasRole§string:roles,string:required§bool§if (string.IsNullOrWhiteSpace(roles) || string.IsNullOrWhiteSpace(required)) return false; foreach (string role in roles.Split('','', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)) if (string.Equals(role, required.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true; return false;§[[["Reader,Operator","Operator"],true],[["Reader","Admin"],false]]§[[[" admin , reader ","ADMIN"],true],[["","Reader"],false]]§Comparer chaque rôle complet sans recherche partielle ni valeur implicite.§O(n) en temps et O(n) en espace',
    'security-owner-policy-001§14§Autoriser propriétaire ou administrateur§3§security.authorization§CanEdit§string:actorId,string:ownerId,bool:isAdmin§bool§if (isAdmin) return true; if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(ownerId)) return false; return string.Equals(actorId, ownerId, System.StringComparison.Ordinal);§[[["u1","u1",false],true],[["u1","u2",false],false]]§[[["u1","u2",true],true],[["","",false],false]]§Évaluer le privilège explicite puis lʼidentité exacte de la ressource.§O(n) en temps et O(1) en espace',
    'security-local-redirect-001§14§Refuser une redirection externe§3§security.owasp§IsLocalRedirect§string:value§bool§if (string.IsNullOrWhiteSpace(value)) return false; return value.StartsWith("/", System.StringComparison.Ordinal) && !value.StartsWith("//", System.StringComparison.Ordinal) && !value.StartsWith("/\\", System.StringComparison.Ordinal);§[[["/orders/1"],true],[["https://evil.example"],false]]§[[["//evil.example"],false],[["/\\evil"],false]]§Accepter un chemin racine local mais refuser les formes réseau et les URL absolues.§O(n) en temps et O(1) en espace',
    'security-login-message-001§14§Uniformiser un échec de connexion§3§security.authentication§LoginFailure§bool:userExists,bool:proofValid§string§_ = userExists; _ = proofValid; return "Identifiants invalides.";§[[[true,false],"Identifiants invalides."],[[false,false],"Identifiants invalides."]]§[[[false,true],"Identifiants invalides."],[[true,true],"Identifiants invalides."]]§Retourner le même message public pour ne révéler ni existence ni nature de lʼéchec.§O(1) en temps et O(1) en espace',
    'tests-boundary-values-001§15§Détecter une frontière§2§tests.boundaries§IsBoundary§int:value,int:minimum,int:maximum§bool§if (minimum > maximum) throw new System.ArgumentException("Bornes inversées."); return value == minimum || value == maximum;§[[[1,1,100],true],[[50,1,100],false]]§[[[100,1,100],true],[[2,3,1],"!ArgumentException"]]§Refuser des bornes incohérentes puis comparer exactement les deux frontières.§O(1) en temps et O(1) en espace',
    'tests-quantity-rule-001§15§Tester une règle de quantité§2§tests.domain§IsValidQuantity§int:value§bool§return value is >= 1 and <= 100;§[[[1],true],[[0],false]]§[[[100],true],[[101],false]]§Exprimer directement la plage inclusive pour rendre ses quatre frontières testables.§O(1) en temps et O(1) en espace',
    'tests-shipping-theory-001§15§Paramétrer une règle de livraison§2§tests.xunit§ShippingCost§decimal:total,bool:express§decimal§if (total < 0m) throw new System.ArgumentOutOfRangeException(nameof(total)); if (express) return 9.90m; return total >= 50m ? 0m : 4.90m;§[[[49.99,false],4.9],[[50,false],0]]§[[[20,true],9.9],[[-1,false],"!ArgumentOutOfRangeException"]]§Faire varier la frontière de gratuité et le mode express dans une même théorie.§O(1) en temps et O(1) en espace',
    'tests-discount-rule-001§15§Tester des partitions de remise§2§tests.domain§DiscountRate§decimal:total§decimal§if (total < 0m) throw new System.ArgumentOutOfRangeException(nameof(total)); if (total >= 200m) return 0.15m; if (total >= 100m) return 0.05m; return 0m;§[[[99.99],0],[[100],0.05]]§[[[200],0.15],[[-1],"!ArgumentOutOfRangeException"]]§Couvrir chaque partition et les frontières exactes cent et deux cents.§O(1) en temps et O(1) en espace',
    'tests-expiry-clock-001§15§Fixer le temps dans un test§2§tests.doubles§IsExpired§date:expiresOn,date:today§bool§return expiresOn < today;§[[["2026-08-04","2026-08-05"],true],[["2026-08-05","2026-08-05"],false]]§[[["2026-08-06","2026-08-05"],false],[["2025-12-31","2026-01-01"],true]]§Recevoir la date observée au lieu de lire lʼhorloge système dans la règle.§O(1) en temps et O(1) en espace',
    'tests-double-choice-001§16§Choisir un double de test§3§tests.doubles§DoubleKind§bool:needsBehavior,bool:needsInteraction§string§if (needsInteraction) return "spy"; return needsBehavior ? "fake" : "stub";§[[[false,false],"stub"],[[true,false],"fake"]]§[[[false,true],"spy"],[[true,true],"spy"]]§Choisir spy pour lʼinteraction, fake pour le comportement et stub pour une réponse.§O(1) en temps et O(1) en espace',
    'tests-saved-identity-001§16§Vérifier une identité persistée§2§tests.integration§HasSavedIdentity§int:id§bool§return id > 0;§[[[1],true],[[0],false]]§[[[-1],false],[[999],true]]§Une intégration réussie produit un identifiant strictement positif observable.§O(1) en temps et O(1) en espace',
    'tests-database-name-001§16§Reconnaître une base de test isolée§3§tests.integration§IsIsolatedDatabase§string:name§bool§if (string.IsNullOrWhiteSpace(name)) return false; return name.StartsWith("forge-test-", System.StringComparison.Ordinal) && name.Length >= 20;§[[["forge-test-123456789"],true],[["production"],false]]§[[["forge-test-short"],false],[["Forge-test-123456789"],false]]§Exiger un préfixe réservé et un suffixe suffisamment unique, avec comparaison ordinale.§O(n) en temps et O(1) en espace',
    'tests-success-status-001§16§Tester une famille de statuts§2§tests.api§IsSuccessStatus§int:statusCode§bool§return statusCode is >= 200 and <= 299;§[[[200],true],[[404],false]]§[[[299],true],[[300],false]]§Tester les frontières de la famille 2xx plutôt quʼun seul statut nominal.§O(1) en temps et O(1) en espace',
    'tests-reset-state-001§16§Réinitialiser un état de test§2§tests.integration§ResetState§int[]:values§int[]§if (values is null) throw new System.ArgumentNullException(nameof(values)); return new int[values.Length];§[[[[1,2]],[0,0]],[[[]],[]]]§[[[[-1,5,9]],[0,0,0]],[[[0]],[0]]]§Retourner un nouvel état de même taille et préserver les données reçues.§O(n) en temps et O(n) en espace',
    'quality-null-guard-001§17§Rendre une garde nullable explicite§2§quality.analysis§NormalizeOptional§string:value§string§return string.IsNullOrWhiteSpace(value) ? "n/a" : value.Trim();§[[[" value "],"value"],[[""],"n/a"]]§[[["   "],"n/a"],[["0"],"0"]]§Traiter absence et blanc avant toute déréférence, sans opérateur de suppression.§O(n) en temps et O(n) en espace',
    'quality-regression-bounds-001§17§Fixer une non-régression de borne§2§quality.regression§IsIndexValid§int:index,int:length§bool§if (length < 0) return false; return index >= 0 && index < length;§[[[0,1],true],[[1,1],false]]§[[[-1,3],false],[[0,0],false]]§La borne supérieure est strictement inférieure à la longueur et la borne basse vaut zéro.§O(1) en temps et O(1) en espace',
    'quality-review-severity-001§17§Classer une remarque de revue§3§quality.review§ReviewSeverity§bool:breaksCorrectness,bool:securityRisk§string§if (securityRisk) return "security-blocker"; return breaksCorrectness ? "blocker" : "suggestion";§[[[true,false],"blocker"],[[false,false],"suggestion"]]§[[[false,true],"security-blocker"],[[true,true],"security-blocker"]]§Prioriser un risque de sécurité, puis un défaut de correction, puis la suggestion.§O(1) en temps et O(1) en espace',
    'quality-complexity-budget-001§17§Borner la complexité structurelle§2§quality.analysis§WithinNestingBudget§int:nestingDepth§bool§return nestingDepth is >= 0 and <= 3;§[[[0],true],[[3],true]]§[[[4],false],[[-1],false]]§Accepter zéro à trois niveaux et refuser les mesures incohérentes ou excessives.§O(1) en temps et O(1) en espace',
    'quality-diff-risk-001§17§Estimer le risque dʼun diff§3§quality.review§DiffRisk§int:changedLines,bool:touchesAuthorization§string§if (changedLines < 0) throw new System.ArgumentOutOfRangeException(nameof(changedLines)); if (touchesAuthorization || changedLines > 300) return "high"; return changedLines > 80 ? "medium" : "low";§[[[20,false],"low"],[[120,false],"medium"]]§[[[5,true],"high"],[[-1,false],"!ArgumentOutOfRangeException"]]§Un changement dʼautorisation est toujours haut risque ; le volume affine les autres cas.§O(1) en temps et O(1) en espace',
    'git-commit-subject-001§18§Valider un sujet de commit§2§git.commits§IsCommitSubjectValid§string:subject§bool§if (string.IsNullOrWhiteSpace(subject)) return false; string value = subject.Trim(); return value.Length <= 72 && !value.EndsWith(".", System.StringComparison.Ordinal);§[[["Ajoute les tests API"],true],[["Corrige."],false]]§[[[""],false],[["Message court"],true]]§Exiger un sujet non vide, borné à 72 caractères et sans point final.§O(n) en temps et O(n) en espace',
    'git-version-patch-001§18§Incrémenter une version patch§3§git.versions§NextPatch§string:version§string§string[] parts = version?.Split(''.'') ?? []; if (parts.Length != 3 || !int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor) || !int.TryParse(parts[2], out int patch) || major < 0 || minor < 0 || patch < 0) throw new System.ArgumentException("Version invalide."); return $"{major}.{minor}.{checked(patch + 1)}";§[[["1.2.3"],"1.2.4"],[["0.0.0"],"0.0.1"]]§[[["1.2"],"!ArgumentException"],[["2.10.99"],"2.10.100"]]§Valider trois entiers non négatifs et incrémenter seulement le composant patch.§O(n) en temps et O(n) en espace',
    'git-conflict-marker-001§18§Détecter un conflit non résolu§2§git.conflicts§HasConflictMarkers§string:text§bool§if (string.IsNullOrEmpty(text)) return false; return text.Contains("<<<<<<<", System.StringComparison.Ordinal) || text.Contains("=======", System.StringComparison.Ordinal) || text.Contains(">>>>>>>", System.StringComparison.Ordinal);§[[["<<<<<<< HEAD\nvalue"],true],[["clean"],false]]§[[["a=======b"],true],[[""],false]]§Détecter chacun des marqueurs Git avant compilation ou fusion.§O(n) en temps et O(1) en espace',
    'docker-memory-limit-001§19§Borner une mémoire de conteneur§2§docker.resources§ClampMemoryMb§int:requestedMb§int§if (requestedMb <= 0) return 256; return System.Math.Clamp(requestedMb, 128, 1024);§[[[512],512],[[0],256]]§[[[64],128],[[4096],1024]]§Choisir 256 par défaut puis borner toute valeur explicite entre 128 et 1024 Mo.§O(1) en temps et O(1) en espace',
    'docker-health-window-001§19§Vérifier une fenêtre de health check§2§docker.health§FitsHealthBudget§int:intervalSeconds,int:retries,int:budgetSeconds§bool§if (intervalSeconds <= 0 || retries <= 0 || budgetSeconds <= 0) return false; return checked(intervalSeconds * retries) <= budgetSeconds;§[[[5,6,30],true],[[10,4,30],false]]§[[[0,3,30],false],[[3,10,30],true]]§Valider les trois valeurs et comparer la fenêtre totale au budget.§O(1) en temps et O(1) en espace',
    'docker-hardening-policy-001§19§Vérifier un socle de durcissement§3§docker.security§IsHardened§bool:nonRoot,bool:readOnly,bool:noNewPrivileges§bool§return nonRoot && readOnly && noNewPrivileges;§[[[true,true,true],true],[[false,true,true],false]]§[[[true,false,true],false],[[true,true,false],false]]§Exiger simultanément utilisateur non-root, lecture seule et no-new-privileges.§O(1) en temps et O(1) en espace',
    'ci-job-result-001§20§Calculer le résultat dʼun job CI§2§ci.pipeline§JobResult§bool:buildPassed,bool:testsPassed§string§return buildPassed && testsPassed ? "success" : "failed";§[[[true,true],"success"],[[true,false],"failed"]]§[[[false,true],"failed"],[[false,false],"failed"]]§Le job réussit seulement si construction et tests réussissent tous les deux.§O(1) en temps et O(1) en espace',
    'ci-artifact-name-001§20§Nommer un artefact de façon stable§2§ci.artifacts§ArtifactName§string:branch,int:runNumber§string§if (string.IsNullOrWhiteSpace(branch) || runNumber <= 0) throw new System.ArgumentException("Identité de run invalide."); string safe = branch.Trim().ToLowerInvariant().Replace("/", "-"); return $"tests-{safe}-{runNumber}";§[[["main",42],"tests-main-42"],[["feature/api",7],"tests-feature-api-7"]]§[[["",1],"!ArgumentException"],[["Release ",3],"tests-release-3"]]§Normaliser la branche, remplacer le séparateur et exiger un numéro positif.§O(n) en temps et O(n) en espace',
    'ci-deploy-gate-001§20§Évaluer une porte de déploiement§3§ci.deployment§CanDeploy§bool:testsPassed,bool:protectedEnvironment,bool:approved§bool§return testsPassed && protectedEnvironment && approved;§[[[true,true,true],true],[[false,true,true],false]]§[[[true,false,true],false],[[true,true,false],false]]§Exiger simultanément preuves vertes, environnement protégé et approbation.§O(1) en temps et O(1) en espace'
)

$ExerciseIdsByWeek = @{}
$exerciseSpecs = New-Object System.Collections.Generic.List[object]
foreach ($row in $ExerciseRows) {
    $parts = $row -split '§', 13
    if ($parts.Count -ne 13) { throw "Ligne dʼexercice invalide : $($parts[0])" }
    $parameters = @($parts[6] -split ',' | ForEach-Object {
        $parameterParts = $_ -split ':', 2
        [pscustomobject]@{ RunnerType=$parameterParts[0]; Name=$parameterParts[1] }
    })
    $spec = [pscustomobject]@{
        Id=$parts[0];Week=[int]$parts[1];Title=$parts[2];Difficulty=[int]$parts[3];Skill=$parts[4];Method=$parts[5]
        Parameters=$parameters;ReturnType=$parts[7];Body=$parts[8];Visible=@($parts[9] | ConvertFrom-Json)
        Hidden=@($parts[10] | ConvertFrom-Json);Rule=$parts[11];Complexity=$parts[12]
    }
    $exerciseSpecs.Add($spec)
    if (-not $ExerciseIdsByWeek.ContainsKey($spec.Week)) { $ExerciseIdsByWeek[$spec.Week] = New-Object System.Collections.Generic.List[string] }
    $ExerciseIdsByWeek[$spec.Week].Add($spec.Id)
}

for ($index=0; $index -lt $exerciseSpecs.Count; $index++) {
    $spec=$exerciseSpecs[$index]
    $variantId=$exerciseSpecs[($index + 1) % $exerciseSpecs.Count].Id
    $directory=Join-Path $CatalogRoot "exercises/$($spec.Id)"
    $lessonPrerequisite=[string]$LessonIdsByWeek[$spec.Week][0]
    $interviewId="interview-$($spec.Id)"
    $parameterDeclarations=@($spec.Parameters | ForEach-Object { "$(Convert-TypeName $_.RunnerType) $($_.Name)" }) -join ', '
    $runnerTypes=@($spec.Parameters | ForEach-Object { $_.RunnerType })
    $returnDeclaration=Convert-TypeName $spec.ReturnType
    $solutionSource="public static class Submission`n{`n    public static $returnDeclaration $($spec.Method)($parameterDeclarations)`n    {`n        $($spec.Body)`n    }`n}"
    $starterSource="public static class Submission`n{`n    public static $returnDeclaration $($spec.Method)($parameterDeclarations)`n    {`n        throw new System.NotImplementedException(`"À implémenter par lʼapprenant.`");`n    }`n}"
    $firstVisible=$spec.Visible[0]
    Write-JsonFile (Join-Path $directory 'exercise.json') ([ordered]@{
        schemaVersion=1;id=$spec.Id;version=1;title=$spec.Title;kind='csharp';difficulty=$spec.Difficulty;skills=@($spec.Skill)
        prerequisites=@($lessonPrerequisite);estimatedMinutes=35;statement='statement.md'
        constraints=@('Conserver exactement la signature publique', $spec.Rule, 'Ne jamais dépendre du réseau, dʼun secret réel ou dʼun état global')
        examples=@([ordered]@{input=(Convert-JsonCompact $firstVisible[0]);output=(Convert-JsonCompact $firstVisible[1])})
        reflectionFields=@('reformulation','inputs','expectedOutput','edgeCases','hypothesis','plan')
        starterPath='starter/';visibleTestsPath='tests/visible/';hiddenTestsPath='tests/hidden/'
        hints=@(
            [ordered]@{level=1;kind='socratic';content="Quel comportement public de « $($spec.Title) » change exactement à la frontière ?"},
            [ordered]@{level=2;kind='location';content="Concentrez la validation et la décision dans $($spec.Method), sans état global ni journal sensible."},
            [ordered]@{level=3;kind='strategy';content=$spec.Rule},
            [ordered]@{level=4;kind='partial-pseudocode';content='valider les entrées ; appliquer la décision bornée ; retourner un résultat déterministe sans effet externe'}
        )
        solution=[ordered]@{path='solution/';unlock=[ordered]@{seriousAttempts=2;minimumDelayMinutes=10}}
        explanation='explanation.md';complexity=$spec.Complexity
        commonMistakes=@('Coder seulement les exemples visibles', 'Masquer une erreur ou accepter une entrée hors contrat', 'Confondre une règle pure avec le transport ou une ressource externe')
        variantId=$variantId;reviewCards=@("card-$($spec.Id)-rule","card-$($spec.Id)-security")
        interviewQuestionId=$interviewId;license='CC-BY-4.0'
    })
    Write-TextFile (Join-Path $directory 'statement.md') @"
# $($spec.Title)

Implémentez `Submission.$($spec.Method)` avec la signature fournie. $($spec.Rule)

Le résultat reste déterministe et hors ligne. Écrivez avant le code un cas nominal, une frontière, un refus et la menace ou régression que ces preuves préviennent. Nʼutilisez aucun secret réel.

Exemple : entrée `$(Convert-JsonCompact $firstVisible[0])`, sortie `$(Convert-JsonCompact $firstVisible[1])`.
"@
    Write-TextFile (Join-Path $directory 'explanation.md') "# Explication`n`n$($spec.Rule) La solution sépare validation et décision, sans état externe. Complexité : **$($spec.Complexity)**. Les cas cachés varient les frontières et réfutent une constante mémorisée. Après lecture, la tentative nʼest pas maîtrisée : expliquez la règle avec vos mots et planifiez une reprise à blanc."
    Write-TextFile (Join-Path $directory 'review-cards.md') "# Cartes de révision`n`n## card-$($spec.Id)-rule`n`n**Question :** Quelle règle gouverne cet exercice ?  `n**Réponse attendue :** $($spec.Rule)`n`n## card-$($spec.Id)-security`n`n**Question :** Quelle preuve empêche un contournement ?  `n**Réponse attendue :** Un cas de frontière ou de refus différent des exemples visibles."
    Write-TextFile (Join-Path $directory 'starter/Submission.cs') $starterSource
    Write-TextFile (Join-Path $directory 'solution/Submission.cs') $solutionSource
    Write-TextFile (Join-Path $directory 'solution/README.md') "# Choix de référence`n`n$($spec.Rule) $($spec.Complexity). Cette solution ne vaut pas preuve de maîtrise."
    Write-JsonFile (Join-Path $directory 'tests/runner.json') ([ordered]@{schemaVersion=1;suiteId="$($spec.Id).v1";exerciseId=$spec.Id;exerciseVersion=1;typeName='Submission';methodName=$spec.Method;parameterTypes=$runnerTypes;returnType=$spec.ReturnType})
    foreach ($visibility in @('visible','hidden')) {
        $rawCases=if($visibility -eq 'visible'){$spec.Visible}else{$spec.Hidden}
        $cases=New-Object System.Collections.Generic.List[object]
        for($caseIndex=0;$caseIndex -lt $rawCases.Count;$caseIndex++){
            $pair=$rawCases[$caseIndex];$expected=$pair[1]
            $case=[ordered]@{name="$(if($visibility -eq 'visible'){'Visible'}else{'Hidden'})_Case$($caseIndex+1)";message="$($spec.Title) — cas $($caseIndex+1) incorrect.";arguments=@($pair[0]);expected=$null;expectedException=$null;argumentsUnchanged=($spec.Parameters.RunnerType -contains 'int[]')}
            if($expected -is [string] -and $expected.StartsWith('!')){$case.expectedException=$expected.Substring(1);$case.Remove('expected')}else{$case.expected=$expected}
            $cases.Add($case)
        }
        Write-JsonFile (Join-Path $directory "tests/$visibility/cases.json") ([ordered]@{schemaVersion=1;cases=$cases.ToArray()})
    }
    Write-JsonFile (Join-Path $CatalogRoot "interviews/$interviewId.json") ([ordered]@{
        schemaVersion=1;id=$interviewId;version=1;title=$spec.Title;level=$(if($spec.Difficulty -ge 3){'intermediate'}else{'junior'});durationMinutes=6
        skills=@($spec.Skill);question="Comment implémenteriez-vous « $($spec.Title) » et quelles preuves réfutent les contournements ?"
        observableCriteria=@('Le contrat, une frontière et un refus sont explicités', 'La complexité et une preuve de sécurité ou de non-régression sont justifiées')
        modelAnswer="$($spec.Rule) Je sépare validation et décision, je teste un cas nominal et les frontières, puis je vérifie quʼaucune donnée sensible ni état externe nʼentre dans la preuve. $($spec.Complexity)."
        commonMistakes=@('Réciter le code sans expliquer le contrat ni la preuve');variants=@("Changer la frontière ou le droit tout en conservant la règle de $($spec.Title.ToLowerInvariant()).");license='CC-BY-4.0'
    })
}

$curriculumPath = Join-Path $CatalogRoot 'curriculum/forge-reference.json'
$currentCurriculum = Get-Content -Raw $curriculumPath | ConvertFrom-Json
$modules = New-Object System.Collections.Generic.List[object]
foreach ($module in $currentCurriculum.modules | Select-Object -First 10) { $modules.Add($module) }
for ($week=11; $week -le 20; $week++) {
    $modules.Add([ordered]@{
        id="week-$week";title="Semaine $week";weeks=@($week);prerequisites=@("week-$($week-1)")
        lessonIds=$LessonIdsByWeek[$week].ToArray();exerciseIds=$ExerciseIdsByWeek[$week].ToArray()
    })
}
Write-JsonFile $curriculumPath ([ordered]@{
    schemaVersion=1;id='forge-reference';version=3;title='Forge.NET — socle professionnel S1 à S20'
    description='Parcours local autonome couvrant C#, algorithmique, débogage, SQL, EF Core, API ASP.NET Core, sécurité, tests, Git, Docker et intégration continue des semaines 1 à 20.'
    weeks=20;modules=$modules.ToArray();license='CC-BY-4.0'
})

$ProjectDefinitions = @(
    [pscustomobject]@{
        Id='project-api-mini-erp-001';Title='API mini-ERP sécurisée';Difficulty=4;Weeks=@(11,12,13,14);Hours=28
        Skills=@('api.http','api.validation','api.async','security.authorization');Prerequisite='api-http-semantics-001'
        Brief='Construire une tranche API de commandes : DTO explicites, validation, erreurs Problem Details, pagination bornée, annulation et autorisation testée.'
        Milestones=@('contract','validation','security','defense')
    },
    [pscustomobject]@{
        Id='project-testing-strategy-001';Title='Stratégie de tests du mini-ERP';Difficulty=4;Weeks=@(15,16,17);Hours=20
        Skills=@('tests.domain','tests.integration','tests.api','quality.review');Prerequisite='tests-xunit-aaa-001'
        Brief='Définir une pyramide de preuves utile : règles pures, doubles justifiés, intégration isolée, API, non-régression et revue contradictoire du diff.'
        Milestones=@('risk-map','unit-tests','integration-tests','review')
    },
    [pscustomobject]@{
        Id='project-container-delivery-001';Title='Livraison conteneurisée locale';Difficulty=4;Weeks=@(18,19,20);Hours=22
        Skills=@('git.pr','docker.security','docker.compose','ci.pipeline');Prerequisite='git-commits-history-001'
        Brief='Préparer une PR bornée, résoudre un conflit en bac à sable, construire une image non-root, vérifier sa santé et exécuter la pipeline locale sans secret réel.'
        Milestones=@('pull-request','container','pipeline','defense')
    }
)
for ($index=0; $index -lt $ProjectDefinitions.Count; $index++) {
    $project=$ProjectDefinitions[$index]
    $milestones=New-Object System.Collections.Generic.List[object]
    foreach ($milestoneId in $project.Milestones) {
        $milestones.Add([ordered]@{
            id=$milestoneId;title=($milestoneId.Replace('-',' '));evidence="Une preuve versionnée et reproductible démontre lʼétape $milestoneId sans masquer les échecs."
            acceptanceCriteria=@('La commande de vérification et son résultat sont consignés','Une frontière, un refus ou un risque est couvert par un test utile')
        })
    }
    Write-JsonFile (Join-Path $CatalogRoot "projects/$($project.Id).json") ([ordered]@{
        schemaVersion=1;id=$project.Id;version=1;title=$project.Title;difficulty=$project.Difficulty;weeks=$project.Weeks
        skills=$project.Skills;prerequisites=@($project.Prerequisite);estimatedHours=$project.Hours;briefPath="$($project.Id).md";milestones=$milestones.ToArray()
        rubric=@(
            [ordered]@{criterion='Comportement et contrat';weight=0.4;observableEvidence='Les cas nominaux, frontières et refus correspondent au contrat publié.'},
            [ordered]@{criterion='Qualité et isolation des preuves';weight=0.35;observableEvidence='Les tests sont déterministes, isolés et échouent avec une implémentation incorrecte plausible.'},
            [ordered]@{criterion='Sécurité et défense orale';weight=0.25;observableEvidence='Les secrets restent factices ou hors Git et les compromis sont défendus avec leurs limites.'}
        )
        solutionPolicy='no-complete-solution-before-submission';commonMistakes=@('Contourner une vérification au lieu de corriger sa cause','Inclure un secret ou une solution complète dans le livrable','Présenter une étape manuelle comme une validation automatique')
        variantIds=@($ProjectDefinitions[($index+1)%$ProjectDefinitions.Count].Id);license='CC-BY-4.0'
    })
    Write-TextFile (Join-Path $CatalogRoot "projects/$($project.Id).md") @"
# $($project.Title)

$($project.Brief)

## Livrables

- contrat et matrice de risques écrits avant lʼimplémentation ;
- code produit par lʼapprenant, sans solution finale fournie par Forge.NET ;
- preuves automatisées utiles et commandes PowerShell reproductibles ;
- journal dʼun défaut reproduit, corrigé et couvert par non-régression ;
- revue de sécurité et défense orale de dix minutes.

## Garde-fous

Aucun secret réel, aucune dépendance réseau obligatoire et aucun échec masqué. Après consultation dʼun exemple de référence, la tentative reste non maîtrisée et une reprise à blanc est planifiée.
"@
}

$ExamDefinitions = @(
    [pscustomobject]@{
        Directory='api-security-v1';Id='api-security-v1';Title='Examen 5 — API et sécurité S11–S14';Duration=120;Draw=8
        Candidates=@('api-http-status-map-001','api-route-normalize-001','api-dto-customer-name-001','api-method-idempotency-001','api-location-header-001','api-order-validation-001','api-di-lifetime-choice-001','api-config-key-001','api-secret-redaction-001','api-error-status-001','api-page-size-001','api-sort-whitelist-001','security-bearer-header-001','security-role-check-001','security-owner-policy-001','security-local-redirect-001')
    },
    [pscustomobject]@{
        Directory='tests-quality-v1';Id='tests-quality-v1';Title='Examen 6 — tests et qualité S15–S17';Duration=120;Draw=8
        Candidates=@('tests-boundary-values-001','tests-quantity-rule-001','tests-shipping-theory-001','tests-discount-rule-001','tests-expiry-clock-001','tests-double-choice-001','tests-saved-identity-001','tests-database-name-001','tests-success-status-001','tests-reset-state-001','quality-null-guard-001','quality-regression-bounds-001','quality-review-severity-001','quality-complexity-budget-001','quality-diff-risk-001')
    }
)
foreach ($exam in $ExamDefinitions) {
    Write-JsonFile (Join-Path $ContentRoot "exams/$($exam.Directory)/exam.json") ([ordered]@{
        schemaVersion=1;id=$exam.Id;version=1;title=$exam.Title;durationMinutes=$exam.Duration;drawCount=$exam.Draw;passingScore=80;eligibleExerciseIds=$exam.Candidates
    })
}

$ApiRoot = Join-Path $ContentRoot 'labs/api-mini-erp'
Write-TextFile (Join-Path $ApiRoot 'README.md') @'
# Laboratoire API mini-ERP S11–S16

Ce laboratoire de référence démontre une tranche HTTP réelle sans devenir le projet final de lʼapprenant. Il couvre DTO, validation automatique, Problem Details, pagination bornée, annulation, authentification par clés factices injectées et autorisation par politique.

```powershell
dotnet test content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj
```

Les clés dʼexécution viennent uniquement de la configuration de test ou de fichiers montés hors Git. Elles ne sont jamais journalisées. Le contrat `openapi.json` est comparé aux routes et statuts testés.
'@
Write-TextFile (Join-Path $ApiRoot 'src/ForgeApiLab/ForgeApiLab.csproj') @'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
'@
Write-TextFile (Join-Path $ApiRoot 'src/ForgeApiLab/Program.cs') @'
using ForgeApiLab.Security;
using ForgeApiLab.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddSingleton<OrderStore>();
builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.AuthenticationSchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationSchemeName, _ => { });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("OrdersWrite", policy => policy.RequireRole("Operator")));

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Text("Healthy")).AllowAnonymous();
app.MapControllers();
app.Run();

public partial class Program;
'@
Write-TextFile (Join-Path $ApiRoot 'src/ForgeApiLab/Security/ApiKeyAuthenticationHandler.cs') @'
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ForgeApiLab.Security;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationSchemeName = "ApiKey";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var supplied) || string.IsNullOrWhiteSpace(supplied))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string? role = Matches(supplied!, ReadConfiguredSecret("Operator")) ? "Operator"
            : Matches(supplied!, ReadConfiguredSecret("Reader")) ? "Reader"
            : null;
        if (role is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Preuve dʼauthentification invalide."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, role.ToLowerInvariant()), new Claim(ClaimTypes.Role, role)],
            AuthenticationSchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationSchemeName)));
    }

    private string? ReadConfiguredSecret(string identity)
    {
        string? direct = configuration[$"Authentication:{identity}ApiKey"];
        if (!string.IsNullOrWhiteSpace(direct)) return direct;
        string? path = configuration[$"Authentication:{identity}ApiKeyFile"];
        if (string.IsNullOrWhiteSpace(path)) return null;
        var info = new FileInfo(Path.GetFullPath(path));
        if (!info.Exists || info.Length is < 8 or > 4096) return null;
        return File.ReadAllText(info.FullName).Trim();
    }

    private static bool Matches(string supplied, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected)) return false;
        byte[] left = Encoding.UTF8.GetBytes(supplied);
        byte[] right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
'@
Write-TextFile (Join-Path $ApiRoot 'src/ForgeApiLab/Models/OrderContracts.cs') @'
using System.ComponentModel.DataAnnotations;

namespace ForgeApiLab.Models;

public sealed record CreateOrderRequest(
    [Required, StringLength(80, MinimumLength = 2)] string Customer,
    [Range(1, 100)] int Quantity);

public sealed record OrderResponse(int Id, string Customer, int Quantity, DateTimeOffset CreatedAtUtc);
'@
Write-TextFile (Join-Path $ApiRoot 'src/ForgeApiLab/Services/OrderStore.cs') @'
using System.Collections.Concurrent;
using ForgeApiLab.Models;

namespace ForgeApiLab.Services;

public sealed class OrderStore
{
    private readonly ConcurrentDictionary<int, OrderResponse> _orders = new();
    private int _nextId;

    public OrderStore()
    {
        var seed = new OrderResponse(1, "Ada", 2, new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));
        _orders[seed.Id] = seed;
        _nextId = seed.Id;
    }

    public ValueTask<OrderResponse?> FindAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_orders.GetValueOrDefault(id));
    }

    public ValueTask<IReadOnlyList<OrderResponse>> ListAsync(int page, int pageSize, string sort, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<OrderResponse> ordered = sort == "customer"
            ? _orders.Values.OrderBy(order => order.Customer, StringComparer.Ordinal).ThenBy(order => order.Id)
            : _orders.Values.OrderBy(order => order.Id);
        return ValueTask.FromResult<IReadOnlyList<OrderResponse>>(
            ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray());
    }

    public ValueTask<OrderResponse> AddAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int id = Interlocked.Increment(ref _nextId);
        var created = new OrderResponse(id, request.Customer.Trim(), request.Quantity, DateTimeOffset.UtcNow);
        _orders[id] = created;
        return ValueTask.FromResult(created);
    }
}
'@
Write-TextFile (Join-Path $ApiRoot 'src/ForgeApiLab/Controllers/OrdersController.cs') @'
using ForgeApiLab.Models;
using ForgeApiLab.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeApiLab.Controllers;

[ApiController]
[Authorize]
[Route("orders")]
public sealed class OrdersController(OrderStore store) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "id",
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || sort is not ("id" or "customer"))
        {
            ModelState.AddModelError("pagination", "Page, taille ou tri invalide.");
            return ValidationProblem(ModelState);
        }

        return Ok(await store.ListAsync(page, pageSize, sort, cancellationToken));
    }

    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        OrderResponse? order = await store.FindAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    [Authorize(Policy = "OrdersWrite")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        OrderResponse created = await store.AddAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetById), new { id = created.Id }, created);
    }
}
'@
Write-TextFile (Join-Path $ApiRoot 'openapi.json') @'
{
  "openapi": "3.0.3",
  "info": { "title": "Forge API Lab", "version": "1.0.0" },
  "paths": {
    "/health": { "get": { "responses": { "200": { "description": "Service sain" } } } },
    "/orders": {
      "get": { "security": [{ "ApiKey": [] }], "responses": { "200": { "description": "Page bornée" }, "400": { "description": "Paramètres invalides" }, "401": { "description": "Non authentifié" } } },
      "post": { "security": [{ "ApiKey": [] }], "responses": { "201": { "description": "Commande créée" }, "400": { "description": "DTO invalide" }, "401": { "description": "Non authentifié" }, "403": { "description": "Non autorisé" } } }
    },
    "/orders/{id}": { "get": { "security": [{ "ApiKey": [] }], "responses": { "200": { "description": "Commande" }, "401": { "description": "Non authentifié" }, "404": { "description": "Absente" } } } }
  },
  "components": { "securitySchemes": { "ApiKey": { "type": "apiKey", "in": "header", "name": "X-Api-Key" } } }
}
'@
Write-TextFile (Join-Path $ApiRoot 'tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj') @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/ForgeApiLab/ForgeApiLab.csproj" />
  </ItemGroup>
  <ItemGroup><Using Include="Xunit" /></ItemGroup>
</Project>
'@
Write-TextFile (Join-Path $ApiRoot 'tests/ForgeApiLab.Tests/ApiContractTests.cs') @'
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForgeApiLab.Tests;

public sealed class ApiContractTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ApiContractTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task HealthIsAnonymousAndHealthy()
    {
        using HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MissingProofIsUnauthorizedAndReaderCannotCreate()
    {
        using HttpClient anonymous = _factory.CreateClient();
        using HttpResponseMessage unauthorized = await anonymous.PostAsJsonAsync("/orders", new { customer = "Ada", quantity = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using HttpClient reader = _factory.CreateAuthenticatedClient("forge-fake-reader-key");
        using HttpResponseMessage forbidden = await reader.PostAsJsonAsync("/orders", new { customer = "Ada", quantity = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task InvalidDtoReturnsProblemDetailsAndOperatorCreates()
    {
        using HttpClient client = _factory.CreateAuthenticatedClient("forge-fake-operator-key");
        using HttpResponseMessage invalid = await client.PostAsJsonAsync("/orders", new { customer = "", quantity = 0 });
        string invalidBody = await invalid.Content.ReadAsStringAsync();
        Assert.True(invalid.StatusCode == HttpStatusCode.BadRequest, $"Statut {(int)invalid.StatusCode}. Corps: {invalidBody}");
        Assert.Contains("application/problem+json", invalid.Content.Headers.ContentType?.MediaType, StringComparison.Ordinal);

        using HttpResponseMessage created = await client.PostAsJsonAsync("/orders", new { customer = " Grace ", quantity = 3 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.StartsWith("/orders/", created.Headers.Location?.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaginationIsBoundedAndMissingResourceIsNotFound()
    {
        using HttpClient client = _factory.CreateAuthenticatedClient("forge-fake-reader-key");
        using HttpResponseMessage invalidPage = await client.GetAsync("/orders?page=1&pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
        using HttpResponseMessage missing = await client.GetAsync("/orders/999999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
            services.AddDataProtection().UseEphemeralDataProtectionProvider());
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Authentication:OperatorApiKey"] = "forge-fake-operator-key",
                ["Authentication:ReaderApiKey"] = "forge-fake-reader-key",
            }));
    }

    public HttpClient CreateAuthenticatedClient(string key)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Api-Key", key);
        return client;
    }
}
'@

$TestingRoot = Join-Path $ContentRoot 'labs/testing-strategy'
Write-TextFile (Join-Path $TestingRoot 'README.md') @'
# Laboratoire de stratégie de tests S15–S17

La bibliothèque contient une règle métier pure et les tests démontrent frontières, invalides et non-régression. La fausse horloge est injectée par valeur ; aucune base partagée ni réseau nʼest requis.

```powershell
dotnet test content/labs/testing-strategy/ForgeTestingLab.Tests/ForgeTestingLab.Tests.csproj
```
'@
Write-TextFile (Join-Path $TestingRoot 'ForgeTestingLab/ForgeTestingLab.csproj') @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
</Project>
'@
Write-TextFile (Join-Path $TestingRoot 'ForgeTestingLab/OrderPolicy.cs') @'
namespace ForgeTestingLab;

public static class OrderPolicy
{
    public static decimal NetTotal(decimal total, int quantity, DateOnly expiresOn, DateOnly today)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, 100);
        if (expiresOn < today) throw new InvalidOperationException("Offre expirée.");
        decimal rate = total >= 200m ? 0.15m : total >= 100m ? 0.05m : 0m;
        return decimal.Round(total * (1m - rate), 2, MidpointRounding.AwayFromZero);
    }
}
'@
Write-TextFile (Join-Path $TestingRoot 'ForgeTestingLab.Tests/ForgeTestingLab.Tests.csproj') @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><IsPackable>false</IsPackable></PropertyGroup>
  <ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" /><PackageReference Include="xunit" /><PackageReference Include="xunit.runner.visualstudio" /></ItemGroup>
  <ItemGroup><ProjectReference Include="../ForgeTestingLab/ForgeTestingLab.csproj" /></ItemGroup>
  <ItemGroup><Using Include="Xunit" /></ItemGroup>
</Project>
'@
Write-TextFile (Join-Path $TestingRoot 'ForgeTestingLab.Tests/OrderPolicyTests.cs') @'
using ForgeTestingLab;

namespace ForgeTestingLab.Tests;

public sealed class OrderPolicyTests
{
    private static readonly DateOnly Today = new(2026, 8, 5);

    [Theory]
    [InlineData(99.99, 99.99)]
    [InlineData(100, 95)]
    [InlineData(200, 170)]
    public void NetTotalUsesDiscountBoundaries(double total, double expected) =>
        Assert.Equal((decimal)expected, OrderPolicy.NetTotal((decimal)total, 1, Today, Today));

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void NetTotalRejectsQuantityOutsideInclusiveRange(int quantity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderPolicy.NetTotal(10m, quantity, Today, Today));

    [Fact]
    public void NetTotalUsesInjectedDateAtExpiryBoundary()
    {
        Assert.Equal(10m, OrderPolicy.NetTotal(10m, 1, Today, Today));
        Assert.Throws<InvalidOperationException>(() => OrderPolicy.NetTotal(10m, 1, Today.AddDays(-1), Today));
    }
}
'@

$GitRoot = Join-Path $ContentRoot 'labs/git-review'
Write-TextFile (Join-Path $GitRoot 'README.md') @'
# Bac à sable Git et revue S18

`verify-conflict.ps1` crée un dépôt temporaire, provoque un conflit réel, prouve les marqueurs, résout en conservant les deux exigences puis valide lʼhistorique. Aucun dépôt de travail nʼest modifié.

La revue manuelle examine ensuite le diff avec cette grille : contrat, correction, sécurité, tests, migrations, portée et commandes reproduites. Toute remarque est classée blocage, risque sécurité, question ou suggestion.
'@
Write-TextFile (Join-Path $GitRoot 'verify-conflict.ps1') @'
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) ("ForgeDotNet-GitLab-" + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($sandbox) | Out-Null
try {
    Push-Location $sandbox
    & git init --initial-branch=main
    & git config user.name 'Forge Lab'
    & git config user.email 'forge-lab@example.invalid'
    [System.IO.File]::WriteAllText((Join-Path $sandbox 'policy.txt'), "validation=true`nauthorization=true`n")
    & git add policy.txt
    & git commit -m 'Ajoute la politique de base'
    & git switch -c validation
    [System.IO.File]::WriteAllText((Join-Path $sandbox 'policy.txt'), "validation=bounded`nauthorization=true`n")
    & git commit -am 'Borne la validation'
    & git switch main
    & git switch -c authorization
    [System.IO.File]::WriteAllText((Join-Path $sandbox 'policy.txt'), "validation=true`nauthorization=policy`n")
    & git commit -am 'Applique la politique dʼautorisation'
    & git merge validation
    if ($LASTEXITCODE -eq 0) { throw 'Le conflit attendu nʼa pas été produit.' }
    $conflicted = [System.IO.File]::ReadAllText((Join-Path $sandbox 'policy.txt'))
    if (-not $conflicted.Contains('<<<<<<<') -or -not $conflicted.Contains('>>>>>>>')) { throw 'Les marqueurs de conflit sont absents.' }
    [System.IO.File]::WriteAllText((Join-Path $sandbox 'policy.txt'), "validation=bounded`nauthorization=policy`n")
    & git add policy.txt
    & git commit -m 'Résout les politiques sans perte'
    $resolved = [System.IO.File]::ReadAllText((Join-Path $sandbox 'policy.txt'))
    if ($resolved -ne "validation=bounded`nauthorization=policy`n") { throw 'La résolution a perdu une exigence.' }
}
finally {
    Pop-Location
    if (Test-Path -LiteralPath $sandbox) { Remove-Item -LiteralPath $sandbox -Recurse -Force }
}
'@

$ContainerRoot = Join-Path $ContentRoot 'labs/container-delivery'
Write-TextFile (Join-Path $ContainerRoot 'README.md') @'
# Livraison conteneurisée locale S19–S20

Lʼimage utilise des bases épinglées, un runtime non-root et un contexte borné. Compose publie seulement sur la boucle locale, monte les preuves dʼauthentification depuis des fichiers hors Git, supprime les capacités, active `no-new-privileges`, limite mémoire, CPU et PID, rend le système de fichiers en lecture seule et ajoute un health check.

```powershell
$env:FORGE_OPERATOR_KEY_FILE = 'C:\chemin\hors-git\operator-key.txt'
$env:FORGE_READER_KEY_FILE = 'C:\chemin\hors-git\reader-key.txt'
docker compose -f content/labs/container-delivery/compose.yaml config
docker compose -f content/labs/container-delivery/compose.yaml up --build --wait
docker compose -f content/labs/container-delivery/compose.yaml down --volumes
```

Utilisez uniquement des valeurs factices dans un bac à sable. Ne copiez jamais un secret réel dans le dépôt ou les logs.
'@
Write-TextFile (Join-Path $ContainerRoot 'Dockerfile') @'
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0.302-alpine3.23@sha256:d8ee39817ca03a3757288e83c37ed73cc969a286c603b827c7cbe33add1c2d1c AS build
WORKDIR /source
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY content/labs/api-mini-erp/src/ForgeApiLab/ForgeApiLab.csproj content/labs/api-mini-erp/src/ForgeApiLab/
RUN --mount=type=cache,id=forge-s11-s20-nuget,target=/root/.nuget/packages dotnet restore content/labs/api-mini-erp/src/ForgeApiLab/ForgeApiLab.csproj
COPY content/labs/api-mini-erp/src/ForgeApiLab/ content/labs/api-mini-erp/src/ForgeApiLab/
RUN --mount=type=cache,id=forge-s11-s20-nuget,target=/root/.nuget/packages dotnet publish content/labs/api-mini-erp/src/ForgeApiLab/ForgeApiLab.csproj --configuration Release --no-restore --output /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.23@sha256:27b6b84beeede74fd16886177d360799c8e4299ceadfbd64eef57bafead7878a
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ForgeApiLab.dll"]
'@
Write-TextFile (Join-Path $ContainerRoot 'compose.yaml') @'
services:
  api-lab:
    image: forge-api-lab:s11-s20
    build:
      context: ../../..
      dockerfile: content/labs/container-delivery/Dockerfile
    init: true
    environment:
      Authentication__OperatorApiKeyFile: /run/secrets/operator_api_key
      Authentication__ReaderApiKeyFile: /run/secrets/reader_api_key
      DOTNET_EnableDiagnostics: "0"
    secrets:
      - operator_api_key
      - reader_api_key
    ports:
      - "127.0.0.1:${FORGE_API_LAB_PORT:-5099}:8080"
    read_only: true
    tmpfs:
      - /tmp:size=32m,mode=1777
    cap_drop:
      - ALL
    security_opt:
      - no-new-privileges:true
    pids_limit: 64
    mem_limit: 256m
    cpus: 0.5
    restart: "no"
    healthcheck:
      test: ["CMD", "wget", "-q", "--spider", "http://127.0.0.1:8080/health"]
      interval: 3s
      timeout: 2s
      retries: 10
      start_period: 5s

secrets:
  operator_api_key:
    file: ${FORGE_OPERATOR_KEY_FILE:-./secrets/operator-key.txt}
  reader_api_key:
    file: ${FORGE_READER_KEY_FILE:-./secrets/reader-key.txt}
'@
Write-TextFile (Join-Path $ContainerRoot 'secrets/.gitignore') "*`n!.gitignore"

$CiRoot = Join-Path $ContentRoot 'labs/ci-delivery'
Write-TextFile (Join-Path $CiRoot 'README.md') @'
# Pipeline CI locale S20

`workflow.yml` limite les permissions à la lecture, construit et teste avant la répétition de livraison, publie un rapport borné et ne reçoit aucun secret. Les actions officielles sont référencées par version majeure ; dans une organisation, la politique de dépendances peut imposer leurs SHA autorisés.

`verify-ci.ps1` exécute localement les mêmes commandes de compilation, tests et image, en sʼarrêtant au premier code de sortie non nul.
'@
Write-TextFile (Join-Path $CiRoot 'workflow.yml') @'
name: forge-s11-s20-ci

on:
  pull_request:
  push:
    branches: [main]

permissions:
  contents: read

jobs:
  build-test:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - uses: actions/checkout@v4
        with:
          persist-credentials: false
      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json
      - name: Restore API lab
        run: dotnet restore content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj
      - name: Build API lab
        run: dotnet build content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj --no-restore --configuration Release
      - name: Test API lab
        run: dotnet test content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj --no-build --configuration Release --logger trx --results-directory artifacts/test-results
      - name: Test strategy lab
        run: dotnet test content/labs/testing-strategy/ForgeTestingLab.Tests/ForgeTestingLab.Tests.csproj --configuration Release
      - name: Build hardened image
        run: docker build --file content/labs/container-delivery/Dockerfile --tag forge-api-lab:${{ github.run_id }} .
      - name: Upload bounded test evidence
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: api-test-results-${{ github.run_id }}
          path: artifacts/test-results
          if-no-files-found: error
          retention-days: 7

  delivery-rehearsal:
    needs: build-test
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    environment: protected-local-rehearsal
    steps:
      - name: Record immutable delivery decision
        run: echo "Delivery gate accepted for tested run $GITHUB_RUN_ID"
'@
Write-TextFile (Join-Path $CiRoot 'verify-ci.ps1') @'
[CmdletBinding()]
param([switch]$SkipDocker)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
Push-Location $root
try {
    & dotnet restore content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj --disable-parallel
    if ($LASTEXITCODE -ne 0) { throw "Restauration API en échec : $LASTEXITCODE" }
    & dotnet build content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Compilation API en échec : $LASTEXITCODE" }
    & dotnet test content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj --no-build --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Tests API en échec : $LASTEXITCODE" }
    & dotnet test content/labs/testing-strategy/ForgeTestingLab.Tests/ForgeTestingLab.Tests.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Tests de stratégie en échec : $LASTEXITCODE" }
    if (-not $SkipDocker) {
        & docker build --file content/labs/container-delivery/Dockerfile --tag forge-api-lab:s11-s20 .
        if ($LASTEXITCODE -ne 0) { throw "Construction Docker en échec : $LASTEXITCODE" }
    }
}
finally { Pop-Location }
'@
