# Runbook local — Forge.NET

Ce runbook couvre le monolithe Web, sa base SQLite locale, le CodeRunner Docker optionnel et le SqlLab SQL Server jetable. Compose conserve le CodeRunner en mode manuel et ne monte jamais le socket Docker.

## Prérequis Windows

- Git ;
- Docker Desktop configuré pour les conteneurs Linux, **moteur 25.0 ou plus récent**
  (`docker version --format '{{.Server.Version}}'`) : le bac à sable monte `/input` en lié non
  récursif, et la forme antérieure de cette option a été supprimée du moteur ;
- Docker Compose v2 (`docker compose version`) ;
- port TCP local `5012` libre, ou un autre port choisi dans `.env`.
- pour les tests SqlLab depuis l'hôte, port loopback `14333` libre ; SQL Server lui-même n'est jamais publié.

Le SDK .NET n'est pas nécessaire pour le mode Compose. Il reste requis pour le mode CLI et les vérifications du dépôt.

## Installation vierge avec Compose

Depuis PowerShell :

```powershell
git clone <URL_DU_DEPOT> forge-dotnet
Set-Location forge-dotnet
Copy-Item .env.example .env
docker compose config
docker compose up -d --build
docker compose ps
Invoke-WebRequest -UseBasicParsing http://localhost:5012/health
```

La commande de démarrage construit l'application, applique explicitement les migrations incluses, puis démarre le Web. Ouvrir ensuite `http://localhost:5012`.

`.env` ne doit contenir aucun secret. Les seules variables prises en charge sont :

| Variable | Valeur par défaut | Usage |
|---|---:|---|
| `FORGE_HTTP_PORT` | `5012` | port HTTP publié sur `127.0.0.1` uniquement |
| `FORGE_DATA_VOLUME` | `forge-dotnet-data` | nom du volume Docker de progression |
| `FORGE_SQL_LAB_PORT` | `14333` | pont de test sur `127.0.0.1`, sans secret |

Après modification de `.env`, contrôler le résultat effectif avec `docker compose config` avant de démarrer.

## Mode CLI sans Docker

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/ForgeDotNet.Infrastructure --startup-project src/ForgeDotNet.Web
dotnet run --project src/ForgeDotNet.Web
```

Ce mode utilise par défaut `%LOCALAPPDATA%\Forge.NET\data` et reste indépendant du volume Docker.

## CodeRunner Docker local

Le mode normal reste `Manual`. Pour construire et vérifier l'image isolée depuis PowerShell :

```powershell
docker --context desktop-linux version
docker --context desktop-linux build --pull --no-cache -t forge-dotnet-runner:test src/ForgeDotNet.CodeRunner/Container
$imageId = (docker --context desktop-linux image inspect forge-dotnet-runner:test --format "{{.Id}}").Trim()
$imageId
docker --context desktop-linux image inspect $imageId
dotnet build ForgeDotNet.sln --no-restore
dotnet test tests/ForgeDotNet.IntegrationTests/ForgeDotNet.IntegrationTests.csproj --no-build --filter "Category=CodeRunnerSecurity"
docker --context desktop-linux ps -a --filter "label=forge-dotnet.runner=true"
```

La construction sans cache nécessite plusieurs gigaoctets libres. Ne jamais remplacer `$imageId` par le tag dans la configuration : le démarrage refuse toute référence qui n'est pas un SHA-256 complet.

Pour démarrer le Web CLI avec l'adaptateur :

```powershell
$runnerRoot = Join-Path $env:LOCALAPPDATA 'ForgeDotNet\runner-workspaces'
$env:CodeRunner__Mode = 'Docker'
$env:CodeRunner__Docker__Context = 'desktop-linux'
$env:CodeRunner__Docker__ImageReference = $imageId
$env:CodeRunner__Docker__WorkspaceRootPath = $runnerRoot
dotnet run --project src/ForgeDotNet.Web
```

En mode Docker, les dix exercices répertoriés dans `docs/CONTENT_S1_S2_MATRIX.md` chargent leur suite approuvée côté serveur. Une révision obsolète, une suite invalide ou tout autre exercice sans suite retourne `Unavailable` ; ce comportement ne doit pas être contourné par une commande locale ou une suite fournie par l'utilisateur.

La preuve fonctionnelle complète du lot se rejoue avec :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-initial-csharp-content.ps1
```

### Politique et scan

Le test `EffectiveContainerPolicyMatchesEveryRequiredControl` inspecte la politique pendant une exécution. Contrôler aussi le moteur et l'image :

```powershell
docker --context desktop-linux info --format "{{json .SecurityOptions}}"
docker --context desktop-linux image inspect forge-dotnet-runner:test --format "{{.Id}}|{{.Os}}|{{.Architecture}}|{{.Config.User}}|{{json .Config.Entrypoint}}|{{.Size}}"
powershell -ExecutionPolicy Bypass -File scripts/scan-code-runner-image.ps1
```

Le script épingle Trivy 0.70.0, crée deux volumes Docker jetables pour la base et l'espace de travail, télécharge sa base sans socket Docker, puis analyse l'image avec le réseau désactivé. Les deux volumes sont supprimés même en cas d'échec ; l'espace libre du stockage Docker doit donc être contrôlé avant l'audit. Le scan doit terminer avec un rapport contenant de vrais paquets OS/.NET. Un timeout, une absence de sortie, `Target: -`, `pkg_num=0`, une base CVE indisponible ou une vulnérabilité critique sont des échecs de validation. Le socket temporaire est réservé à la phase native hors ligne du scanner et n'est jamais monté dans le runner Forge.

Audit de référence du 28 juillet 2026 sur l'image runner `sha256:d34875ea2a6adcd8247bc67ef214fdda7613fda89208629d55bac1e6851bd40c` : Alpine 3.23.5, 37 paquets OS, 34 manifestes .NET, `0` vulnérabilité critique avec Trivy 0.70.0. Rejouer après toute reconstruction ou mise à jour de digest.

### Coupure et récupération

Le test manuel de reprise se fait uniquement quand `docker ps` confirme qu'aucun autre conteneur n'est actif :

1. lancer le test `InfiniteLoopIsKilledByTestTimeout` ;
2. dès que le conteneur labellisé apparaît, arrêter Docker Desktop ;
3. vérifier que l'appel n'est jamais annoncé comme réussi ;
4. relancer Docker Desktop et attendre que `docker version` réussisse ;
5. noter l'orphelin éventuel avec la commande ci-dessous, sans le supprimer manuellement ;
6. lancer le test nominal : la maintenance du runner doit supprimer l'orphelin avant la nouvelle tentative ;
7. prouver que les conteneurs et workspaces sont absents.

```powershell
docker --context desktop-linux ps -a --filter "label=forge-dotnet.runner=true"
dotnet test tests/ForgeDotNet.IntegrationTests/ForgeDotNet.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~NormalProgramCompilesAndPassesVisibleAndHiddenTests"
docker --context desktop-linux ps -a --filter "label=forge-dotnet.runner=true"
Get-ChildItem (Join-Path $env:LOCALAPPDATA 'ForgeDotNet\runner-workspaces') -Force -ErrorAction SilentlyContinue
```

Un nettoyage dont l'absence ne peut pas être prouvée est une erreur. Ne pas utiliser `docker system prune`, ne pas supprimer des conteneurs étrangers et ne jamais monter `/var/run/docker.sock` dans le runner.

## SqlLab SQL Server jetable

Le démarrage normal de Forge laisse SqlLab désactivé. Depuis PowerShell :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start-sql-lab.ps1
docker compose --profile sql-lab --profile sql-lab-test ps
dotnet test --no-build --filter "Category=SqlLabSecurity"
powershell -ExecutionPolicy Bypass -File scripts/stop-sql-lab.ps1
```

Pour démarrer également le Web Compose avec SqlLab activé :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start-sql-lab.ps1 -IncludeWeb
Invoke-WebRequest -UseBasicParsing http://localhost:5012/health/sql-lab
```

Le script crée un secret aléatoire sous `.secrets/`, ignoré par Git. Il n'affiche jamais sa valeur. Ne pas déplacer ce secret vers `.env`, Compose ou le volume SQLite. Le profil `sql-lab-test` ajoute un pont TCP sans secret publié uniquement sur `127.0.0.1:14333` et seul membre de son réseau sortant dédié ; le service SQL reste sans port et uniquement sur le réseau interne.

Contrôles manuels :

```powershell
docker inspect forge-dotnet-sql-lab
docker network inspect forge-dotnet-sql-lab-internal
docker logs --tail 100 forge-dotnet-sql-lab
docker volume inspect forge-dotnet-data
```

Vérifier `10001:10001`, `CapDrop=[ALL]`, `no-new-privileges`, le profil seccomp, `internal=true`, l'absence de port/montage SQLite/socket et l'absence de `MSSQL_SA_PASSWORD` dans `Config.Env`. Le reset de page détruit/recrée une base de session ; l'arrêt Compose détruit toute l'instance SQL. `scripts/stop-sql-lab.ps1 -PurgeSecret` supprime aussi le secret local après contrôle strict du chemin. Les attaques et risques résiduels sont dans `docs/SQLLAB.md`.

## Santé et diagnostic

```powershell
docker compose ps
docker compose logs --tail 100 forge-dotnet
docker compose config
docker inspect forge-dotnet
docker volume inspect forge-dotnet-data
Invoke-WebRequest -UseBasicParsing http://localhost:5012/health
powershell -ExecutionPolicy Bypass -File scripts/verify-compose.ps1
powershell -ExecutionPolicy Bypass -File scripts/test-compose.ps1
```

Un état `unhealthy` doit être traité comme un échec. Examiner les logs sans les publier s'ils contiennent des données locales.

## Arrêt et redémarrage

Arrêt en conservant la progression :

```powershell
docker compose down --remove-orphans
```

Le volume nommé n'est pas supprimé. Un redémarrage réutilise les mêmes données :

```powershell
docker compose up -d
```

Vérifier ensuite `/health` et le profil enregistré.

## Mise à jour des images

Les images de base sont figées par digest dans le Dockerfile. Une mise à jour est volontaire : modifier les digests, reconstruire, analyser l'image, puis rejouer tous les tests.

```powershell
docker compose build --pull
docker scout cves forge-dotnet:local
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

Une vulnérabilité critique non acceptée interdit la validation de l'image.

Audit de référence du 25 juillet 2026 sur `forge-dotnet:local` : `0` critique, `0` élevée, `1` moyenne et `0` faible. L'alerte moyenne est `CVE-2025-60876` sur BusyBox `1.37.0-r30`, sans version corrigée publiée dans la base Docker Scout au moment du contrôle. Rejouer l'audit lors de toute mise à jour d'image et appliquer la correction Alpine dès qu'elle existe.

Sous Linux, les clés Data Protection persistées dans le volume ne disposent pas du protecteur DPAPI Windows et ASP.NET signale qu'elles sont stockées sans chiffrement applicatif. Le port reste local et le volume Docker doit être protégé par les contrôles d'accès et le chiffrement disque du poste. Aucun secret ne doit être ajouté au volume ou à Compose pour masquer cet avertissement.

## Nettoyage récupérable

`docker compose down --remove-orphans` supprime les conteneurs et le réseau Compose, mais conserve la progression. Les images inutilisées peuvent être inspectées avec `docker image ls` avant toute suppression.

La suppression du volume est irréversible sans sauvegarde. Avant une remise à zéro volontaire, créer et vérifier une sauvegarde depuis `/settings`, arrêter Compose, puis seulement utiliser :

```powershell
docker compose down --volumes --remove-orphans
```

Ne jamais utiliser cette commande pour un simple arrêt.

## Incidents courants

- **Moteur Docker indisponible** : démarrer Docker Desktop et vérifier `docker version`.
- **« Docker a refusé la politique d'isolation »** : vérifier d'abord la version du moteur. Sous 25.0,
  l'option de montage non récursif du bac à sable n'existe pas dans la forme employée, et aucun
  conteneur d'exécution n'est créé. Le message ne distingue pas ce cas d'un refus de sécurité réel.
- **Port occupé** : choisir un autre `FORGE_HTTP_PORT` dans `.env`, puis rejouer `docker compose config`.
- **Health rouge** : consulter `docker compose logs forge-dotnet`, vérifier le volume et ne pas supprimer la base pour masquer l'erreur.
- **Erreur de permission du volume** : contrôler l'utilisateur effectif avec `docker inspect`; le conteneur doit rester non-root.
- **Migration en échec** : arrêter le service et conserver le volume pour diagnostic ou restauration. Ne pas lancer la migration suivante.

## Qualification finale du MVP — 6 août 2026

La procédure d'installation vierge a été rejouée depuis une copie source isolée, avec un nom de projet Compose, un port loopback et un volume neufs. Elle a révélé deux dépendances d'exécution absentes de l'ancienne image : `content/exams`, `content/sql` et la base de fuseaux horaires requise par `Europe/Paris`. Le Dockerfile les embarque désormais et `WebCompositionTests.ContainerImagePackagesEveryContentDirectoryLoadedAtStartup` empêche leur régression.

Après reconstruction sur volume vierge :

- le conteneur était `healthy`, non-root (`1654`), avec racine en lecture seule, capacités supprimées et `no-new-privileges` ;
- seul `127.0.0.1` publiait le port HTTP ;
- `/health` et les douze routes principales répondaient 200 avec CSP, `nosniff` et politique de référent restrictive ;
- aucun niveau `Error` ou `Critical`, secret SQL, en-tête Bearer ou code soumis n'apparaissait dans les logs finaux ;
- après échauffement, les réponses allaient de 41 ms à 720 ms, p95 720 ms ; le premier démarrage à froid a atteint 24,5 s sur le poste de qualification.

Pour rejouer sans toucher au volume habituel, choisir des valeurs dédiées :

```powershell
$env:COMPOSE_PROJECT_NAME = 'forge-dotnet-mvp-check'
$env:FORGE_HTTP_PORT = '5099'
$env:FORGE_DATA_VOLUME = 'forge-dotnet-mvp-check-data'
docker compose config
docker compose up -d --build
docker compose ps
Invoke-WebRequest -UseBasicParsing http://127.0.0.1:5099/health
docker compose logs --no-color forge-dotnet
docker compose down --remove-orphans
```

Supprimer le volume de qualification uniquement après avoir vérifié son nom exact et confirmé qu'il ne contient aucune progression utile. Ne jamais employer une purge Docker globale pour libérer de l'espace pendant cette procédure.

La revue visuelle responsive et la navigation clavier n'ont pas pu être rejouées avec le navigateur intégré, qui échouait avant lancement avec `failed to write kernel assets: Le chemin d’accès spécifié est introuvable. (os error 3)`. Les contrôles automatisés couvrent structure sémantique, lien d'évitement, focus, viewport et contrastes, mais une vérification humaine reste à refaire dans un navigateur fonctionnel avant diffusion plus large.

La matrice complète, les commandes et les risques résiduels se trouvent dans [`MVP_ACCEPTANCE.md`](MVP_ACCEPTANCE.md).
