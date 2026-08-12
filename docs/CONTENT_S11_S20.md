# Contenu S11–S20 — matrice gelée

## Portée et règle de comptage

Cette matrice est la source de vérité de l’incrément 09. Un élément compte seulement si son manifeste v1 est valide, ses références sont résolues, ses artefacts privés existent et ses preuves spécialisées réussissent. Les fixtures, projets de laboratoire et fichiers de documentation ne comptent pas comme leçons ou exercices du catalogue.

L’incrément complète ici les minima globaux de 35 activités API/tests/sécurité et 15 activités Git/Docker/CI/Azure : 09 en livre respectivement 35 et 9, tandis que les 6 activités Azure restent exclusivement réservées à S21–S22. Aucun volume S21+ ne répare un trou S11–S20.

## Volumes cibles et réalisés

| Semaine | Leçons | Activités automatisables | Domaine principal | Projet actif | Examen de jalon |
|---:|---:|---:|---|---|---|
| S11 | 3 | 5 | HTTP, REST, routage, contrôleurs, DTO | API mini-ERP | — |
| S12 | 3 | 5 | validation, DI, configuration, secrets, erreurs | API mini-ERP | — |
| S13 | 3 | 5 | async, annulation, pagination, filtres, tri, OpenAPI | API mini-ERP | — |
| S14 | 3 | 5 | authentification, autorisation, OWASP | API mini-ERP | Examen 5 |
| S15 | 3 | 5 | xUnit, AAA, règles et frontières | stratégie de tests | — |
| S16 | 3 | 5 | doubles, intégration, base et API de test | stratégie de tests | — |
| S17 | 3 | 5 | non-régression, analyse, refactoring, revue | stratégie de tests | Examen 6 |
| S18 | 3 | 3 | Git, commits, branches, conflits, PR, versions | livraison conteneurisée | — |
| S19 | 3 | 3 | images, runtime, volumes, réseaux, Compose | livraison conteneurisée | — |
| S20 | 3 | 3 | CI, artefacts, variables, secrets, porte de livraison | livraison conteneurisée | — |
| **Total** | **30** | **44** | **35 API/tests/sécurité + 9 Git/Docker/CI** | **3 projets** | **2 examens** |

Après l’incrément 09, le catalogue de référence contient 352 documents et 1 812 fichiers : 60 leçons, 129 exercices, 25 DebugLabs, 128 questions d’entretien liées, 8 mini-projets, une activité d’anglais historique et un curriculum de 20 semaines. Les 40 scénarios SQL/EF restent dans leur source spécialisée et les 6 banques d’examen sous `content/exams/`.

## Projets progressifs

| Projet | Semaines | Preuves obligatoires | Frontière pédagogique |
|---|---|---|---|
| `project-api-mini-erp-001` | 11–14 | contrat HTTP, validation 400, auth 401/403, création 201, pagination, OpenAPI | aucun projet final, règles hors contrôleur |
| `project-testing-strategy-001` | 15–17 | règles pures, frontières, date injectée, intégration/API, non-régression et revue | aucun test cosmétique ou double sans intention |
| `project-container-delivery-001` | 18–20 | conflit Git réel, PR/revue, image épinglée non-root, santé Compose, CI locale | aucun cloud S21+, aucun secret réel |

Chaque manifeste impose `no-complete-solution-before-submission`. Le laboratoire montre une tranche de référence bornée ; il ne génère pas la remise finale de l’apprenant.

## Examens 5 et 6

- `api-security-v1` : 16 candidats S11–S14, tirage de 8, 120 minutes, seuil 80 %.
- `tests-quality-v1` : 15 candidats S15–S17, tirage de 8, 120 minutes, seuil 80 %.

Les exercices utilisent le runner C# existant et conservent solutions et tests cachés côté serveur. Le moteur n’est pas modifié par 09 ; la couverture thématique est portée par les compétences des manifestes, les banques et les laboratoires HTTP.

## Laboratoires et preuves

- `content/labs/api-mini-erp/` : ASP.NET Core .NET 10, contrôleur mince, DTO validé, Problem Details, annulation, pagination bornée, clé factice injectée, rôles Reader/Operator et contrat OpenAPI local.
- `content/labs/testing-strategy/` : bibliothèque pure et tests xUnit de partitions, frontières, erreurs et date injectée.
- `content/labs/git-review/` : script PowerShell créant un dépôt temporaire, produisant un conflit réel, vérifiant les marqueurs puis conservant les deux exigences à la résolution.
- `content/labs/container-delivery/` : Dockerfile multi-stage à bases épinglées et Compose avec boucle locale, secrets fichier, lecture seule, capacités supprimées, `no-new-privileges`, limites CPU/mémoire/PID et health check.
- `content/labs/ci-delivery/` : workflow à permissions minimales, restore/build/test, artefact borné, image Docker et répétition de livraison protégée ; script local équivalent avec arrêt au premier échec.

## Sécurité et exclusions

Les seules preuves d’authentification versionnées sont explicitement factices et limitées aux tests. En exécution conteneurisée, les valeurs viennent de fichiers hors Git, sont comparées à temps constant et ne sont jamais journalisées. Le rôle provient de la preuve reconnue, jamais d’un en-tête choisi séparément par le client.

Sont absents : Azure, observabilité avancée, Kubernetes, microservices, sujets distribués, projet final, carrière, secret réel et dépendance réseau pédagogique obligatoire. Les ressources réseau utilisées pour restaurer des outils ou paquets relèvent uniquement de la validation technique.

## Commandes spécialisées

```powershell
dotnet test content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj --configuration Release
dotnet test content/labs/testing-strategy/ForgeTestingLab.Tests/ForgeTestingLab.Tests.csproj --configuration Release
powershell -ExecutionPolicy Bypass -File content/labs/git-review/verify-conflict.ps1
docker compose -f content/labs/container-delivery/compose.yaml config
powershell -ExecutionPolicy Bypass -File content/labs/ci-delivery/verify-ci.ps1
dotnet test tests/ForgeDotNet.IntegrationTests/ForgeDotNet.IntegrationTests.csproj --no-build --filter "Category=ContentS11S20"
```

Une commande applicable en échec refuse l’incrément. Une vérification manuelle n’est jamais rapportée comme une validation automatique.

## Validation du 5 août 2026

- La commande de référence complète réussit : build sans avertissement ni erreur, 124 tests unitaires, 149 tests d’intégration et 28 tests E2E, soit 301 tests réussis ; validation des fixtures, des 40 scénarios SQL/EF, des 352 documents du catalogue de référence et formatage sans changement.
- Les laboratoires réussissent séparément : 4 tests API avec preuves 200/400/401/403/404/201, 6 tests de stratégie, conflit Git réel créé puis résolu, image Docker construite et service sain sous utilisateur non-root/lecture seule/quotas, workflow CI exécuté localement et contrôlé par `actionlint`.
- Les 129 solutions publiées réussissent dans le runner et chaque starter compile sans passer indûment. La matrice dédiée prouve que 44 de ces exercices appartiennent à S11–S20 et possèdent chacun au moins deux tests visibles, deux tests cachés, quatre indices progressifs et une variante.
- Un défaut de fiabilité séparé a été corrigé sans retry ni relèvement des limites : les deux examens EF conteneurisés utilisent une collection non parallèle prioritaire et une fixture distincte avant le balayage massif du catalogue.
- Les revues éditoriale et de sécurité ont vérifié les durées, rubriques, secrets factices, politiques d’autorisation et exclusions. Une performance orale réelle d’apprenant reste une activité humaine ; aucune réussite orale n’est revendiquée automatiquement.

## Reprise de densité, postérieure à l'incrément 09

La matrice ci-dessus est le relevé gelé de l'incrément 09 et n'est pas réécrite. Le volume de pratique
qu'elle décrit — cinq activités pour chacune des semaines S11 à S17 — s'est révélé être le défaut
central de cette période : 5,1 activités par semaine contre 8,8 en S1–S10, précisément sur les
semaines qui décident d'une embauche backend.

La reprise avance par lots et son état courant est figé, semaine par semaine, par la matrice de
`ContentS11S20CoverageTests`. Après le lot 1, S11 à S17 portent six activités chacune, soit 42 au lieu
de 36. Le détail des lots, le choix de conception qui les gouverne et la cible restante figurent dans
`ROADMAP.md`.

Deux limites de cette période restent ouvertes et ne sont pas comblées par du volume :

- les activités S19 et S20 — et leurs équivalents Azure en S21–S22 — sont des fonctions pures sur un
  domaine d'entrée de quelques valeurs, donc mémorisables ; elles entraînent la décision, pas le
  geste ;
- les cinq laboratoires de cette période portent la seule pratique réelle de Docker, de la chaîne de
  livraison et d'une API complète, et ils ne sont rattachés à aucune page du parcours public.
