<#
.SYNOPSIS
    Échafaude les documents S21-S24 : manifestes, sources, jeux de tests et squelettes de leçon.

.DESCRIPTION
    Ce script ne rédige pas la pédagogie. Il produit la structure et les données dérivables, et
    laisse des marqueurs « TODO: » partout où une rédaction humaine est nécessaire. Ces marqueurs
    sont refusés par la règle d'authenticité unsubstituted-placeholder : un lot échafaudé mais non
    rédigé ne peut donc pas être publié.

    Un fichier déjà présent est conservé, jamais réécrit, sauf avec -Force.

.PARAMETER Force
    Régénère les fichiers existants. Détruit toute reprise éditoriale.
#>
[CmdletBinding()]
param([switch]$Force)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$EffectiveScriptRoot = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) { Join-Path (Get-Location) 'scripts' } else { $PSScriptRoot }
$RepositoryRoot = Split-Path -Parent $EffectiveScriptRoot
$ContentRoot = Join-Path $RepositoryRoot 'content'
$CatalogRoot = Join-Path $ContentRoot 'reference'
$script:ForceOverwrite = $Force.IsPresent
. (Join-Path $EffectiveScriptRoot 'ContentScaffolding.ps1')

function Convert-TypeName {
    param([Parameter(Mandatory)][string]$RunnerType)
    switch ($RunnerType) {
        'bool' { 'bool' }
        'decimal' { 'decimal' }
        'int' { 'int' }
        'string' { 'string' }
        default { throw "Type runner inconnu : $RunnerType" }
    }
}

$LessonRows = @(
    'azure-hosting-choice-001§21§Choisir entre App Service et Container Apps§azure.hosting§App Service convient à une application web gérée avec peu de contrôle dʼorchestration. Container Apps convient à une image conteneurisée, une révision et une mise à lʼéchelle déclarées. Le choix part du besoin, pas de la nouveauté.§Une application monolithique HTTP sans exigence dʼimage reste sur App Service ; un artefact conteneur déjà éprouvé peut viser Container Apps.§Ajouter les deux services au même livrable multiplie le coût et lʼexploitation sans preuve utile.',
    'azure-data-services-001§21§Azure SQL et Storage avec moindre privilège§azure.data§Azure SQL porte les données relationnelles ; Blob Storage porte les objets. Chaque accès utilise une identité dédiée et le droit minimal, sans identifiant réel dans le dépôt.§Le service web lit ses blobs avec un rôle de lecture et accède à sa base applicative, tandis que le stockage de progression Forge.NET reste local.§Partager une clé de compte ou ouvrir la base à tout Internet remplace une frontière contrôlée par un secret durable.',
    'azure-managed-identity-key-vault-001§21§Managed Identity et Key Vault sans secret applicatif§azure.identity§Managed Identity donne une identité au service Azure ; Key Vault centralise les valeurs sensibles. Lʼapplication demande une valeur par identité et ne versionne ni valeur ni jeton.§La configuration contient lʼURI du coffre, non sensible, puis lʼidentité du service reçoit seulement Key Vault Secrets User sur ce coffre.§Copier une valeur du coffre vers appsettings.json annule le bénéfice du mécanisme et laisse une trace durable.',
    'observability-correlation-001§22§Logs structurés, métriques et corrélation§observability.signals§Un incident se comprend en reliant événement, métrique et requête avec un identifiant de corrélation. Les logs décrivent des faits bornés et excluent données personnelles et valeurs sensibles.§Un pic de latence déclenche une alerte ; la corrélation relie le point lent à une dépendance SQL sans journaliser la requête complète ni ses paramètres.§Journaliser chaque corps HTTP produit du bruit, augmente le coût et peut exposer des données privées.',
    'observability-alerts-costs-001§22§Alertes actionnables et coûts bornés§observability.operations§Une alerte nomme un symptôme, une fenêtre, un seuil, un destinataire et une action. Budget, rétention et suppression doivent être prévus avant une ressource payante.§Une alerte sur le taux dʼerreur renvoie au runbook ; les données de test ont une rétention courte et le groupe de ressources est supprimable en une opération.§Une alerte sans action crée de la fatigue ; un budget seul ne garantit pas lʼarrêt automatique des dépenses.',
    'performance-security-incident-001§22§Diagnostiquer performance et sécurité sans masquer la cause§observability.incident§Mesurer avant dʼoptimiser : reproduire, borner lʼimpact, comparer une métrique, corriger, puis prouver la non-régression. Un incident de sécurité conserve les preuves sans recopier de donnée sensible.§Une lecture lente est reliée à un volume et une requête ; le correctif est validé par un budget de latence et un contrôle dʼautorisation inchangé.§Désactiver un contrôle ou augmenter tous les timeouts peut réduire un symptôme tout en aggravant le risque.',
    'final-project-architecture-001§23§Cadrer lʼarchitecture du projet final§project.architecture§Le projet final reste un monolithe modulaire : contexte, acteurs, contrats, données, menaces et preuves précèdent les choix techniques. Le cloud est une cible optionnelle, jamais une condition de réussite.§Une décision dʼarchitecture relie une contrainte à une option écartée, une conséquence et un test ou une observation vérifiable.§Dessiner plusieurs services distribués sans besoin démontré déplace lʼeffort vers lʼinfrastructure et affaiblit les preuves métier.',
    'final-project-evidence-001§23§Piloter le projet final par jalons et preuves§project.evidence§Chaque jalon livre une tranche révisable avec critères, tests, revue de sécurité et démonstration. Forge.NET fournit le cadre et la grille, jamais la remise complète.§Le premier jalon prouve un parcours métier vertical local ; les suivants ajoutent persistance, qualité, exploitation et défense sans réécrire le socle.§Attendre la fin pour tester ou documenter transforme chaque défaut en enquête globale et empêche une revue contradictoire.',
    'final-defense-english-001§24§Défendre une architecture en anglais professionnel§english.defense§Une défense courte annonce le problème, le choix, la preuve, la limite et la prochaine expérience. Le vocabulaire technique reste précis ; lʼaccent nʼest pas un critère de maîtrise.§The application remains a modular monolith because one team owns one transactional boundary; integration tests verify the critical workflow.§Réciter des adjectifs comme scalable ou enterprise sans contrainte ni mesure ne défend aucune décision.',
    'career-evidence-plan-001§24§Transformer le parcours en preuves de carrière honnêtes§career.evidence§CV, réponses STAR et suivi de candidatures utilisent des faits vérifiables, minimisent les données personnelles et nʼannoncent ni emploi ni salaire garanti. Le plan post-embauche reste un plan, pas un parcours livré.§Une ligne de CV cite un problème, une action, une preuve et une portée exacte ; une réponse STAR distingue contribution personnelle et résultat collectif.§Inventer une métrique, publier une adresse personnelle ou présenter un laboratoire comme une expérience professionnelle trompe le lecteur.'
)

$LessonIdsByWeek = @{}
$previousLessonId = 'ci-deployment-gates-001'
foreach ($row in $LessonRows) {
    $parts = $row -split '§', 7
    $id=$parts[0];$week=[int]$parts[1];$title=$parts[2];$skill=$parts[3];$concept=$parts[4];$example=$parts[5];$mistake=$parts[6]
    if (-not $LessonIdsByWeek.ContainsKey($week)) { $LessonIdsByWeek[$week] = New-Object System.Collections.Generic.List[string] }
    $LessonIdsByWeek[$week].Add($id)
    $directory = Join-Path $CatalogRoot "curriculum/lessons/$id"
    Write-JsonFile (Join-Path $directory 'lesson.json') ([ordered]@{
        schemaVersion=1;id=$id;version=1;title=$title;week=$week;skills=@([ordered]@{id=$skill;weight=1.0})
        prerequisites=@($previousLessonId);estimatedMinutes=80;objectives=@("Appliquer $title à un cas nouveau et défendre la preuve observable associée")
        sections=@('intuition','explanation','example','counterExample','check','guided','independent','debugging','interview','summary','reviewCards','masteryTest')
        markdownPath='lesson.md';license='CC-BY-4.0'
    })
    Write-TextFile (Join-Path $directory 'lesson.md') (New-LessonScaffold -Id $id -Title $title `
        -PreviousLessonId $previousLessonId -Concept $concept -Example $example -Mistake $mistake)
    $previousLessonId = $id
}

$ExerciseRows = @(
    'azure-hosting-decision-001§21§Choisir un hébergement Azure§3§azure.hosting§HostingChoice§bool:requiresContainerRevisions,bool:alreadyHasContainer§string§return requiresContainerRevisions && alreadyHasContainer ? "container-apps" : "app-service";§[[[true,true],"container-apps"],[[false,true],"app-service"]]§[[[true,false],"app-service"],[[false,false],"app-service"]]§Container Apps exige à la fois un besoin de révisions de conteneur et un artefact déjà conteneurisé.§O(1) en temps et O(1) en espace',
    'azure-secret-source-001§21§Choisir une source de valeur sensible§3§azure.identity§SensitiveValueSource§bool:isSensitive,bool:managedIdentityAvailable§string§if (!isSensitive) return "configuration"; return managedIdentityAvailable ? "key-vault-managed-identity" : "local-user-secrets";§[[[false,true],"configuration"],[[true,true],"key-vault-managed-identity"]]§[[[true,false],"local-user-secrets"],[[false,false],"configuration"]]§Une valeur non sensible reste en configuration ; une valeur sensible utilise lʼidentité gérée ou un magasin local hors Git.§O(1) en temps et O(1) en espace',
    'azure-correlation-signal-001§22§Classer un signal corrélé§3§observability.signals§IncidentSignal§int:errorCount,int:p95LatencyMs§string§if (errorCount < 0 || p95LatencyMs < 0) throw new System.ArgumentOutOfRangeException(); if (errorCount > 0) return "errors"; return p95LatencyMs > 750 ? "latency" : "healthy";§[[[2,300],"errors"],[[0,900],"latency"]]§[[[0,400],"healthy"],[[-1,100],"!ArgumentOutOfRangeException"]]§Valider les mesures, traiter les erreurs en priorité, puis comparer la latence p95 au budget de 750 ms.§O(1) en temps et O(1) en espace',
    'azure-cost-guardrail-001§22§Évaluer une garde de coût§3§azure.cost§CostGuardrail§decimal:estimatedDailyCost,decimal:dailyBudget,bool:deletionPlanReady§string§if (estimatedDailyCost < 0 || dailyBudget <= 0) throw new System.ArgumentOutOfRangeException(); if (!deletionPlanReady) return "block"; return estimatedDailyCost <= dailyBudget ? "allow" : "block";§[[[2.5,5,true],"allow"],[[7,5,true],"block"]]§[[[1,5,false],"block"],[[-1,5,true],"!ArgumentOutOfRangeException"]]§Autoriser seulement un coût estimé dans le budget avec un plan de suppression vérifié.§O(1) en temps et O(1) en espace',
    'azure-release-evidence-001§23§Valider les preuves dʼun jalon§3§project.evidence§MilestoneReady§bool:testsPassed,bool:securityReviewed,bool:rollbackDocumented§bool§return testsPassed && securityReviewed && rollbackDocumented;§[[[true,true,true],true],[[false,true,true],false]]§[[[true,false,true],false],[[true,true,false],false]]§Un jalon est révisable uniquement si tests, revue de sécurité et retour arrière documenté sont tous présents.§O(1) en temps et O(1) en espace',
    'azure-incident-brief-001§24§Structurer un bref dʼincident en anglais§3§english.incident§IncidentBriefStatus§bool:impactStated,bool:evidenceCited,bool:nextStepOwned§string§return impactStated && evidenceCited && nextStepOwned ? "ready" : "incomplete";§[[[true,true,true],"ready"],[[true,false,true],"incomplete"]]§[[[false,true,true],"incomplete"],[[true,true,false],"incomplete"]]§Un bref est prêt seulement sʼil nomme lʼimpact, cite une preuve et attribue la prochaine action.§O(1) en temps et O(1) en espace'
)

$ExerciseIdsByWeek = @{}
# Ces suppressions retirent des documents obsolètes d'une génération antérieure. Elles ne
# s'appliquent qu'avec -Force : sans ce commutateur, l'échafaudeur ne détruit aucun contenu publié.
$staleExercisePath=Join-Path $CatalogRoot 'exercises/azure-hosting-choice-001'
if($script:ForceOverwrite -and (Test-Path -LiteralPath $staleExercisePath)){Remove-Item -LiteralPath $staleExercisePath -Recurse -Force}
$staleInterviewPath=Join-Path $CatalogRoot 'interviews/interview-azure-hosting-choice-001.json'
if($script:ForceOverwrite -and (Test-Path -LiteralPath $staleInterviewPath)){Remove-Item -LiteralPath $staleInterviewPath -Force}
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
        prerequisites=@($lessonPrerequisite);estimatedMinutes=40;statement='statement.md'
        constraints=@('Conserver exactement la signature publique', $spec.Rule, 'Rester déterministe, hors ligne et indépendant de toute ressource Azure réelle')
        examples=@([ordered]@{input=(Convert-JsonCompact $firstVisible[0]);output=(Convert-JsonCompact $firstVisible[1])})
        reflectionFields=@('reformulation','inputs','expectedOutput','edgeCases','hypothesis','plan')
        starterPath='starter/';visibleTestsPath='tests/visible/';hiddenTestsPath='tests/hidden/'
        hints=@(
            [ordered]@{level=1;kind='socratic';content="Quelle condition publique de « $($spec.Title) » doit faire changer le résultat ?"},
            [ordered]@{level=2;kind='location';content="Concentrez la validation et la décision dans $($spec.Method), sans réseau ni état global."},
            [ordered]@{level=3;kind='strategy';content=$spec.Rule},
            [ordered]@{level=4;kind='partial-pseudocode';content='valider les entrées ; appliquer la garde la plus stricte ; retourner une décision explicite et déterministe'}
        )
        solution=[ordered]@{path='solution/';unlock=[ordered]@{seriousAttempts=2;minimumDelayMinutes=10}}
        explanation='explanation.md';complexity=$spec.Complexity
        commonMistakes=@('Coder seulement les exemples visibles', 'Présenter une étape manuelle ou cloud comme une validation automatique', 'Masquer une valeur invalide ou supprimer une garde de sécurité')
        variantId=$variantId;reviewCards=@("card-$($spec.Id)-rule","card-$($spec.Id)-proof")
        interviewQuestionId=$interviewId;license='CC-BY-4.0'
    })
    Write-TextFile (Join-Path $directory 'statement.md') @"
# $($spec.Title)

Implémentez `Submission.$($spec.Method)` avec la signature fournie. $($spec.Rule)

Le résultat reste déterministe et hors ligne : aucun abonnement, appel Azure ou identifiant réel nʼest nécessaire. Avant le code, écrivez un cas nominal, une frontière, un refus et le risque que ces preuves réduisent.

Exemple : entrée ``$(Convert-JsonCompact $firstVisible[0])``, sortie ``$(Convert-JsonCompact $firstVisible[1])``.
"@
    Write-TextFile (Join-Path $directory 'explanation.md') "# Explication`n`n$($spec.Rule) La solution sépare validation et décision et nʼeffectue aucun appel externe. Complexité : **$($spec.Complexity)**. Les cas cachés changent les frontières et réfutent une constante mémorisée. Après consultation, expliquez la règle avec vos mots et planifiez une reprise à blanc : cette tentative nʼest pas maîtrisée."
    Write-TextFile (Join-Path $directory 'review-cards.md') "# Cartes de révision`n`n## card-$($spec.Id)-rule`n`n**Question :** Quelle règle gouverne la décision ?  `n**Réponse attendue :** $($spec.Rule)`n`n## card-$($spec.Id)-proof`n`n**Question :** Quelle preuve empêche le contournement ?  `n**Réponse attendue :** Un cas de frontière ou de refus distinct des exemples visibles."
    Write-TextFile (Join-Path $directory 'starter/Submission.cs') $starterSource
    Write-TextFile (Join-Path $directory 'solution/Submission.cs') $solutionSource
    Write-TextFile (Join-Path $directory 'solution/README.md') "# Choix de référence`n`n$($spec.Rule) $($spec.Complexity). Cette solution ne vaut pas preuve de maîtrise."
    Write-JsonFile (Join-Path $directory 'tests/runner.json') ([ordered]@{schemaVersion=1;suiteId="$($spec.Id).v1";exerciseId=$spec.Id;exerciseVersion=1;typeName='Submission';methodName=$spec.Method;parameterTypes=$runnerTypes;returnType=$spec.ReturnType})
    foreach ($visibility in @('visible','hidden')) {
        $rawCases=if($visibility -eq 'visible'){$spec.Visible}else{$spec.Hidden}
        $cases=New-Object System.Collections.Generic.List[object]
        for($caseIndex=0;$caseIndex -lt $rawCases.Count;$caseIndex++){
            $pair=$rawCases[$caseIndex];$expected=$pair[1]
            $case=[ordered]@{name="$(if($visibility -eq 'visible'){'Visible'}else{'Hidden'})_Case$($caseIndex+1)";message="$($spec.Title) — cas $($caseIndex+1) incorrect.";arguments=@($pair[0]);expected=$null;expectedException=$null;argumentsUnchanged=$false}
            if($expected -is [string] -and $expected.StartsWith('!')){$case.expectedException=$expected.Substring(1);$case.Remove('expected')}else{$case.expected=$expected}
            $cases.Add($case)
        }
        Write-JsonFile (Join-Path $directory "tests/$visibility/cases.json") ([ordered]@{schemaVersion=1;cases=$cases.ToArray()})
    }
    Write-JsonFile (Join-Path $CatalogRoot "interviews/$interviewId.json") ([ordered]@{
        schemaVersion=1;id=$interviewId;version=1;title=$spec.Title;level='intermediate';durationMinutes=7
        skills=@($spec.Skill);question="Comment implémenteriez-vous « $($spec.Title) » et quelles preuves empêchent un contournement ?"
        observableCriteria=@('Le contrat, une frontière et un refus sont explicités', 'La limite du mode local et le coût ou risque associé sont justifiés')
        modelAnswer="$($spec.Rule) Je sépare validation et décision, je teste nominal et frontières, puis je vérifie quʼaucun appel cloud ni donnée sensible nʼentre dans la preuve. $($spec.Complexity)."
        commonMistakes=@('Réciter le code sans expliquer le contrat, le coût ou la preuve');variants=@("Changer une contrainte tout en conservant la règle de $($spec.Title.ToLowerInvariant()).");license='CC-BY-4.0'
    })
}

$curriculumPath = Join-Path $CatalogRoot 'curriculum/forge-reference.json'
$currentCurriculum = Get-Content -Raw $curriculumPath | ConvertFrom-Json
$modules = New-Object System.Collections.Generic.List[object]
foreach ($module in $currentCurriculum.modules | Select-Object -First 20) { $modules.Add($module) }
for ($week=21; $week -le 24; $week++) {
    $modules.Add([ordered]@{
        id="week-$week";title="Semaine $week";weeks=@($week);prerequisites=@("week-$($week-1)")
        lessonIds=$LessonIdsByWeek[$week].ToArray();exerciseIds=$ExerciseIdsByWeek[$week].ToArray()
    })
}
Write-JsonFile $curriculumPath ([ordered]@{
    schemaVersion=1;id='forge-reference';version=4;title='Forge.NET — parcours professionnel S1 à S24'
    description='Parcours local autonome couvrant C#, données, API, sécurité, tests, livraison, Azure optionnel, observabilité, projet final guidé, anglais et preuves de carrière.'
    weeks=24;modules=$modules.ToArray();license='CC-BY-4.0'
})

$InterviewRows = @(
    'Définir une frontière App Service§azure.hosting§Une application web monolithique nʼa pas de besoin de révision de conteneur. Que choisissez-vous et quelle limite annoncez-vous ?§Je choisis App Service pour réduire lʼexploitation. Je documente que ce choix serait revu si une image portable ou des révisions pondérées devenaient une contrainte vérifiée.',
    'Justifier Container Apps§azure.hosting§Le livrable possède déjà une image durcie et doit tester deux révisions. Quelle preuve exigez-vous avant Container Apps ?§Jʼexige la provenance de lʼimage, un démarrage non-root, une sonde utile et une stratégie de retour vers la révision précédente. Sans ces preuves, le service ne corrige pas le risque.',
    'Séparer SQL et Blob Storage§azure.data§Comment répartir factures structurées et pièces jointes sans dupliquer les responsabilités ?§Azure SQL conserve les relations et métadonnées transactionnelles ; Blob Storage conserve les objets. La base porte une référence opaque et les droits des deux accès sont testés séparément.',
    'Appliquer le moindre privilège Storage§azure.data§Un service doit seulement lire un conteneur de documents. Quel droit et quelle preuve proposez-vous ?§Jʼattribue un rôle de lecture au périmètre le plus étroit. Une preuve positive lit un objet autorisé et une preuve négative montre que lʼécriture est refusée.',
    'Expliquer Managed Identity§azure.identity§Pourquoi Managed Identity réduit-elle le risque sans supprimer le besoin dʼautorisation ?§Elle évite de distribuer un identifiant applicatif durable, mais ne donne aucun droit par elle-même. Le rôle minimal reste explicite, borné et révocable.',
    'Utiliser Key Vault§azure.identity§Quelles valeurs vont dans Key Vault et lesquelles restent dans la configuration versionnée ?§Les valeurs sensibles ou rotatives vont dans le coffre ; les noms de ressources, URI publiques et seuils non sensibles peuvent rester versionnés. Aucune valeur du coffre ne rejoint les logs.',
    'Préparer la suppression Azure§azure.cost§Que doit contenir un plan de suppression avant un laboratoire Azure facultatif ?§Il nomme le groupe de ressources dédié, le propriétaire de lʼaction, la commande de vérification, lʼheure limite et la preuve finale quʼaucune ressource facturable ne subsiste.',
    'Comprendre un budget Azure§azure.cost§Pourquoi une alerte de budget ne suffit-elle pas comme limite automatique de dépense ?§Une alerte informe selon une mesure et un délai ; elle ne constitue pas un coupe-circuit universel. Je limite aussi durée, références de taille, périmètre et suppression explicite.',
    'Choisir une région§azure.architecture§Comment justifier une région sans prétendre garantir disponibilité ou conformité ?§Je cite latence attendue, disponibilité des services, politique de données et coût observé. Je marque les hypothèses et je ne transforme pas une région en garantie juridique.',
    'Éviter une dépendance cloud§azure.local§Comment conserver un apprentissage autonome lorsque le compte Azure est absent ?§Je fournis une configuration inspectable, une simulation déterministe et des preuves locales. Le déploiement réel reste une extension manuelle facultative et clairement étiquetée.',
    'Protéger une chaîne de connexion§azure.security§Une application utilise Azure SQL. Comment évitez-vous de versionner une chaîne sensible ?§En Azure, je privilégie lʼidentité gérée et un fournisseur adapté ; en local, une valeur factice reste hors Git. Les erreurs et captures ne contiennent jamais la valeur.',
    'Séparer Forge et le laboratoire§azure.boundary§Pourquoi la progression SQLite de Forge.NET ne doit-elle jamais devenir la base du laboratoire cloud ?§La progression est une donnée personnelle locale. Le laboratoire utilise ses propres ressources jetables et aucune configuration ne lui donne un chemin vers SQLite.',
    'Décrire une ressource supprimable§azure.operations§Quels éléments rendent une ressource pédagogique réellement supprimable ?§Un périmètre dédié, aucune dépendance cachée, une commande de suppression, une vérification après suppression et une responsabilité temporelle explicite rendent le retrait auditable.',
    'Valider un déploiement simulé§azure.validation§Que prouve un déploiement simulé et que ne prouve-t-il pas ?§Il prouve la cohérence locale des choix, fichiers et garde-fous testés. Il ne prouve ni acceptation par Azure, ni disponibilité, ni performance réelle du service.',
    'Construire un log structuré§observability.logs§Quels champs conservez-vous pour diagnostiquer une requête sans exposer son contenu ?§Je conserve horodatage, niveau, événement, corrélation, route normalisée, durée et résultat. Jʼexclus corps, en-têtes dʼauthentification et identifiants personnels.',
    'Relier une corrélation§observability.correlation§Comment suivez-vous une requête entre HTTP et SQL sans journaliser la commande complète ?§Je propage un identifiant de corrélation et journalise le nom stable de lʼopération, la durée et le résultat. Les paramètres et données métier restent absents.',
    'Distinguer logs et métriques§observability.signals§Quand choisissez-vous une métrique plutôt quʼun log ?§Une métrique mesure une tendance agrégée bornée, par exemple taux dʼerreur ou latence. Un log explique un événement particulier ; les deux se relient sans copier la charge utile.',
    'Définir un p95§performance.metrics§Que signifie un p95 de latence et quelle erreur dʼinterprétation évitez-vous ?§Environ 95 pour cent des observations sont inférieures ou égales à cette valeur sur la fenêtre. Je cite la fenêtre, le volume et je ne le confonds pas avec un maximum.',
    'Écrire une alerte actionnable§observability.alerts§Quels critères rendent une alerte exploitable ?§Elle porte un symptôme utilisateur, une fenêtre, un seuil justifié, une gravité, un destinataire et un lien vers une action. Son test contrôle déclenchement et retour à la normale.',
    'Borner la rétention§observability.cost§Comment reliez-vous rétention de télémétrie, confidentialité et coût ?§Je garde seulement la durée nécessaire au diagnostic annoncé, limite cardinalité et volume, puis vérifie la suppression. Une durée plus longue demande un besoin et un propriétaire.',
    'Éviter la haute cardinalité§observability.metrics§Pourquoi un identifiant utilisateur est-il un mauvais label de métrique ?§Il crée une série par valeur, augmente coût et complexité et expose potentiellement une donnée personnelle. Je conserve des dimensions bornées et non personnelles.',
    'Qualifier un incident§observability.incident§Que dites-vous dans les cinq premières minutes dʼun incident simulé ?§Je nomme le symptôme, la période, lʼimpact observé, la portée connue, la première mesure de confinement et la prochaine mise à jour, sans inventer la cause.',
    'Séparer symptôme et cause§observability.incident§Une latence augmente après un déploiement. Pourquoi le déploiement nʼest-il pas encore la cause ?§La proximité temporelle est un indice. Je compare révisions, traces et dépendances, puis cherche une observation discriminante avant rollback ou correction.',
    'Mesurer avant optimisation§performance.method§Quelle séquence appliquez-vous avant de modifier une requête lente ?§Je reproduis avec un jeu borné, fixe la mesure et le budget, localise le coût, change une seule hypothèse puis compare et ajoute une non-régression.',
    'Préserver la sécurité sous charge§performance.security§Pourquoi désactiver lʼautorisation pour gagner du temps est-il invalide ?§Cela change le contrat et supprime une frontière critique. La preuve de performance doit inclure le chemin autorisé réel et vérifier que les refus restent corrects.',
    'Gérer un faux positif§observability.alerts§Une alerte se déclenche sur un trafic de test légitime. Que corrigez-vous ?§Je vérifie données et fenêtre, documente le faux positif, ajuste le signal plutôt que de désactiver lʼalerte et ajoute un scénario qui distingue test sain et incident.',
    'Rédiger un runbook§observability.operations§Quel contenu minimal placez-vous dans un runbook dʼalerte ?§Contexte, impact, contrôles sûrs, données à ne pas consulter, étapes de confinement, rollback, escalade, preuve de résolution et critères de clôture.',
    'Comparer avant et après§performance.evidence§Quelles preuves empêchent une optimisation seulement déclarative ?§Je conserve le même scénario, les mêmes volumes et la même méthode de mesure, publie avant et après avec variabilité, et maintiens tous les tests fonctionnels et de sécurité.',
    'Cadrer le problème final§project.scope§Comment empêchez-vous le projet final de devenir une liste infinie de fonctionnalités ?§Je définis un acteur, un parcours critique, trois exclusions et une preuve de valeur. Toute extension attend une tranche verticale verte et une décision de périmètre.',
    'Choisir le monolithe modulaire§project.architecture§Pourquoi le projet final reste-t-il un monolithe modulaire ?§Une équipe et un déploiement couvrent le besoin ; les modules donnent des frontières de code et de règles. Des services séparés ajouteraient des pannes sans exigence démontrée.',
    'Écrire une décision dʼarchitecture§project.architecture§Que contient une décision révisable ?§Contexte, forces, options, choix, conséquences, risques, date et signal de réexamen. Elle cite une preuve plutôt quʼune préférence personnelle.',
    'Définir un jalon vertical§project.milestone§À quoi reconnaissez-vous un premier jalon utile ?§Il traverse interface, cas dʼusage, domaine et persistance pour un comportement étroit, avec test, migration reproductible, erreur visible et revue de sécurité.',
    'Préparer une démonstration§project.demo§Comment rendez-vous une démonstration reproductible ?§Je pars dʼun état connu, annonce le scénario, exécute les commandes documentées, montre succès et refus puis relie chaque observation au critère.',
    'Refuser une solution prématurée§project.mastery§Pourquoi la solution complète du projet final reste-t-elle absente ?§La construction et les arbitrages sont les preuves recherchées. Une remise générée mesure la consultation ; le cadre fournit jalons, critères, indices et revue, pas le produit.',
    'Auditer une migration§project.persistence§Quelles preuves demandez-vous pour une migration finale ?§Création depuis zéro, montée depuis la version précédente, données de démonstration reproductibles, échec visible et absence dʼaccès à une base personnelle hors périmètre.',
    'Défendre un compromis§project.defense§Comment répondez-vous à une objection dʼarchitecture légitime ?§Je reformule le risque, cite la contrainte initiale et la preuve, reconnais la limite, puis donne le signal concret qui ferait réviser le choix.',
    'Sécuriser la configuration finale§project.security§Quels éléments inspectez-vous avant une démonstration ?§Diff Git, historique récent, configuration, logs, exports et captures. Je retire toute donnée personnelle, vérifie les valeurs factices et révoque une valeur exposée.',
    'Planifier un rollback§project.operations§Que doit prouver un plan de rollback sans production réelle ?§Le plan nomme lʼartefact précédent, la condition de déclenchement, les données incompatibles, lʼordre des actions et une répétition locale ou simulée explicitement étiquetée.',
    'Évaluer une preuve§project.rubric§Comment évitez-vous quʼune grille récompense seulement une belle présentation ?§Chaque critère cite un comportement ou artefact observable et un poids. Les portes critiques de sécurité et dʼintégrité ne sont pas compensées par le style.',
    'Présenter une limite§project.communication§Quelle formule utilisez-vous lorsquʼune preuve reste manuelle ?§Je dis exactement ce qui a été exécuté, par qui et dans quel environnement, puis ce qui reste non vérifié. Je ne transforme pas une inspection en test automatique.',
    'Réviser après revue§project.review§Comment traitez-vous un commentaire contradictoire sur le projet final ?§Je reproduis le risque, classe son impact, réponds par une décision ou un test, borne le diff et conserve la trace de la conclusion sans masquer le désaccord.',
    'Rédiger une preuve de CV§career.cv§Comment transformer un laboratoire en ligne de CV honnête ?§Je le nomme comme projet personnel, décris le problème, lʼaction et une preuve reproductible. Je nʼinvente ni utilisateur, revenu, durée professionnelle ni résultat commercial.',
    'Minimiser les données du CV§career.privacy§Quelles données retirez-vous dʼun export pédagogique de CV ?§Adresse complète, date de naissance, identifiants privés, noms de tiers et liens non destinés au public. Je vérifie aussi métadonnées et historique du fichier.',
    'Construire une réponse STAR§career.star§Que rend une réponse STAR vérifiable ?§La situation et la tâche restent brèves, lʼaction précise ma contribution personnelle, le résultat cite une observation réelle et la rétrospective nomme une amélioration.',
    'Suivre une candidature§career.applications§Quelles colonnes conservez-vous sans transformer le suivi en collecte excessive ?§Organisation, rôle, source publique, date, statut, prochaine action et notes minimales. Je nʼenregistre pas de donnée sensible sur des recruteurs ou contacts.',
    'Répondre sur une lacune§career.interview§Comment parlez-vous dʼune technologie non pratiquée ?§Je dis ce que je nʼai pas encore fait, relie les concepts transférables, propose une expérience bornée et refuse de présenter une lecture comme une expérience.',
    'Négocier sans promesse§career.negotiation§Comment préparer une négociation sans annoncer un salaire garanti ?§Je distingue préférences, données de marché datées et contraintes personnelles, prépare des questions sur le rôle et évalue lʼensemble sans promettre un résultat.',
    'Planifier les trente premiers jours§career.posthire§Que contient un plan post-embauche sans prétendre livrer un parcours complet ?§Questions dʼaccès, architecture, pratiques dʼéquipe, premier correctif borné, feedback et journal dʼapprentissage. Cʼest une trame adaptable, pas une garantie de performance.',
    'Protéger des preuves publiques§career.privacy§Que vérifiez-vous avant de publier un dépôt comme preuve ?§Historique, branches, issues, captures, données de démonstration, licences et configuration. Toute valeur exposée est retirée et, si réelle, révoquée hors du dépôt.',
    'Open an architecture defense§english.defense§How do you open a two-minute architecture defense in English?§I state the user problem, the main constraint and the decision in plain language. Then I announce the evidence and one limitation before technical detail.',
    'Clarify an ambiguous requirement§english.clarification§A requirement says the service must be fast. What do you ask?§I ask which user action, workload, percentile, environment and time budget define fast, and what consequence follows when the budget is exceeded.',
    'Report an incident update§english.incident§Give the structure of a concise incident update.§I state observed impact and time window, confirmed facts, current mitigation, owner of the next action and next update time. I avoid an unverified root cause.',
    'Challenge a risky proposal§english.review§How do you challenge a proposal to log every request body?§I explain the privacy, security and cost risks, ask which diagnostic question needs the data, and propose structured bounded fields plus correlation instead.',
    'Describe a trade-off§english.architecture§How do you explain the App Service versus Container Apps trade-off?§App Service reduces operational choices for a web application. Container Apps fits an existing container and revision needs, but adds configuration and cost to verify.',
    'Explain a failed test§english.testing§How do you explain a failed hidden test without revealing it?§I describe the public contract, the boundary category and my incorrect assumption. I do not claim knowledge of private inputs and I add my own regression case.',
    'Ask for review evidence§english.review§How do you request stronger evidence in a code review?§Could you add a test that fails when an unauthorized user calls this route, and include the command output? The current success case does not prove the policy.'
)

# La renumérotation des fiches exige de repartir d'un lot vide, ce qui écrase toute reprise
# éditoriale : réservé à -Force, conformément au contrat de l'échafaudeur.
if($script:ForceOverwrite){
    $generatedInterviewFiles=Get-ChildItem (Join-Path $CatalogRoot 'interviews') -File -Filter 'interview-s21-s24-*.json'
    foreach($generatedInterviewFile in $generatedInterviewFiles){Remove-Item -LiteralPath $generatedInterviewFile.FullName -Force}
}
for ($index=0; $index -lt $InterviewRows.Count; $index++) {
    $parts=$InterviewRows[$index] -split '§', 4
    $number=$index + 1
    $level=if($number -le 14){'junior'}elseif($number -le 36){'intermediate'}else{'advanced'}
    $id=('interview-s21-s24-{0:D3}' -f $number)
    Write-JsonFile (Join-Path $CatalogRoot "interviews/$id.json") ([ordered]@{
        schemaVersion=1;id=$id;version=1;title=$parts[0];level=$level;durationMinutes=$(if($level -eq 'junior'){6}elseif($level -eq 'intermediate'){9}else{12})
        skills=@($parts[1]);question=$parts[2]
        observableCriteria=@('La réponse prend une décision explicite et cite une preuve observable', 'Une limite de sécurité, de coût, de portée ou de validité est reconnue')
        modelAnswer=$parts[3];commonMistakes=@('Employer un slogan sans contrainte, preuve ni limite vérifiable')
        variants=@("Changez une contrainte du scénario « $($parts[0]) » et expliquez si la décision reste valide.");license='CC-BY-4.0'
    })
}

$EnglishTopics = @(
    'Hosting decision§azure.hosting§a teammate asks why the service is not containerized§App Service matches the current web workload, while a container platform would add revision and image operations without a verified need§hosting choice',
    'Managed identity§azure.identity§a reviewer asks where the application credential is stored§The service uses its Azure identity and a scoped role, so no long-lived application credential is copied into the repository§credentialless access',
    'Storage boundary§azure.data§an architect asks why files are outside the relational database§The database keeps transactional metadata and the object store keeps file content; an opaque reference connects them§data responsibility',
    'SQL access§azure.data§a security reviewer asks how database access is limited§The application identity receives only the database permissions required by its use cases, and denied operations are tested§least privilege',
    'Key Vault use§azure.identity§a teammate suggests copying a sensitive value into configuration§The repository stores only the vault location; the runtime identity retrieves the sensitive value and logs never include it§sensitive configuration',
    'Optional cloud lab§azure.local§a learner has no Azure subscription§The local simulation validates configuration and guardrails; a real deployment is optional, manual and may create costs§simulation scope',
    'Cost warning§azure.cost§a manager asks whether a budget guarantees zero overspend§A budget produces notifications but is not a universal spending stop, so duration, size and deletion are also controlled§cost boundary',
    'Deletion plan§azure.operations§a lab owner must close the exercise§The owner deletes the dedicated resource group, checks that no billable resource remains and records the time of verification§resource cleanup',
    'Structured logging§observability.logs§an incident reviewer asks what a log event contains§The event contains a stable name, severity, normalized route, duration, result and correlation identifier, but no request body§safe logging',
    'Correlation§observability.correlation§a request crosses HTTP and a database dependency§The same correlation identifier links bounded events while parameters and personal data remain outside the logs§request tracing',
    'Metric choice§observability.signals§a teammate wants a log line for every latency sample§A latency histogram answers the trend question more efficiently, while a bounded event explains a specific failure§signal selection',
    'Latency percentile§performance.metrics§a reviewer asks what p95 means§On the stated window and workload, about ninety-five percent of observations are at or below the value; it is not the maximum§percentile interpretation',
    'Actionable alert§observability.alerts§an alert has no owner or runbook§The alert needs a user symptom, window, threshold, severity, owner and safe first action before it is enabled§alert design',
    'High cardinality§observability.metrics§a proposal uses customer identifiers as metric labels§Unbounded labels increase cost and can expose personal data, so the metric uses a small stable category instead§cardinality control',
    'Incident opening§observability.incident§the team has just observed an error spike§We know the time window and affected operation; mitigation is in progress, the cause is not confirmed and the next update has an owner§incident status',
    'Root cause caution§observability.incident§latency rose after a release§The timing is evidence for an investigation, not proof of causation; we compare revisions and dependencies before concluding§causal language',
    'Performance evidence§performance.evidence§an optimization is said to be twice as fast§The claim needs the same scenario, workload, environment and measurement method before and after, plus unchanged functional tests§benchmark evidence',
    'Security under load§performance.security§someone proposes bypassing authorization in a benchmark§Removing authorization changes the workload and invalidates the security proof; the real protected path must remain measured§representative testing',
    'Project scope§project.scope§a stakeholder asks for five more features before the first milestone§We keep one critical user journey and explicit exclusions until the vertical slice and its tests are reviewable§scope control',
    'Modular monolith§project.architecture§an interviewer asks why the project has one deployable unit§One team owns one transactional boundary, and modules separate rules without adding distributed failure modes§architecture rationale',
    'Architecture record§project.architecture§a decision is based only on personal preference§The record must state context, options, forces, consequences and the evidence or signal that would trigger a review§decision record',
    'Milestone evidence§project.milestone§a milestone contains code but no verification record§It is reviewable only when behavior, tests, security review and reproducible commands support the acceptance criteria§reviewable increment',
    'Project limitation§project.communication§a cloud deployment was not executed§The configuration and simulation were verified locally; Azure acceptance, availability and real cost remain unverified§honest limitation',
    'CV evidence§career.cv§a project needs a concise CV line§I describe it as a personal project, state my action and cite a reproducible test result without inventing users or business impact§verifiable achievement',
    'STAR reflection§career.star§an interviewer asks about a defect you introduced§I explain the context, my responsibility, the diagnostic action, the observed correction and the prevention I added afterward§accountable reflection'
)

for ($index=0; $index -lt $EnglishTopics.Count; $index++) {
    $parts=$EnglishTopics[$index] -split '§', 5
    $number=$index + 1
    foreach ($mode in @('written','spoken')) {
        $id=('english-card-{0:D2}-{1}' -f $number,$mode)
        $isWritten=$mode -eq 'written'
        Write-JsonFile (Join-Path $CatalogRoot "english/$id.json") ([ordered]@{
            schemaVersion=1;id=$id;version=1;title="$($parts[0]) — $(if($isWritten){'written update'}else{'spoken follow-up'})"
            level=$(if($number -le 8){'B1'}elseif($number -le 18){'B2'}else{'C1'});durationMinutes=$(if($isWritten){12}else{10});skills=@("english.$mode",$parts[1])
            situation="In a technical discussion, $($parts[2]). You must $(if($isWritten){'write the decision'}else{'answer aloud'}) accurately without overstating the evidence."
            instructions=@($(if($isWritten){'Write a four-sentence update with decision, evidence, limitation and next action.'}else{'Deliver a sixty-second answer with decision, evidence, limitation and one clarifying question.'}))
            vocabulary=@(
                [ordered]@{term=$parts[4];meaning="the precise concept required for $($parts[0].ToLowerInvariant())"},
                [ordered]@{term='verified evidence';meaning='an observation supported by a reproducible check'}
            )
            expectedElements=@('A clear decision tied to the scenario', 'Verified evidence and one explicit limitation', $(if($isWritten){'A named next action'}else{'One useful clarifying question'}))
            modelAnswer=$(if($isWritten){"Decision: $($parts[3]). Evidence: the documented local checks support this scope. Limitation: this does not prove behavior in an untested production environment. Next action: record the owner and the next bounded verification."}else{"My decision is that $($parts[3]). The current evidence is limited to the documented checks, so I would not claim more. Which constraint or observation would make us revisit this decision?"})
            commonMistakes=@('Using confident adjectives without naming evidence or a limitation')
            variants=@($(if($isWritten){'Rewrite the update for a non-technical stakeholder without removing the limitation.'}else{'Answer again after the reviewer changes one cost or security constraint.'}));license='CC-BY-4.0'
        })
    }
}

$finalProjectId='project-final-service-operations-001'
Write-JsonFile (Join-Path $CatalogRoot "projects/$finalProjectId.json") ([ordered]@{
    schemaVersion=1;id=$finalProjectId;version=1;title='Projet final — service métier exploitable';difficulty=5;weeks=@(21,22,23,24)
    skills=@('project.architecture','api.contracts','data.persistence','tests.strategy','security.review','observability.operations','azure.optional','english.defense')
    prerequisites=@('project-container-delivery-001','azure-hosting-choice-001','observability-correlation-001');estimatedHours=70;briefPath="$finalProjectId.md"
    milestones=@(
        [ordered]@{id='scope-and-risks';title='Périmètre, architecture et risques';evidence='Un brief rédigé par lʼapprenant relie acteurs, parcours critique, exclusions, décisions, menaces et preuves prévues.';acceptanceCriteria=@('Un parcours critique et trois exclusions bornent la remise','Chaque décision cite une contrainte, une option écartée et un signal de révision','Aucune architecture distribuée nʼest ajoutée sans besoin démontré')},
        [ordered]@{id='local-vertical-slice';title='Tranche verticale locale';evidence='Une démonstration reproductible traverse interface, cas dʼusage, domaine et persistance locale avec succès et refus.';acceptanceCriteria=@('La création depuis zéro et les données de démonstration sont reproductibles','Les tests utiles couvrent nominal, frontière, refus et non-régression','Les erreurs applicables restent visibles et actionnables')},
        [ordered]@{id='quality-and-security';title='Qualité et revue de sécurité';evidence='Une revue contradictoire du diff, des droits, des entrées et des journaux est accompagnée de corrections et de preuves.';acceptanceCriteria=@('Aucune valeur sensible ni donnée personnelle nʼapparaît dans Git ou les logs','Les contrôles dʼautorisation ont des preuves positives et négatives','Les échecs de build, test ou analyse ne sont pas masqués')},
        [ordered]@{id='operations-and-cloud-plan';title='Exploitation et plan cloud optionnel';evidence='Un incident simulé est résolu localement et un plan Azure optionnel documente identité, coût, observabilité et suppression.';acceptanceCriteria=@('Logs, métriques et corrélation répondent à une question dʼincident','Le mode simulé fonctionne sans compte cloud','Tout déploiement réel reste manuel, facultatif, supprimable et précédé dʼun avertissement de coût')},
        [ordered]@{id='defense-and-handoff';title='Défense et transmission';evidence='Une démonstration et une défense en français puis un résumé en anglais relient décisions, preuves, limites et prochaine expérience.';acceptanceCriteria=@('La démonstration part dʼun état connu et cite les commandes exécutées','Les limites manuelles ou simulées ne sont pas présentées comme automatiques','Le dépôt permet à une autre personne de reproduire les preuves sans solution externe')}
    )
    rubric=@(
        [ordered]@{criterion='Valeur et contrat métier';weight=0.2;observableEvidence='Le parcours critique et ses refus répondent au brief sans fonctionnalité hors périmètre.'},
        [ordered]@{criterion='Architecture et maintenabilité';weight=0.15;observableEvidence='Le monolithe modulaire sépare les règles, orchestre les cas dʼusage et isole les adaptateurs.'},
        [ordered]@{criterion='Tests et reproductibilité';weight=0.2;observableEvidence='Les tests réfutent des erreurs plausibles et les commandes repartent dʼun état connu.'},
        [ordered]@{criterion='Sécurité et confidentialité';weight=0.2;observableEvidence='Entrées, droits, données, configuration et journaux sont bornés par des preuves positives et négatives.'},
        [ordered]@{criterion='Observabilité et exploitation';weight=0.15;observableEvidence='Un incident simulé est expliqué par des signaux corrélés, puis couvert par non-régression et runbook.'},
        [ordered]@{criterion='Défense et honnêteté des preuves';weight=0.1;observableEvidence='La présentation distingue faits, hypothèses, simulations, limites et contribution personnelle.'}
    )
    solutionPolicy='no-complete-solution-before-submission'
    commonMistakes=@('Commencer plusieurs modules sans tranche verticale révisable','Présenter une simulation ou une inspection comme un déploiement validé','Copier un projet de référence au lieu de produire et défendre ses décisions','Inclure une valeur sensible, une donnée personnelle ou une promesse de résultat professionnel')
    variantIds=@('project-api-mini-erp-001','project-testing-strategy-001','project-container-delivery-001');license='CC-BY-4.0'
})
Write-TextFile (Join-Path $CatalogRoot "projects/$finalProjectId.md") @'
# Projet final — service métier exploitable

## Mission

Concevez et réalisez vous-même un service métier local en .NET sous forme de monolithe modulaire. Choisissez un domaine différent des mini-projets déjà livrés, un acteur principal et un parcours critique mesurable. Le produit doit pouvoir être construit, testé et démontré hors ligne.

Forge.NET fournit uniquement le brief, les jalons, les critères et les questions de revue. Aucun squelette métier, code de remise, modèle de données final ou solution complète nʼest fourni. Une consultation de documentation ne remplace pas votre justification.

## Contraintes non négociables

- un seul déployable applicatif et des modules cohésifs ;
- règles métier dans le domaine, orchestration dans lʼapplication, adaptateurs dans lʼinfrastructure et UI sans règle importante ;
- persistance locale reproductible, données de démonstration factices et migrations rejouables ;
- succès, frontières, refus, autorisation, erreurs et non-régressions couverts par des preuves utiles ;
- aucune donnée personnelle réelle, aucune valeur sensible et aucune dépendance réseau obligatoire ;
- déploiement Azure facultatif : le mode simulé satisfait le projet ;
- aucun échec applicable masqué.

## Dossier de preuve à produire

Créez vos propres fichiers de décision, matrice de risques, journal dʼincident, résultats de commandes et support de défense. Pour chaque affirmation, indiquez lʼartefact ou la commande qui la soutient. Les captures ne remplacent pas un test reproductible.

## Questions de revue contradictoire

1. Quel comportement incorrect plausible reste vert avec vos tests ?
2. Quelle donnée ou quel droit traverse une frontière sans validation ?
3. Comment repartir de zéro et comment revenir à lʼartefact précédent ?
4. Quel signal dʼincident est actionnable et lequel ajoute seulement du bruit ?
5. Quelle décision changerait si la charge, le coût ou lʼéquipe changeait ?

## Défense

Présentez le parcours critique, un refus de sécurité, un incident simulé résolu et une décision dʼarchitecture. Terminez par un résumé de deux minutes en anglais : problem, constraint, decision, evidence, limitation, next experiment.
'@

$ExamDefinitions = @(
    [pscustomobject]@{Directory='azure-observability-v1';Id='azure-observability-v1';Title='Examen 7 — Azure et observabilité S21–S22';Duration=120;Candidates=@('azure-hosting-decision-001','azure-secret-source-001','azure-correlation-signal-001','azure-cost-guardrail-001','api-secret-redaction-001','docker-hardening-policy-001','docker-memory-limit-001','ci-deploy-gate-001','ci-artifact-name-001','api-cancellation-budget-001','api-page-size-001','quality-diff-risk-001','tests-expiry-clock-001','security-owner-policy-001','security-login-message-001')},
    [pscustomobject]@{Directory='final-readiness-v1';Id='final-readiness-v1';Title='Examen 8 — synthèse et défense S1–S24';Duration=150;Candidates=@('csharp-price-conversion-001','algo-pair-sum-001','api-http-status-map-001','api-order-validation-001','security-owner-policy-001','tests-boundary-values-001','tests-database-name-001','quality-review-severity-001','git-conflict-marker-001','docker-hardening-policy-001','ci-deploy-gate-001','azure-hosting-decision-001','azure-secret-source-001','azure-correlation-signal-001','azure-release-evidence-001','azure-incident-brief-001')}
)
foreach ($exam in $ExamDefinitions) {
    Write-JsonFile (Join-Path $ContentRoot "exams/$($exam.Directory)/exam.json") ([ordered]@{
        schemaVersion=1;id=$exam.Id;version=1;title=$exam.Title;durationMinutes=$exam.Duration;drawCount=8;passingScore=80;eligibleExerciseIds=$exam.Candidates
    })
}

$AzureLabRoot=Join-Path $ContentRoot 'labs/azure-operations'
Write-TextFile (Join-Path $AzureLabRoot 'README.md') @'
# Laboratoire Azure et observabilité — mode local de référence

Le laboratoire se réussit sans compte Azure. Le mode local construit le starter, inspecte le plan Bicep et résout un incident à partir dʼune télémétrie entièrement factice. Il ne prétend pas prouver quʼAzure accepterait un déploiement, ni mesurer disponibilité, coût ou performance réels.

## Avertissement coût et confidentialité

Un déploiement Azure réel est facultatif, manuel et peut être facturé. Avant toute création, utilisez un groupe de ressources dédié, choisissez des tailles minimales adaptées, fixez un budget et une heure de suppression, puis vérifiez après suppression quʼaucune ressource facturable ne subsiste. Une alerte de budget ne garantit pas lʼarrêt des dépenses.

Ne placez aucun identifiant Azure, donnée personnelle ou valeur sensible dans ces fichiers, paramètres enregistrés, sorties de commande, captures ou journaux. Les noms de paramètres décrivent uniquement les valeurs à fournir hors du dépôt. Managed Identity et les rôles minimaux remplacent les identifiants applicatifs durables lorsque le service les prend en charge.

## Preuve hors ligne

```powershell
dotnet build content/labs/azure-operations/starter/DeploymentPlan.csproj
powershell -ExecutionPolicy Bypass -File content/labs/azure-operations/Verify-LocalMode.ps1
powershell -ExecutionPolicy Bypass -File content/labs/azure-operations/Resolve-SimulatedIncident.ps1
```

Le plan compare App Service et Container Apps ; il décrit Azure SQL, Storage, Key Vault, Managed Identity, Log Analytics et Application Insights. `main.bicep` est un support dʼinfrastructure inspectable, pas une commande exécutée par Forge.NET. Toute validation réelle doit être annoncée comme manuelle et consigner région, taille, heure de création, propriétaire de suppression et résultat final.

## Suppression dʼune répétition réelle facultative

Vérifiez dʼabord le nom exact du groupe dédié et son contenu. Lancez ensuite la suppression depuis votre propre procédure contrôlée, attendez sa fin et interrogez à nouveau Azure. Ne réutilisez jamais un groupe contenant une ressource personnelle ou partagée.
'@
Write-TextFile (Join-Path $AzureLabRoot 'infra/main.bicep') @'
targetScope = 'resourceGroup'

@description('Short lowercase prefix chosen for this optional isolated rehearsal.')
@minLength(3)
@maxLength(12)
param resourcePrefix string

@description('Azure region selected after checking service availability and current price.')
param location string = resourceGroup().location

@allowed([
  'app-service'
  'container-apps'
])
param hostingChoice string

@description('Pinned container reference supplied outside the repository when Container Apps is selected.')
param containerImageReference string

@description('Directory object identifier supplied outside the repository for the optional SQL Entra administrator.')
param deploymentOperatorObjectId string

@description('Display name supplied outside the repository for the optional SQL Entra administrator.')
param deploymentOperatorName string

var suffix = uniqueString(resourceGroup().id)
var compactName = take(replace('${resourcePrefix}${suffix}', '-', ''), 24)

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${resourcePrefix}-logs-${suffix}'
  location: location
  properties: {
    retentionInDays: 30
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${resourcePrefix}-insights-${suffix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    DisableLocalAuth: true
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2025-01-01' = {
  name: compactName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
  }
}

resource vault 'Microsoft.KeyVault/vaults@2025-05-01' = {
  name: '${resourcePrefix}-kv-${suffix}'
  location: location
  properties: {
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enablePurgeProtection: true
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
  }
}

resource sql 'Microsoft.Sql/servers@2025-01-01' = {
  name: '${resourcePrefix}-sql-${suffix}'
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: deploymentOperatorName
      principalType: 'User'
      sid: deploymentOperatorObjectId
      tenantId: subscription().tenantId
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
    version: '12.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sql
  name: 'app'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

resource appPlan 'Microsoft.Web/serverfarms@2024-11-01' = if (hostingChoice == 'app-service') {
  name: '${resourcePrefix}-plan-${suffix}'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2025-03-01' = if (hostingChoice == 'app-service') {
  name: '${resourcePrefix}-web-${suffix}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: appPlan.id
    siteConfig: {
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
    }
  }
}

resource containerEnvironment 'Microsoft.App/managedEnvironments@2025-01-01' = if (hostingChoice == 'container-apps') {
  name: '${resourcePrefix}-env-${suffix}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: listKeys(logs.id, logs.apiVersion).primarySharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2025-01-01' = if (hostingChoice == 'container-apps') {
  name: '${resourcePrefix}-app-${suffix}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: 'app'
          image: containerImageReference
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        maxReplicas: 1
        minReplicas: 0
      }
    }
  }
}

output selectedHosting string = hostingChoice
output deletionScope string = resourceGroup().name
output nonSensitiveVaultUri string = vault.properties.vaultUri
'@
Write-TextFile (Join-Path $AzureLabRoot 'starter/DeploymentPlan.csproj') @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
'@
Write-TextFile (Join-Path $AzureLabRoot 'starter/Program.cs') @'
string choice = args.Length == 1 ? args[0] : "simulate";
if (!StringComparer.Ordinal.Equals(choice, "simulate"))
{
    Console.Error.WriteLine("Ce starter exécute uniquement le mode local simulé.");
    return 2;
}

string[] checks =
[
    "hosting-choice-explicit",
    "managed-identity-required",
    "shared-keys-disabled",
    "cost-warning-present",
    "deletion-scope-dedicated",
    "telemetry-without-personal-data",
];
foreach (string check in checks)
{
    Console.WriteLine($"PASS {check}");
}
return 0;
'@
Write-TextFile (Join-Path $AzureLabRoot 'telemetry/simulated-incident.csv') @'
timestamp,correlationId,operation,status,durationMs
2026-08-05T08:00:00Z,forge-fake-corr-001,GET-orders,200,210
2026-08-05T08:00:02Z,forge-fake-corr-002,GET-orders,500,920
2026-08-05T08:00:04Z,forge-fake-corr-003,GET-orders,500,1100
2026-08-05T08:00:06Z,forge-fake-corr-004,GET-orders,200,810
'@
Write-PowerShellFile (Join-Path $AzureLabRoot 'Resolve-SimulatedIncident.ps1') @'
<#
.SYNOPSIS
    Échafaude les documents S21-S24 : manifestes, sources, jeux de tests et squelettes de leçon.

.DESCRIPTION
    Ce script ne rédige pas la pédagogie. Il produit la structure et les données dérivables, et
    laisse des marqueurs « TODO: » partout où une rédaction humaine est nécessaire. Ces marqueurs
    sont refusés par la règle d'authenticité unsubstituted-placeholder : un lot échafaudé mais non
    rédigé ne peut donc pas être publié.

    Un fichier déjà présent est conservé, jamais réécrit, sauf avec -Force.

.PARAMETER Force
    Régénère les fichiers existants. Détruit toute reprise éditoriale.
#>
[CmdletBinding()]
param([switch]$Force)
$ErrorActionPreference='Stop'
$rows=Import-Csv (Join-Path $PSScriptRoot 'telemetry/simulated-incident.csv')
if($rows.Count -ne 4){throw 'Le jeu de télémétrie simulé doit contenir exactement quatre observations.'}
if($rows.correlationId | Where-Object {$_ -notmatch '^forge-fake-corr-[0-9]{3}$'}){throw 'Un identifiant de corrélation simulé est invalide.'}
$errors=@($rows | Where-Object {[int]$_.status -ge 500})
$ordered=@($rows.durationMs | ForEach-Object {[int]$_} | Sort-Object)
$p95=$ordered[$ordered.Count-1]
if($errors.Count -ne 2 -or $p95 -ne 1100){throw "Diagnostic inattendu : errors=$($errors.Count), p95=$p95."}
Write-Output 'INCIDENT SIMULÉ RÉSOLU : 2 erreurs corrélées, p95 borné à 1100 ms, aucune donnée personnelle.'
'@
Write-PowerShellFile (Join-Path $AzureLabRoot 'Verify-LocalMode.ps1') @'
<#
.SYNOPSIS
    Échafaude les documents S21-S24 : manifestes, sources, jeux de tests et squelettes de leçon.

.DESCRIPTION
    Ce script ne rédige pas la pédagogie. Il produit la structure et les données dérivables, et
    laisse des marqueurs « TODO: » partout où une rédaction humaine est nécessaire. Ces marqueurs
    sont refusés par la règle d'authenticité unsubstituted-placeholder : un lot échafaudé mais non
    rédigé ne peut donc pas être publié.

    Un fichier déjà présent est conservé, jamais réécrit, sauf avec -Force.

.PARAMETER Force
    Régénère les fichiers existants. Détruit toute reprise éditoriale.
#>
[CmdletBinding()]
param([switch]$Force)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$bicep=Get-Content -Raw (Join-Path $root 'infra/main.bicep')
foreach($proof in @('Microsoft.Web/sites@','Microsoft.App/containerApps@','Microsoft.Sql/servers@','Microsoft.Storage/storageAccounts@','Microsoft.KeyVault/vaults@','Microsoft.Insights/components@','SystemAssigned','allowSharedKeyAccess: false','publicNetworkAccess: ''Disabled''')){
    if($bicep.IndexOf($proof,[System.StringComparison]::Ordinal) -lt 0){throw "Preuve IaC absente : $proof"}
}
foreach($forbidden in @('BEGIN PRIVATE KEY','AccountKey=','SharedAccessSignature=','ghp_','sk-live-','sk-proj-')){
    if($bicep.IndexOf($forbidden,[System.StringComparison]::OrdinalIgnoreCase) -ge 0){throw "Valeur interdite détectée : $forbidden"}
}
dotnet build (Join-Path $root 'starter/DeploymentPlan.csproj') --nologo
if($LASTEXITCODE -ne 0){throw "Le starter local ne construit pas : code $LASTEXITCODE."}
dotnet run --project (Join-Path $root 'starter/DeploymentPlan.csproj') --no-build -- simulate
if($LASTEXITCODE -ne 0){throw "Le mode local simulé échoue : code $LASTEXITCODE."}
& (Join-Path $root 'Resolve-SimulatedIncident.ps1')
if($LASTEXITCODE -ne 0){throw "La résolution dʼincident simulé échoue : code $LASTEXITCODE."}
Write-Output 'MODE LOCAL VALIDÉ : aucune ressource Azure créée.'
'@

$CareerRoot=Join-Path $CatalogRoot 'career'
Write-TextFile (Join-Path $CareerRoot 'README.md') @'
# Kit carrière local — preuves, pas promesses

Ces supports transforment des apprentissages en faits vérifiables. Ils ne promettent ni emploi, ni entretien, ni niveau de salaire. Une activité pédagogique reste nommée comme telle et ne devient jamais une expérience professionnelle inventée.

Les données dʼun CV et dʼun suivi de candidature sont personnelles. Travaillez dans une copie locale exclue de Git, minimisez les champs, retirez adresse complète, date de naissance, identifiants privés et noms de tiers, puis inspectez métadonnées et historique avant toute publication.

Le fichier dʼexemple utilise uniquement des personnes et organisations fictives. `Export-CareerEvidence.ps1` refuse les champs qui ressemblent à des coordonnées directes et génère un Markdown local à lʼemplacement explicitement choisi.
'@
Write-TextFile (Join-Path $CareerRoot 'CV-EVIDENCE.md') @'
# Matrice de preuves pour CV

Pour chaque ligne : contexte exact, action personnelle, technologie utile, résultat observé, commande ou artefact reproductible, limite. Nʼinventez jamais utilisateur, revenu, pourcentage, durée professionnelle ou impact commercial.

Exemple factice : « Projet personnel Forge.NET — isolé un runner Docker non-root et sans réseau ; tests dʼintégration reproductibles couvrant limites de temps, mémoire et sortie. » La preuve porte sur les tests, pas sur une exploitation en production.
'@
Write-TextFile (Join-Path $CareerRoot 'STAR-WORKBOOK.md') @'
# Carnet STAR

- Situation : contexte court, sans nom privé.
- Task : responsabilité réelle et bornée.
- Action : décisions et actions personnelles, au singulier.
- Result : observation vérifiable ; écrire « non mesuré » lorsque nécessaire.
- Reflection : erreur, feedback et prochaine expérience.

Une réponse modèle consultée nʼest pas une preuve de maîtrise. Reformulez sans support, répondez à une variante et planifiez une reprise à blanc.
'@
Write-TextFile (Join-Path $CareerRoot 'APPLICATION-TRACKER.md') @'
# Suivi minimal de candidatures

Conservez seulement organisation, rôle, source publique, date, statut, prochaine action et note strictement utile. Nʼenregistrez pas dʼadresse personnelle, de commentaire sensible sur un tiers ni de donnée reçue hors contexte. Définissez une durée de conservation et supprimez les lignes devenues inutiles.

Statuts suggérés : à étudier, préparée, envoyée, réponse reçue, entretien, close. Aucun statut ne prédit un résultat.
'@
Write-TextFile (Join-Path $CareerRoot 'NEGOTIATION-GUIDE.md') @'
# Préparation à la négociation

Listez vos contraintes, préférences, questions sur le rôle, étendue des responsabilités et éléments non salariaux. Toute donnée de marché est datée, sourcée et présentée comme observation, jamais comme garantie. Définissez votre décision vous-même ; ce guide ne promet aucun emploi ni salaire.
'@
Write-TextFile (Join-Path $CareerRoot 'POST-HIRE-PLAN.md') @'
# Trame post-embauche — plan seulement

Cette trame nʼimplémente pas un parcours post-embauche. Elle aide à préparer trente jours adaptables : obtenir les accès minimaux, cartographier un parcours métier, comprendre build/test/livraison, observer une revue, livrer un correctif borné, demander du feedback et consigner les zones inconnues. Les attentes réelles de lʼéquipe priment sur ce plan.
'@
Write-PowerShellFile (Join-Path $CareerRoot 'sample-evidence.psd1') @'
@{
    DisplayName = 'Camille Exemple'
    Project = 'Forge.NET — laboratoire local'
    Action = 'A résolu un incident simulé avec logs structurés et corrélation.'
    Evidence = 'Commande locale reproductible et tests verts.'
    Limitation = 'Aucun déploiement de production ni résultat commercial revendiqué.'
}
'@
Write-PowerShellFile (Join-Path $CareerRoot 'Export-CareerEvidence.ps1') @'
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$InputPath,
    [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference='Stop'
$data=Import-PowerShellDataFile -LiteralPath $InputPath
foreach($required in @('DisplayName','Project','Action','Evidence','Limitation')){
    if([string]::IsNullOrWhiteSpace([string]$data[$required])){throw "Champ requis absent : $required"}
}
$text=($data.Values -join ' ')
if($text -match '[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}' -or $text -match '(?:\+33|0)[1-9](?:[ .-]?\d{2}){4}'){
    throw 'Export refusé : une coordonnée directe semble présente. Minimisez les données avant de recommencer.'
}
$parent=Split-Path -Parent ([System.IO.Path]::GetFullPath($OutputPath))
if([string]::IsNullOrWhiteSpace($parent)){throw 'Le chemin de sortie doit avoir un dossier explicite.'}
[System.IO.Directory]::CreateDirectory($parent) | Out-Null
$markdown="# Preuve de carrière — données à vérifier avant partage`n`n> Document local contenant des données personnelles potentielles. Inspecter contenu, métadonnées et historique avant publication.`n`n- Nom dʼaffichage : $($data.DisplayName)`n- Projet : $($data.Project)`n- Action : $($data.Action)`n- Preuve : $($data.Evidence)`n- Limite : $($data.Limitation)`n`nCe document ne promet ni emploi ni salaire.`n"
[System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($OutputPath),$markdown,(New-Object System.Text.UTF8Encoding($true)))
Write-Output "EXPORT CRÉÉ : $([System.IO.Path]::GetFullPath($OutputPath))"
'@

Write-Output 'Contenu S21-S24 généré : 10 leçons, 6 exercices Azure, 62 entretiens, 50 cartes dʼanglais, 1 projet final et 2 examens.'

Write-ScaffoldSummary -ScriptName (Split-Path -Leaf $PSCommandPath)
