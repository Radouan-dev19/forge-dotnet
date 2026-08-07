# Acceptation du MVP — incrément 11

Date de qualification finale : 7 août 2026. Les 16 critères sont conformes ; la passe intégrale finale termine avec un code de sortie nul sur le poste Windows/Docker cible.

Cette matrice relie chaque critère du MVP à une preuve reproductible. Une ligne n'est conforme que si le test cité est réellement exécuté par la suite de vérification finale et si les commandes de qualification restent vertes.

## Matrice des 16 critères

| # | Critère MVP | Statut | Preuve reproductible |
|---:|---|---|---|
| 1 | Application démarre localement | Conforme | Installation Compose depuis une copie source isolée, avec projet, port et volume vierges ; conteneur `healthy` et `GET /health` = 200. Le contrôle de composition [`ContainerImagePackagesEveryContentDirectoryLoadedAtStartup`](../tests/ForgeDotNet.EndToEndTests/WebCompositionTests.cs) empêche d'omettre un répertoire de contenu ou `tzdata` de l'image. |
| 2 | Diagnostic fonctionne | Conforme | [`ReducedDiagnosticCompletesWithAggregateEvaluationOnly`](../tests/ForgeDotNet.EndToEndTests/DiagnosticWebTests.cs) couvre le parcours HTTP réduit ; [`FrozenSessionAndAutosavedResponseSurviveCompleteRestart`](../tests/ForgeDotNet.IntegrationTests/DiagnosticSessionPersistenceTests.cs) prouve l'autosauvegarde et la reprise après redémarrage complet. |
| 3 | Parcours personnalisé créé | Conforme | [`ProvisionalPlanCanBeAdjustedAcceptedAndReloaded`](../tests/ForgeDotNet.EndToEndTests/DiagnosticWebTests.cs) et [`DiagnosticToAdjustedAcceptedPlanIsVersionedAndSurvivesRestart`](../tests/ForgeDotNet.IntegrationTests/WeeklyPlanPersistenceTests.cs) couvrent diagnostic, ajustement, acceptation, version et reprise. |
| 4 | Leçon complète consultable | Conforme | [`LessonIsCompleteAccessibleAndDoesNotExposeServerOnlyContent`](../tests/ForgeDotNet.EndToEndTests/WebSmokeTests.cs) vérifie le lecteur public ; [`ReferenceLessonContainsFourteenOrderedSectionsAndAUsefulQuiz`](../tests/ForgeDotNet.IntegrationTests/LessonContentReaderTests.cs) contrôle la structure et le quiz conformément au guide de contenu. |
| 5 | Exercice C# soumis et testé | Conforme | [`NormalProgramCompilesAndPassesVisibleAndHiddenTests`](../tests/ForgeDotNet.IntegrationTests/DockerCodeRunnerSecurityTests.cs) exécute le cas nominal dans le runner isolé ; [`AllOneHundredThirtyFivePublishedSolutionsPassAndStartersCompileWithoutPassing`](../tests/ForgeDotNet.IntegrationTests/InitialCSharpContentTests.cs) valide les solutions publiées et refuse d'assimiler les starters à des réussites. |
| 6 | Exercice SQL exécuté | Conforme | [`NormalSelectValidationRollbackAndResetAreReliable`](../tests/ForgeDotNet.IntegrationTests/SqlLabSecurityTests.cs) prouve résultat, rollback et reset sur base jetable ; [`SqlSolutionEquivalentNegativeVariantPlanEffectsAndResetAreProven`](../tests/ForgeDotNet.IntegrationTests/SqlEfContentTests.cs) couvre solutions, variantes négatives, plans, effets et remise à zéro. |
| 7 | Lab de débogage suivi | Conforme | [`WebJourneyEnforcesInvestigationTestAndProtectedSolution`](../tests/ForgeDotNet.EndToEndTests/DebugLabWebTests.cs) couvre le cycle Web ; [`EveryScenarioIsBrokenThenRepairedByItsRegressionSuite`](../tests/ForgeDotNet.IntegrationTests/DebugLabDockerRunnerTests.cs) prouve les huit régressions cassées puis réparées dans le runner. |
| 8 | Indices progressifs | Conforme | [`ManualPracticePageProtectsHintsAndSolutionUntilServerTransitions`](../tests/ForgeDotNet.EndToEndTests/PracticeWebTests.cs) contrôle l'exposition Web ; [`HintRequiresReflectionAndStrictOrderWithCap`](../tests/ForgeDotNet.UnitTests/PracticeRulesTests.cs) impose réflexion préalable, ordre et plafond. |
| 9 | Solution déclenche révision | Conforme | [`ErrorsBugsMissedQuestionsAndSolutionsBecomeDeduplicatedPrivateCandidates`](../tests/ForgeDotNet.IntegrationTests/ReviewSourceProviderTests.cs) transforme les consultations de solution en candidates privées dédupliquées ; les tests Practice empêchent l'attribution de maîtrise après consultation. |
| 10 | Examen sans aide | Conforme | [`ActiveExamExposesNoAidOrReportAndDashboardUsesOnlyRealMetrics`](../tests/ForgeDotNet.EndToEndTests/ExamDashboardWebTests.cs) vérifie l'interface active ; [`ServerDeadlineControlsResumeAndSubmission`](../tests/ForgeDotNet.UnitTests/ExamIntegrityTests.cs) impose l'échéance serveur lors de la reprise et de la soumission. |
| 11 | Maîtrise calculée | Conforme | [`AssistanceCapsCannotBeBypassed`](../tests/ForgeDotNet.UnitTests/MasteryRulesTests.cs) attaque les plafonds d'aide et [`FakeExamCannotSatisfyExamComponent`](../tests/ForgeDotNet.UnitTests/MasteryRulesTests.cs) refuse un examen autoproclamé. Toute la catégorie `MasteryAntiGaming` est rejouée par la suite complète. |
| 12 | Révisions dues visibles | Conforme | [`GeneralSuccessesFollowEveryDocumentedInterval`](../tests/ForgeDotNet.UnitTests/ReviewSchedulingTests.cs) contrôle J+1/J+3/J+7/J+14/J+30 avec horloge déterministe ; [`EmptyQueueExplainsIntervalsAbsenceAndScoreIntegrity`](../tests/ForgeDotNet.EndToEndTests/ReviewWebTests.cs) vérifie l'état vide honnête et l'accès Web. |
| 13 | Dashboard honnête | Conforme | [`ActiveExamExposesNoAidOrReportAndDashboardUsesOnlyRealMetrics`](../tests/ForgeDotNet.EndToEndTests/ExamDashboardWebTests.cs) relit uniquement les preuves réelles ; [`AnalyticsExcludesInactiveGapsAndNeverInventsUnavailableRates`](../tests/ForgeDotNet.UnitTests/ExamIntegrityTests.cs) exclut les périodes inactives et laisse indisponible un taux sans échantillon. |
| 14 | Sauvegarde et restauration | Conforme | Les six tests [`LocalDataBackupTests`](../tests/ForgeDotNet.IntegrationTests/LocalDataBackupTests.cs) couvrent aller-retour, archive invalide, traversal, checksum faux, corruption avec checksum recalculé et version de manifeste non supportée. |
| 15 | Tests automatisés passent | Conforme | La passe finale de `powershell -ExecutionPolicy Bypass -File scripts/verify.ps1` termine avec le code `0` : build avec 0 avertissement/0 erreur, 124/124 tests unitaires, 42/42 E2E, 107/107 intégrations non-SqlLab et 48/48 intégrations SqlLab, 0 test ignoré, contenu valide et `dotnet format --verify-no-changes` vert. |
| 16 | Installation neuve documentée | Conforme | La procédure [`Installation vierge avec Compose`](RUNBOOK.md#installation-vierge-avec-compose) a été rejouée depuis une copie source isolée avec un nom de projet Compose, un port et un volume jamais utilisés. Les douze routes principales ont répondu 200, les en-têtes de sécurité étaient présents et les logs finaux ne contenaient aucune erreur. |

## Qualification complémentaire

### Incidents corrigés et passe finale

Les tentatives rouges précédentes sont conservées comme historique et n'ont jamais été présentées comme acceptables :

1. 124/124 unitaires et 42/42 E2E verts ; 151/155 intégrations vertes, puis `Espace insuffisant sur le disque` et trois timeouts en cascade.
2. Après sérialisation des projets : 124/124 et 42/42 verts ; 133/155 intégrations vertes, puis `fichier de pagination insuffisant`, `out of memory`, moteur Docker indisponible et échecs en cascade.
3. Après sérialisation des collections xUnit : 124/124 et 42/42 verts ; 152/155 intégrations vertes. Les trois timeouts restants se produisaient pendant que SqlLab occupait les ressources en parallèle des runners.
4. Après partition démontrée de 107 intégrations non-SqlLab et 48 SqlLab : 124/124 et 42/42 verts ; 106/107 non-SqlLab verts. Le test des 135 solutions a finalement reçu `Unavailable` pour `csharp-nullable-fallback-001` lorsque C: est tombé à 184 Mio libres. La partition SqlLab n'a donc pas été lancée par cette passe arrêtée fermée.

Après récupération de l'espace disque, les derniers défauts mesurés ont été corrigés sans masquer les erreurs : projets et collections sérialisés, SqlLab démarré seulement pour ses 48 tests, quota runner porté à 1 CPU, délai global des tests runner porté de 15 à 30 s et délai de connexion SqlLab porté de 5 à 15 s. Les bornes restent validées ; 31 s est refusé, une boucle infinie est tuée sous 60 s et le nettoyage est prouvé. La découverte xUnit couvre exactement 107 + 48 = 155 intégrations, sans trou ni doublon.

La passe finale du 7 août 2026 est verte de bout en bout en 45 min 14 s : 124 unitaires, 42 E2E, 107 non-SqlLab et 48 SqlLab, aucun échec et aucun test ignoré. Elle valide aussi 8 documents de fixture valides, le refus de 10 fixtures invalides avec 109 diagnostics attendus, 40 scénarios SQL/EF et les deux catalogues. Le nettoyage sûr n'a touché ni aux volumes de progression, ni aux images étrangères, ni au cache global ; la configuration WSL et le pagefile n'ont pas été modifiés.

### Installation et erreurs

- L'image Web inclut désormais les six répertoires de contenu chargés au démarrage (`reference`, `diagnostic`, `practice`, `debug-lab`, `exams`, `sql`) et la base de fuseaux horaires.
- L'essai vierge a détecté puis fait corriger deux erreurs bloquantes : banque d'examens absente de l'image et fuseau `Europe/Paris` absent. Un test de composition rend ces omissions non régressives.
- Le mode sans Docker et la panne SqlLab restent honnêtes : les pages sont consultables, signalent l'indisponibilité et n'annoncent aucune validation automatique.
- La restauration hostile échoue fermée et ne remplace jamais la base active.

### Accessibilité et performance

- Les douze routes principales sont contrôlées automatiquement pour `lang="fr"`, viewport mobile, navigation étiquetée, lien d'évitement, cible `main` focalisable, région `main` unique et absence de `tabindex` positif ou d'`autofocus`.
- Un lien « Aller au contenu principal » visible au focus et un anneau de focus à deux tons ont été ajoutés. L'activation transfère le focus vers `main-content`. Les contrastes calculés des textes et contrôles inspectés vont de 5,18:1 à 14,64:1.
- Sur l'image Compose finale et après échauffement, les douze routes ont répondu entre 41 ms et 720 ms, avec un p95 de 720 ms. Le premier démarrage à froid a atteint 24,5 s sur ce poste ; cette latence d'initialisation/JIT est documentée et doit être surveillée.
- Le navigateur intégré a vérifié l'accueil, le dashboard, la pratique, SqlLab indisponible et les examens à 390 puis 375 px : aucun débordement horizontal et aucune erreur/alerte console. L'API de frappe n'a pas déplacé le focus avec Tab/Entrée dans cette session ; une revue humaine native du parcours clavier reste donc recommandée avant diffusion, sans remettre en cause la structure, l'activation de la cible et les non-régressions automatisées.

### Sécurité et dépendances

- Les huit projets ne déclarent aucun paquet NuGet vulnérable ou déprécié. `xunit` 2.9.3 est toutefois classé legacy par NuGet ; la migration vers xUnit v3 est différée car elle modifierait le harnais complet, sans vulnérabilité connue justifiant ce risque pendant la finition.
- Trivy 0.70.0 avec base fraîche trouve zéro vulnérabilité critique dans l'image Web finale (`forge-dotnet:local`) et zéro vulnérabilité dans l'image CodeRunner finale `sha256:43e075c820a78f5cb0f61e3c6923b9c5bd3833f3a20bd9e168a0028665ed181a` après inspection de 37 paquets Alpine et 34 manifestes .NET.
- SQL Server 2022 CU26 remplace CU21. L'image de base CU26 a zéro vulnérabilité critique. Le scan direct de l'image dérivée a été interrompu par un `SIGBUS` du moteur Docker ; l'inventaire des paquets et les trois binaires Go ont des empreintes identiques à la base, et l'historique ne contient aucune installation de paquet. Cette preuve compensatoire est une inférence documentée, pas un scan direct réussi.
- Les contrôles effectifs SqlLab ont été inspectés : utilisateur `10001`, réseau interne, capacités supprimées, `no-new-privileges`, seccomp et limites mémoire/CPU/PID. Les tests de quota, timeout, annulation, redaction, rollback et reset sont verts.
- Les scans de secrets n'ont trouvé aucun secret littéral commité. Les `NotImplementedException` restants appartiennent uniquement aux starters pédagogiques et les tests prouvent qu'ils ne réussissent pas avant implémentation.

## Risques résiduels et décisions humaines

1. Rejouer la navigation clavier avec des frappes natives humaines avant une diffusion plus large ; les viewports mobiles, la cible de saut, le transfert de focus, le DOM accessible et la console sont déjà vérifiés.
2. Rejouer un scan direct de l'image SQL dérivée lorsque le stockage Docker est stabilisé ; refuser toute vulnérabilité critique alors détectée.
3. Planifier la migration xUnit v3 dans un changement dédié avec requalification intégrale du harnais.
4. Les clés Data Protection Linux restent protégées par les droits et le chiffrement disque du poste, sans protecteur applicatif dans le volume Compose.
5. L'incrément 12 reste un audit pédagogique indépendant et contradictoire ; aucune de ses conclusions n'est anticipée ici.
