# Forge.NET

Forge.NET est une application locale d'apprentissage actif de C# et .NET. Le MVP couvre un diagnostic prudent, un plan personnalisé de 24 semaines, leçons, pratique C#, DebugLab, SqlLab, maîtrise anti-contournement, révisions espacées, examens et dashboard factuel. Le contenu S1–S24 est versionné sous `content/` et la progression reste dans SQLite. Le CodeRunner et SqlLab sont des adaptations Docker optionnelles et isolées ; le mode manuel ne prétend jamais avoir validé du code automatiquement.

## Prérequis

- SDK .NET `10.0.300` compatible avec [`global.json`](global.json).
- PowerShell 5.1 ou 7+ pour le script de vérification.
- Le CLI EF Core local, restauré par `dotnet tool restore`.
- Docker Desktop avec les conteneurs Linux et Docker Compose v2 pour le mode Compose.

## Lancement local

```powershell
dotnet run --project src/ForgeDotNet.Web
```

Avec le profil HTTP fourni, l'application est disponible sur [http://localhost:5012](http://localhost:5012). Le terminal affiche les adresses effectivement utilisées au démarrage.

Routes disponibles :

- `/` : accueil ;
- `/dashboard` : informations réelles du profil et prochaine étape ;
- `/profile` : édition du profil et contrat d'apprentissage ;
- `/settings` : emplacement des données, sauvegarde et restauration explicite ;
- `/about` : présentation du projet ;
- `/learn` : modules publiés et recherche par titre, objectif ou compétence ;
- `/learn/{lessonId}` : lecteur de leçon, quiz, note, signet et progression de lecture ;
- `/diagnostic` : consignes, couverture, démarrage ou reprise du diagnostic ;
- `/diagnostic/session/{sessionId}` : section chronométrée et collecte autosauvegardée ;
- `/diagnostic/session/{sessionId}/evaluation` : rapport agrégé, incertitude et lacunes critiques après clôture ;
- `/plan/{sessionId}` : proposition hebdomadaire, ajustement de charge et acceptation versionnée ;
- `/practice` : exercices de pratique manuelle publiés ;
- `/practice/{exerciseId}` : réflexion, indices, tentatives, solution protégée et historique ;
- `/labs` et `/labs/{labId}` : les huit laboratoires — vrais projets exécutés sur le poste de l'apprenant, hors bac à sable, à réussite déclarée et sans preuve de maîtrise ;
- `/debug-lab` et `/debug-lab/{scenarioId}` : méthode d'investigation, journal et scénarios de débogage ;
- `/sql-lab` : choix d'un scénario SQL publié, session jetable provisionnée avec son jeu de données, exécution bornée, validation et reset ;
- `/mastery` : preuves, scores versionnés et portes de maîtrise explicables ;
- `/reviews` : file de révisions dues et cartes personnelles ;
- `/exams` et `/exams/{attemptId}` : examens sans aide gouvernés par l'échéance serveur ;
- `/health` : santé HTTP, intégrité SQLite et état des migrations.

## Lancement avec Docker Compose

```powershell
Copy-Item .env.example .env
docker compose config
docker compose up -d --build
docker compose ps
Invoke-WebRequest -UseBasicParsing http://localhost:5012/health
```

L'application est publiée uniquement sur `127.0.0.1`. Le conteneur s'exécute sans privilèges root, avec un système racine en lecture seule et un volume nommé pour SQLite et les clés Data Protection. `docker compose down --remove-orphans` arrête proprement l'environnement sans supprimer ce volume.

Le SDK .NET sert uniquement à construire l'image ; l'étape d'exécution contient le runtime ASP.NET. Les images officielles sont figées par digest. Les variables autorisées et la procédure d'installation vierge sont détaillées dans [`docs/RUNBOOK.md`](docs/RUNBOOK.md).

## Validation des schémas de contenu v1

Depuis la racine du dépôt :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/validate-content.ps1 content/fixtures/valid
```

La commande ne démarre pas le serveur, ne charge pas de catalogue et ne modifie pas SQLite. Les contrats, protections et codes de sortie sont décrits dans [`docs/CONTENT_SCHEMA_V1.md`](docs/CONTENT_SCHEMA_V1.md).

## Authenticité du contenu

Au-delà des schémas, trois règles refusent un contenu structurellement valide mais pédagogiquement creux : marqueurs de génération non substitués, paragraphes recopiés d'un document à l'autre, et leçon dont l'explication répète l'intuition ou qui ne montre aucun code.

Le registre [`content/authoring/content-debt.json`](content/authoring/content-debt.json) liste les documents encore hérités du générateur. C'est un cliquet : un défaut non déclaré refuse le lot, une déclaration devenue inutile le refuse aussi, et `ContentAuthenticityTests` refuse toute dette supérieure au plafond figé. La dette ne peut donc que décroître.

```powershell
# Régénérer le registre après une reprise éditoriale, puis abaisser le plafond dans le test.
dotnet run --project src/ForgeDotNet.Web --no-launch-profile -- --emit-content-debt content/reference content/sql
```

Le standard attendu d'une leçon est décrit dans [`docs/CONTENT_AUTHORING_STANDARD.md`](docs/CONTENT_AUTHORING_STANDARD.md). Les semaines S1 à S10 en sont l'application de référence et se suivent sans ressource externe ; S11 à S24 restent à reprendre.

Les échafaudeurs `scripts/New-S*Content.ps1` ne réécrivent jamais un fichier existant sans `-Force`, et émettent des marqueurs `TODO:` que la validation refuse : un lot généré mais non rédigé ne peut pas être publié comme terminé.

### Correction des exercices, sans Docker

```powershell
dotnet test tests/ForgeDotNet.IntegrationTests --filter FullyQualifiedName~ExerciseCorrectnessTests
```

Pour chacun des 142 exercices publiés, la solution est compilée en mémoire et doit passer tous ses cas visibles et cachés, tandis que le starter doit compiler et en échouer au moins un. La sémantique reproduit celle du conteneur. Ce contrôle ne remplace pas le bac à sable Docker pour une soumission d'apprenant : il rend seulement le contenu vérifiable sur un poste sans moteur Docker.

Trois autres invariants sont vérifiés au passage : aucun exercice ne répète deux fois les mêmes arguments, chacun porte au moins 3 cas visibles et 4 cachés — sauf domaine booléen épuisé — et chaque cas déclare soit un résultat attendu, soit une exception attendue, jamais les deux.

## Charger le catalogue minimal

```powershell
powershell -ExecutionPolicy Bypass -File scripts/load-catalog.ps1 content/reference -Search evaluer -Skill csharp.types
powershell -ExecutionPolicy Bypass -File scripts/test-catalog.ps1
```

Le premier script charge et recherche un snapshot immuable. Le second démontre qu'un rechargement invalide est refusé sans remplacer le snapshot courant. Les contrats et limites sont décrits dans [`docs/CONTENT_CATALOG.md`](docs/CONTENT_CATALOG.md).

## Utiliser le lecteur

Ouvrez `/learn`, recherchez par exemple `csharp.types`, puis ouvrez la leçon de référence. Le sommaire, les blocs de code et le quiz sont rendus depuis un modèle typé sans HTML brut. Une visite seule ne crée aucune progression : seules les sections confirmées et la réussite du quiz sont comptées.

La note est enregistrée automatiquement, tandis que le signet est modifié explicitement. Ces états survivent au rechargement et au redémarrage local. Le contrat complet, les limites et les contrôles de sécurité sont décrits dans [`docs/LESSON_READER.md`](docs/LESSON_READER.md).

## Utiliser le diagnostic

Ouvrez `/diagnostic`, puis choisissez le diagnostic initial de 27 questions ou le mode réduit de 9 questions. Les neuf domaines sont toujours représentés. Le tirage et les échéances sont enregistrés dès le démarrage ; actualiser ou fermer la page ne prolonge pas une section active.

Chaque sélection est sauvegardée localement. Une section expirée refuse les réponses tardives, un abandon conserve les réponses existantes et une clôture avec des réponses manquantes reste explicitement incomplète. Après clôture, le rapport affiche les neuf domaines, un score borné, un intervalle d'incertitude, une confiance qualitative et les lacunes critiques sans exposer les réponses attendues. Le mode réduit reste provisoire et aucune faiblesse critique n'est compensée par la moyenne. Le contrat détaillé est décrit dans [`docs/DIAGNOSTIC.md`](docs/DIAGNOSTIC.md).

Depuis le rapport, ouvrez le plan personnalisé. Sa charge est limitée aux disponibilités du profil et à 15 h par semaine. Les lacunes critiques sont prioritaires, une compétence forte raccourcit l'étude sans supprimer le contrôle prévu, et un diagnostic incomplet produit un plan provisoire. Chaque ajustement crée une version ; l'acceptation fige la dernière. Les semaines sont des thèmes de progression, pas des activités déjà livrées. Le contrat détaillé est décrit dans [`docs/WEEKLY_PLAN.md`](docs/WEEKLY_PLAN.md).

## Utiliser la pratique manuelle

Ouvrez `/practice`, choisissez un exercice et renseignez les six champs de réflexion. Chaque indice H1 à H4 est débloqué dans l'ordre et tracé. Une solution ne devient accessible qu'après deux tentatives sérieuses distinctes et le délai serveur configuré ; sa consultation marque explicitement l'activité comme non maîtrisée et demande une explication personnelle ainsi qu'une variante.

Depuis 04D, dix exercices C# S1–S2 disposent chacun de trois cas visibles, quatre cas cachés, quatre indices, une solution expliquée, une variante, des cartes et une question d'entretien. Le mode par défaut, y compris dans Compose, reste `Manual` et ne prétend jamais avoir validé le code. En mode `Docker`, l'adaptateur charge uniquement la suite approuvée correspondant à l'identité, la version et la révision exactes du contenu. Le protocole est décrit dans [`docs/PRACTICE.md`](docs/PRACTICE.md), le contrat dans [`docs/CODE_RUNNER_CONTRACT.md`](docs/CODE_RUNNER_CONTRACT.md), la matrice dans [`docs/CONTENT_S1_S2_MATRIX.md`](docs/CONTENT_S1_S2_MATRIX.md) et l'exploitation dans [`docs/RUNBOOK.md`](docs/RUNBOOK.md).

## Persistance locale

Sous Windows, le chemin par défaut est :

```text
%LOCALAPPDATA%\Forge.NET\data\forge-dotnet.db
```

Le répertoire peut être remplacé par la variable `LocalData__DirectoryPath`, obligatoirement avec un chemin absolu hors du dépôt et de `wwwroot`. Le nom du fichier peut être remplacé avec `LocalData__DatabaseFileName`, mais doit rester un nom simple terminé par `.db`.

En environnement `Development`, la migration explicite incluse est appliquée au démarrage. Dans un autre environnement local, l'appliquer avant le lancement :

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/ForgeDotNet.Infrastructure --startup-project src/ForgeDotNet.Web
```

Forge.NET ne crée aucune donnée pédagogique de démonstration dans SQLite. Le profil initial vide est créé au premier accès et clairement distingué d'un profil renseigné.

### Sauvegarde et restauration

La page `/settings` crée une archive `.backup.zip`. L'opération effectue un checkpoint WAL, utilise l'API de sauvegarde SQLite, puis produit un manifeste versionné et un checksum SHA-256. La restauration :

1. refuse les chemins relatifs et les archives inattendues ;
2. contrôle la taille, les entrées, le manifeste et le checksum ;
3. vérifie l'intégrité et la migration sur une copie confinée ;
4. remplace atomiquement la base active ;
5. conserve la base précédente sous la forme `forge-dotnet.pre-restore-*.db`.

La case de confirmation de la page Paramètres doit être cochée avant toute restauration.

Les clés antiforgery ASP.NET sont conservées dans le sous-dossier privé `data-protection` du répertoire local et protégées par DPAPI sous Windows. Elles ne sont ni servies par le Web, ni incluses dans la sauvegarde SQLite.

## Structure

```text
src/
  ForgeDotNet.Web/             Hôte Blazor et composition
  ForgeDotNet.Application/     Cas d'usage et contrats
  ForgeDotNet.Domain/          Règles métier pures
  ForgeDotNet.Infrastructure/  EF Core, SQLite et adaptateurs locaux
  ForgeDotNet.CodeRunner/      Adaptateurs déterministe, manuel et Docker isolé
tests/
  ForgeDotNet.UnitTests/
  ForgeDotNet.IntegrationTests/
  ForgeDotNet.EndToEndTests/   Parcours HTTP et composition locale
```

Les dépendances autorisées sont décrites dans [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). `CodeRunner` dépend uniquement des contrats Application nécessaires à `ICodeRunner` et à l'export manuel ; aucun détail Docker ne remonte dans Domain ou Infrastructure.

## Vérification

```powershell
dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef migrations list --project src/ForgeDotNet.Infrastructure --startup-project src/ForgeDotNet.Web
dotnet build --no-restore
dotnet test --no-build
dotnet format
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
powershell -ExecutionPolicy Bypass -File scripts/verify-initial-csharp-content.ps1
powershell -ExecutionPolicy Bypass -File scripts/verify-compose.ps1
powershell -ExecutionPolicy Bypass -File scripts/test-compose.ps1
docker compose config
```

Les analyseurs .NET utilisent le niveau `latest-recommended`. Les avertissements restent visibles mais ne sont pas transformés globalement en erreurs : cela évite qu'une mise à jour du SDK bloque arbitrairement le socle. Les avertissements introduits doivent néanmoins être examinés et corrigés ou justifiés.

## Limites actuelles

- Le CodeRunner automatique exige Docker et une image explicitement configurée par digest ; Compose conserve volontairement le mode `Manual`, qui ne constitue jamais une preuve automatique.
- SqlLab est désactivé par défaut et exige le profil Docker dédié. SQL Server n'est jamais exposé directement sur le réseau hôte.
- **La dette éditoriale est éteinte** : `content/authoring/content-debt.json` ne déclare plus aucun document, contre 376 au relevé initial. Les trois règles d'authenticité n'ont donc plus aucune exception, et `ContentAuthenticityTests` refuse désormais toute réapparition au lieu de borner un existant. L'audit pédagogique reste néanmoins **refusé** tant que les sept personas n'ont pas été rejoués avec un navigateur et Docker disponibles. Voir [`docs/PEDAGOGICAL_AUDIT.md`](docs/PEDAGOGICAL_AUDIT.md).
- La densité de pratique se rapproche de la cible sur les semaines backend : 8,8 exercices par semaine en S1–S10 contre 5,4 en S11–S24. La reprise avance semaine par semaine et son état exact est figé par les matrices de `ContentS11S20CoverageTests` et `ContentS21S24CoverageTests` : S11 à S13 sont à dix exercices chacune depuis le lot REST, S14 à douze depuis le lot JWT, S15 à S17 à six, S18 à S20 à trois, S21 à sept depuis le lot OAuth/OIDC, S22 à S24 à un ou deux. La cible de huit par semaine est atteinte de S11 à S14 ; restent S15 à S17.
- Les exercices des semaines S19 à S22 pratiquent Docker, l'intégration continue et Azure par des fonctions pures : ils entraînent la décision, pas le geste. La pratique réelle de ces sujets passe par les onze laboratoires de `content/labs/`, désormais servis par les pages `/labs`. Un laboratoire s'exécute sur le poste de l'apprenant, hors du bac à sable : sa réussite est déclarée et ne produit aucune preuve de maîtrise, ce que chaque page annonce avant le brief.
- Le bloc front-end (S25–S27) suit une stratégie mixte : quatre exercices C# à domaine ouvert nourrissent le score de maîtrise, alors que le câblage réel passe par trois laboratoires. Le laboratoire `blazor-jwt-client` fait exception à la preuve déclarée — sa suite bUnit s'exécute dans la solution (`dotnet test`) et constitue une vraie preuve serveur au niveau de la suite de tests — sans pour autant ouvrir de porte de maîtrise, qu'aucun laboratoire ne produit. Les laboratoires `angular-orders-client` et `react-orders-client` exigent **une installation réseau `npm` initiale** : c'est le seul point du parcours qui rompt la promesse « aucune dépendance réseau obligatoire ». Leurs versions sont épinglées, leur `package-lock.json` est commité, `node_modules` n'est jamais versionné, et la commande exacte est `npm ci` puis `npm test`.
- Forge.NET reste un produit local mono-utilisateur avec SQLite ; il n'est pas conçu pour une exposition Internet ou un usage collaboratif.
- La préférence de langue est enregistrée dans le profil de session, mais seule l'interface française est complète.

## Documentation

- [`docs/PRODUCT_SPEC.md`](docs/PRODUCT_SPEC.md)
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
- [`docs/CURRICULUM.md`](docs/CURRICULUM.md)
- [`docs/CONTENT_GUIDE.md`](docs/CONTENT_GUIDE.md)
- [`docs/CONTENT_AUTHORING_STANDARD.md`](docs/CONTENT_AUTHORING_STANDARD.md)
- [`docs/CONTENT_SCHEMA_V1.md`](docs/CONTENT_SCHEMA_V1.md)
- [`docs/CONTENT_CATALOG.md`](docs/CONTENT_CATALOG.md)
- [`docs/LESSON_READER.md`](docs/LESSON_READER.md)
- [`docs/DIAGNOSTIC.md`](docs/DIAGNOSTIC.md)
- [`docs/WEEKLY_PLAN.md`](docs/WEEKLY_PLAN.md)
- [`docs/PRACTICE.md`](docs/PRACTICE.md)
- [`docs/CODE_RUNNER_CONTRACT.md`](docs/CODE_RUNNER_CONTRACT.md)
- [`docs/SECURITY.md`](docs/SECURITY.md)
- [`docs/ROADMAP.md`](docs/ROADMAP.md)
- [`docs/RUNBOOK.md`](docs/RUNBOOK.md)
- [`docs/MVP_ACCEPTANCE.md`](docs/MVP_ACCEPTANCE.md)
