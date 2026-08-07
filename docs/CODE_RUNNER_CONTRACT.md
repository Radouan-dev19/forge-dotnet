# Contrat CodeRunner 04B–05

`ICodeRunner` reste un port d'Application. L'incrément 04C ajoute deux adaptateurs à l'orchestration définie en 04B : `DockerCodeRunner`, qui exécute une suite approuvée dans un conteneur isolé, et le mode manuel, qui exporte une archive sans prétendre valider la proposition.

## Dépendances

```text
Web -> RunExercise (Application) -> ICodeRunner (Application)
                                  -> DockerCodeRunner (CodeRunner)
                                  -> DeterministicCodeRunner (CodeRunner, tests/démonstration)
```

Application ne dépend pas de Docker. Domain, Infrastructure et les composants Razor ne construisent aucune commande. Web choisit le mode uniquement dans la composition de dépendances. Le processus Web n'exécute jamais le code soumis.

## Requête

`CodeRunRequest` contient uniquement :

- un `RequestId` GUID non vide pour l'idempotence ;
- l'identifiant stable, la version et la révision SHA-256 de l'activité exécutable ;
- de un à huit fichiers C# nommés sans chemin.

Le contrat n'accepte aucune commande, aucun argument de shell, aucun chemin de test, aucune solution et aucun test caché. Un fichier est limité à 64 Kio UTF-8 et la requête à 256 Kio. Les noms doivent être uniques sans distinction de casse, simples et terminés par `.cs`.

La source serveur `IDockerRunSpecificationSource` associe seule une requête à une suite approuvée. `FileSystemDockerRunSpecificationSource` exige l'identité, la version et la révision privée exactes d'un exercice 04D ou d'un scénario DebugLab 05, confine `tests/runner.json`, `tests/visible/cases.json` et `tests/hidden/cases.json` sous le catalogue et n'accepte qu'une liste blanche de types. Une révision périmée retourne `Unavailable` ; une suite mal formée échoue explicitement. La fixture `forge-security-fixture-v1` reste réservée aux tests d'abus.

## Résultat et statuts

`CodeRunResult` sépare toujours `Compilation` et `Tests`.

| Statut global | Compilation | Tests |
|---|---|---|
| `Succeeded` | `Succeeded` | `Succeeded` |
| `CompilationFailed` | `Failed` | `NotRun` |
| `TestsFailed` | `Succeeded` | `Failed` |
| `TimedOut` | `TimedOut`, ou `Succeeded` | `NotRun`, ou `TimedOut` |
| `Cancelled` | `Cancelled`, ou `Succeeded` | `NotRun`, ou `Cancelled` |
| `Unavailable` | `Unavailable` | `NotRun` |

Les diagnostics publics indiquent sévérité, code, message et éventuellement une position dans un fichier soumis. Un nom de fichier inconnu est supprimé. Un échec caché fournit seulement un compteur avec `HiddenFailuresRedacted=true` : nom, code, chemin et diagnostic cachés ne traversent pas le contrat.

Chaque sortie est bornée à 64 Kio UTF-8 et marquée lorsqu'elle est tronquée. Le nombre de diagnostics et d'échecs visibles est limité à 100. `RunExercise` normalise à nouveau chaque résultat, même si l'adaptateur affirme avoir appliqué ces limites.

## Pipeline Docker

`DockerCodeRunner` accepte uniquement un identifiant d'image local immuable de la forme `sha256:<64 caractères>`. Il vérifie avant exécution : Linux, architecture supportée, utilisateur `1654:1654`, point d'entrée fixe, label de version `04C`, absence de port/volume déclaré et taille maximale de 900 Mio.

Pour chaque requête :

1. les anciens conteneurs labellisés et workspaces `run-*` sont récupérés ou supprimés ;
2. un workspace aléatoire, enfant direct de la racine configurée, reçoit `request.json`, les sources validées et, pour une suite pédagogique, `suite.bin` chiffré par AES-GCM ;
3. `docker create` reçoit une liste fixe d'arguments sans shell ;
4. le conteneur compile avec le Roslyn de l'image épinglée, sans restauration NuGet ni réseau ;
5. la clé éphémère de 256 bits est transmise uniquement sur l'entrée standard attachée, puis effacée ; le processus parent protège sa mémoire contre les dumps ;
6. chaque cas est exécuté dans un sous-processus seccomp distinct qui reçoit seulement la signature et les arguments du cas courant, jamais la valeur attendue, les autres cas ou le marqueur visible/caché ;
7. les messages NDJSON structurés sont normalisés ; les sorties brutes ne sont pas journalisées ;
8. le conteneur et le workspace sont supprimés, puis leur absence est prouvée. Une preuve de nettoyage impossible devient une erreur explicite.

La commande et le point d'entrée ne viennent jamais de la requête. Les chemins du compilateur et des assemblies de référence correspondent au SDK/runtime de l'image de base épinglée ; toute mise à jour du digest impose leur revue et la reconstruction de l'image.

## Politique effective

- réseau `none`, aucun port publié ;
- racine en lecture seule ;
- seul `/input` est monté depuis l'hôte, en lecture seule et depuis le workspace dédié ;
- `/workspace` 64 Mio et `/tmp` 16 Mio sont des `tmpfs` jetables avec `noexec,nosuid,nodev` ;
- utilisateur `1654:1654`, aucune capability, `no-new-privileges`, profil `seccomp=builtin` explicite, aucun device, aucun socket Docker ;
- 1 CPU, 512 Mio de mémoire et de swap, 64 PID/threads, 256 descripteurs ouverts ;
- 25 s de compilation, 30 s globales pour tous les cas d'une suite et 5 s de marge de contrôle ;
- sortie publique 64 Kio, capture interne bornée et pilote de logs Docker `none` ;
- concurrence configurable de 1 à 4, valeur locale par défaut 2.

La limite disque repose sur la taille du `tmpfs`. Une limite `fsize` de 64 Kio a été refusée après mesure car elle empêche CoreCLR de démarrer ; elle n'est pas nécessaire pour borner l'espace inscriptible. La limite `nofile=256:256` est la plus petite valeur conservée après les essais de restauration/compilation du SDK.

## Modes

- `Manual` est le mode par défaut et celui de Compose. Il retourne `Unavailable` et permet de télécharger un zip déterministe contenant seulement un manifeste public, un README et les sources soumises. Le zip ne contient ni solution ni test caché et n'est jamais une preuve automatique.
- `Deterministic` reste un double pour tests et démonstrations locales. Il n'appelle aucun processus.
- `Docker` exige un contexte valide, un workspace absolu et l'identifiant SHA-256 complet de l'image. Une configuration absente ou hors bornes échoue au démarrage.

Exemple PowerShell pour la composition Docker locale :

```powershell
$imageId = (docker --context desktop-linux image inspect forge-dotnet-runner:test --format "{{.Id}}").Trim()
$env:CodeRunner__Mode = 'Docker'
$env:CodeRunner__Docker__Context = 'desktop-linux'
$env:CodeRunner__Docker__ImageReference = $imageId
dotnet run --project src/ForgeDotNet.Web
```

Les 85 exercices C#/algo, 25 scénarios DebugLab et deux items EF Core d’examen S1–S10 répertoriés dans `CONTENT_S1_S10.md` possèdent une suite conforme au contrat générique. L’identité, la version et la révision publiée sont vérifiées avant lecture des fichiers privés ; aucune liste de commandes ou logique par exercice n’est ajoutée au moteur. Un contenu absent ou périmé reste `Unavailable`, sans bascule vers une exécution locale non isolée.

## Idempotence, annulation et historique

Le runner met en cache la première exécution par `RequestId` et empreinte complète. Un retry identique retourne le même résultat ; réutiliser l'identifiant avec un autre contenu échoue explicitement. L'annulation tue le client Docker puis le conteneur en nettoyage. `RunExerciseHistory` conserve au plus vingt résultats par exercice, sans proposition, et reste volatil.

## Vérifications

```powershell
docker --context desktop-linux build --pull --no-cache -t forge-dotnet-runner:test src/ForgeDotNet.CodeRunner/Container
docker --context desktop-linux image inspect forge-dotnet-runner:test
dotnet build ForgeDotNet.sln --no-restore
dotnet test tests/ForgeDotNet.IntegrationTests/ForgeDotNet.IntegrationTests.csproj --no-build --filter "Category=CodeRunnerSecurity"
dotnet test tests/ForgeDotNet.IntegrationTests/ForgeDotNet.IntegrationTests.csproj --configuration Release --no-build --filter "Category=InitialCSharpContent"
docker --context desktop-linux ps -a --filter "label=forge-dotnet.runner=true"
powershell -ExecutionPolicy Bypass -File scripts/scan-code-runner-image.ps1
dotnet format ForgeDotNet.sln --verify-no-changes --no-restore
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

La validation 04C exige un scan de l'image sans vulnérabilité critique non acceptée et le test manuel de coupure/reprise décrit dans `docs/RUNBOOK.md`. Après l’ajout borné des assemblies EF Core SQLite pour l’examen 4, l’image `sha256:64289bc26b73a208f5a5f3029c5fdf5698a24d89dee20a402b31a963df48dc7f` a repassé les 18 tests d’abus. Le scan Trivy hors ligne du 30 juillet 2026 a couvert 37 paquets Alpine et 34 manifestes .NET sans vulnérabilité critique détectée.

## Limites

Docker réduit le risque sans constituer une isolation absolue. Le runner est local, mono-hôte et prévu pour une seule instance Forge à la fois. Il n'attribue ni tentative sérieuse, ni maîtrise, ni score. Il n'inclut pas SqlLab, Kubernetes, cloud ou intégration IDE distante.
