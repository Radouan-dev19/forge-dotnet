# Examens et dashboard — 07C

## Portée

L’incrément 07C livre le moteur d’examen local, son rapport auditable et un dashboard calculé exclusivement depuis les données déjà persistées. L’incrément 08 fournit quatre banques complètes : trois banques C# pour S1–S7 et une banque mixte SQL/EF Core pour S8–S10. Le type de soumission est figé avec chaque item et impose le runner autorisé ; SQL n’est jamais simulé par un exercice C#.

## États, tirage et temps

Une tentative suit exclusivement `Active -> Completed`, `Active -> Abandoned` ou `Active -> TimedOut`. Une tentative terminée ne reprend jamais. Une seule tentative active est admise par profil ; l’identité, la version, la révision, le barème et les items tirés sont figés dans SQLite.

Le tirage `sha256-rank-v1` classe chaque candidat éligible par `SHA-256(seed || identité/version/révision)` puis retient les premiers rangs. Le serveur génère une seed aléatoire de 256 bits et publie seulement son engagement SHA-256 pendant l’examen. Le rapport final révèle la seed, l’algorithme et l’engagement : le tirage est alors reproductible et toute substitution devient détectable.

L’échéance UTC est créée par le serveur à partir de la durée figée. Chaque lecture, soumission et transition la revérifie. Fermer l’onglet, redémarrer l’application ou actualiser ne suspend pas l’échéance. Une exécution runner terminée après l’échéance n’est pas comptée et clôt la tentative en timeout. Le compteur affiché par Web est une photographie indicative de l’échéance serveur, jamais l’autorité.

La finalisation est transactionnelle et versionnée : statut, instant de fin et rapport immuable sont enregistrés ensemble. Une double fin concurrente produit exactement un rapport et une erreur explicite pour l’autre requête. Le détail d’une soumission et le rapport restent absents de la projection tant que la tentative est active.

## Protections sans aide

- Pendant une tentative active non expirée, `IExamAccessPolicy` ferme les cas d’usage Practice, y compris une URL directe et toutes les mutations d’indice ou de solution.
- La vue d’examen contient seulement énoncé, contraintes et code de départ. Elle omet solution, indices, cas de test, résultats visibles/cachés et seed.
- L’examen appelle le runner directement sans produire d’historique Practice. Une soumission active reçoit uniquement un accusé d’enregistrement ; les compteurs de tests n’apparaissent qu’après la fin.
- Les échecs cachés sont réduits à un compteur. Aucun nom, code, valeur attendue ou diagnostic de cas caché n’entre dans le rapport.
- Les identifiants de tentative et de soumission sont des GUID opaques. Toutes les lectures et écritures sont rattachées au profil local et chaque mutation exige la version attendue.
- Les événements Blazor passent par l’antiforgery ASP.NET existant. Aucun code, réponse d’examen ou détail de test n’est journalisé par 07C.
- Une aide externe déclarée est conservée dans le rapport et interdit la réussite ainsi que la preuve de maîtrise. Le produit n’utilise ni caméra ni proctoring intrusif. Le copier-coller n’est présenté que comme une friction possible, jamais comme une garantie.

## Examen 4 SQL/EF Core

La banque `sql-ef-core-v1` dure 120 minutes et tire ses huit candidats : six requêtes SQL et deux exercices EF Core. `FileSystemExamBankSource` fige pour chaque item identité, version, révision SHA-256, domaine et `ExamSubmissionKind`. Un snapshot absent, obsolète ou possédant un type inconnu est refusé à la lecture.

- Une soumission `Sql` est confiée exclusivement à `SqlLabExamRunner`. Celui-ci recharge l’attente privée par identité/version/révision, crée une session SqlLab jetable, exécute la requête avec le login minimal et l’attente structurée, puis détruit la session même après refus, timeout ou annulation. Une absence de preuve de nettoyage refuse le résultat.
- Une soumission EF Core reste du C# et passe par `ICodeRunner`. Les deux starters et solutions utilisent réellement EF Core avec SQLite en mémoire dans le conteneur éphémère : aucune base de progression, aucun réseau, fichier hôte ou secret n’est accessible.
- Les attentes et solutions SQL sont absentes du blueprint public. Les solutions EF et suites visibles/cachées sont confinées sous `content/sql/<id>/exam/`; la révision couvre manifeste, contrat, énoncé, starter, solution et suites.
- Le rapport conserve le même modèle borné pour les deux runners. `Unavailable` n’est jamais automatiquement vérifié et ne peut ni réussir l’examen ni produire une preuve de maîtrise.

## Maîtrise et révisions

Seuls les items soumis, vérifiés automatiquement par le runner et issus d’un rapport terminé peuvent créer une observation `ExamEngine`. Une tentative assistée n’en crée aucune. Pour la porte A, l’accomplissement « examen sans aide de 90 minutes » exige en plus une tentative réussie, non assistée et d’une durée configurée d’au moins 90 minutes ; la banque de référence de 30 minutes ne peut donc pas ouvrir cette condition.

Chaque item échoué d’un rapport terminé crée une carte de récupération `ExamFailure`. Cette carte est autoévaluée, ne révèle aucune solution et ne produit pas directement de maîtrise. L’identité figée rend sa génération idempotente.

## Dashboard et métriques

Le dashboard est une projection de lecture. Il ne persiste ni score ni agrégat et ne possède aucune commande de mutation.

| Mesure | Source et règle |
|---|---|
| Temps actif observé | somme des intervalles entre événements d’un même contexte, uniquement si l’intervalle est positif et inférieur ou égal au seuil d’inactivité de 5 minutes |
| Réussite au premier essai | première tentative observée de chaque activité ; aucune tentative donne « non disponible », pas 0 % |
| Réussite avant solution | réussite dont aucune solution antérieure n’est enregistrée ; aucune tentative donne « non disponible » |
| Aides | nombres réels d’indices et de solutions effectivement consultés |
| Révisions | file réellement due et prochaine date calculées par `ReviewService` |
| Objectif | objectif du dernier plan hebdomadaire accepté ; absent sinon |
| Examens | comptes terminés/abandonnés/timeout et moyenne des seuls rapports terminés |
| Forces/faiblesses | domaines possédant au moins une observation de maîtrise ; triés par écart au seuil, jamais inventés pour un profil vide |
| Portes | projection `MasteryService` avec chaque condition manquante ; une compétence critique ne se compense pas |

Les événements d’activité proviennent des leçons, diagnostics, tentatives Practice/DebugLab/SqlLab, révisions et examens. L’absence de mesure reste explicitement indisponible. Les durées entre contextes ou après une période inactive ne sont jamais imputées à l’apprentissage.

## Vérifications automatiques

La catégorie `ExamIntegrity` couvre tirage et seed, échéance et reprise, rapport différé, redaction des tests cachés, verrouillage des aides, fin concurrente, abandon, timeout, redémarrage, routage SQL sans appel au runner C#, échec vers révision/maîtrise, activité avec inactivité, métriques absentes, réussite avant solution et non-compensation d’une porte critique. `ContentS1S10`, `SqlLabExam` et `ContentS1S10Docker` contrôlent respectivement la banque privée, les six solutions SQL jetables et les deux starters/solutions EF isolés. Le test E2E vérifie aussi que la banque 4 est publiée sans solution.

```powershell
dotnet build ForgeDotNet.sln --no-restore --disable-build-servers
dotnet test ForgeDotNet.sln --no-build --no-restore --filter "Category=ExamIntegrity" --disable-build-servers
dotnet test ForgeDotNet.sln --no-build --no-restore --disable-build-servers
dotnet format ForgeDotNet.sln --no-restore --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

## Parcours manuel de référence

1. Ouvrir `/dashboard` sur un profil vide et confirmer que taux, moyenne, objectif, forces et faiblesses restent indisponibles.
2. Ouvrir `/exams`, démarrer l’examen de référence et vérifier engagement, échéance serveur et absence de seed, rapport, indice, solution ou détail de tests.
3. Pendant la tentative, tenter l’URL directe d’un exercice Practice : l’accès doit être refusé.
4. Soumettre les items puis terminer : le rapport apparaît une seule fois et révèle la seed vérifiable sans révéler les tests cachés.
5. Démarrer une nouvelle tentative puis l’abandonner ; contrôler son statut et les compteurs réels du dashboard.
6. Contrôler au clavier et en largeur mobile les actions essentielles. Le timeout est couvert avec une horloge serveur contrôlée dans le test d’intégration, sans attendre artificiellement trente minutes.

Ce parcours a été rejoué le 29 juillet 2026 sur une base temporaire : réussite puis abandon, accès Practice direct refusé, rapport différé, seed révélée après clôture, compteurs dashboard exacts, contrôles natifs accessibles au clavier, largeur mobile de 375 px sans débordement et aucune erreur navigateur. L’hôte, les journaux et la base temporaires ont été supprimés après vérification.

## Limites assumées

- La banque `reference-csharp-foundations-v1` contient 16 candidats S1–S2, en tire huit et dure 90 minutes. Les banques `csharp-modern-v1` et `algorithm-debug-v1` couvrent respectivement S3–S4 et S5–S7 avec 16 candidats chacune. La banque `sql-ef-core-v1` contient six soumissions SQL réelles et deux soumissions EF Core réelles, toutes imposées par son tirage de huit items.
- Le mode runner configuré détermine si une validation automatique est disponible. Un runner indisponible ne crée ni réussite ni preuve.
- Un utilisateur maître de sa machine locale peut employer une aide extérieure ; la déclaration, le temps, le tirage, les tests cachés et les réévaluations renforcent la crédibilité sans promettre une inviolabilité.
- Le temps actif est une estimation transparente fondée sur des événements, pas un suivi continu ni une mesure de productivité.
