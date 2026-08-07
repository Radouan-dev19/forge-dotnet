# SqlLab 06A

SqlLab fournit un moteur SQL Server local et jetable, pas les douze scénarios pédagogiques de 06B. Le dataset technique minimal contient uniquement `dbo.Orders`. Il sert à démontrer le cycle session → requête bornée → validation → rollback → reset → destruction.

## Topologie

```text
Navigateur --Blazor--> Web --réseau sql-lab-internal--> SQL Server 2022 CU26
                              base + login par session

Tests hôte --127.0.0.1:14333--> pont TCP sans secret --réseau interne--> SQL Server
```

SQL Server est construit depuis `mcr.microsoft.com/mssql/server@sha256:ba4c8329f48fb8f02e1416be6a930ebfd71268caee78aa985f3af4315e457c89`. Il n'a aucun port hôte, n'est membre que du réseau Docker `internal: true` et ne monte ni progression SQLite, ni chemin utilisateur, ni socket Docker. Ses fichiers sont conservés uniquement dans la couche du conteneur ; `docker compose down` les détruit. Le pont de test optionnel ne possède aucun secret, publie uniquement `127.0.0.1:14333` et utilise un second réseau dédié auquel aucun autre service n'est raccordé.

Le conteneur s'exécute en `10001:10001`, sans capability, avec `no-new-privileges`, un profil seccomp versionné, 2 CPU, 2 Gio, 512 PID et `/tmp` borné. La capability de fichier `cap_net_bind_service`, inutile sur le port 1433, est retirée de l'image afin de conserver `no-new-privileges`.

## Secret et démarrage

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start-sql-lab.ps1
docker compose --profile sql-lab --profile sql-lab-test ps
dotnet test --no-build --filter "Category=SqlLabSecurity"
powershell -ExecutionPolicy Bypass -File scripts/stop-sql-lab.ps1
```

Le script crée `.secrets/sql-lab-sa-password.txt` avec un générateur cryptographique. Le dossier est ignoré par Git et Docker. Compose monte le fichier en lecture seule ; le mot de passe n'apparaît ni dans `Config.Env`, ni dans la configuration rendue, ni dans les vues Web. `-IncludeWeb` démarre également le Web avec SqlLab activé. `-PurgeSecret` sur le script d'arrêt supprime volontairement le secret après contrôle de son chemin.

## Cycle d'une session

1. Infrastructure génère un identifiant opaque, un nom de base, un login et un mot de passe aléatoires.
2. Le compte de contrôle crée la base, désactive `TRUSTWORTHY` et le chaînage inter-base, charge le schéma/dataset puis crée l'utilisateur.
3. Le login reçoit uniquement `SELECT`, `INSERT`, `UPDATE` et `DELETE` sur `dbo`. Il n'a aucun rôle serveur ou `db_owner`; `ALTER`, `EXECUTE`, `TAKE OWNERSHIP` et `VIEW DEFINITION` sont niés.
4. La requête est contrôlée, puis envoyée telle quelle à `SqlCommand`, séparément des commandes de session. Aucun shell, chemin, serveur, login ou mot de passe ne vient du navigateur.
5. L'exécution utilise une transaction, un délai, un lock timeout, un quota de lignes et un quota UTF-8. Les effets sont observés dans la transaction, puis la transaction est toujours annulée.
6. Un reset provisionne d'abord une nouvelle base, détruit l'ancienne, puis échange la session. Si la destruction échoue, la nouvelle base est supprimée et l'ancienne session reste la référence.
7. La destruction retire la base et le login. L'arrêt de l'hôte tente aussi de nettoyer toutes les sessions restantes.

La garde lexicale limite le laboratoire à une instruction DML/`WITH` ou au test `WAITFOR`, refuse les commandes serveur, les batches et les noms en trois parties. Ce contrôle n'est jamais considéré comme la frontière principale : les tests contournent volontairement la garde et attaquent directement le login SQL.

## Résultats et validation

Le résultat public contient des métadonnées de colonnes, des cellules normalisées, les effets autorisés, un statut, un temps et un identifiant de diagnostic. Il ne contient aucun message SQL brut. Les validations comparent :

- noms et ordre des colonnes ;
- lignes ordonnées ou ensembles non ordonnés ;
- `NULL` distinct d'une chaîne vide ;
- nombres avec tolérance décimale ;
- effets observés avant rollback.

Les statuts distinguent réussite, refus, timeout, annulation, dépassement de quota, erreur expurgée et indisponibilité. Le mode désactivé n'annonce jamais une validation automatique.

## Matrice d'attaques

| Attaque | Contrôle principal | Preuve attendue |
|---|---|---|
| autre base de session | aucun utilisateur dans l'autre base | permission SQL refusée en connexion directe |
| `CREATE LOGIN`/DDL serveur | aucun rôle ou droit serveur | erreur SQL |
| `xp_cmdshell` | aucun `EXECUTE`, fonctionnalité non accordée | erreur SQL |
| script externe | aucun `EXECUTE`/external scripts | erreur SQL |
| lecture SQLite par `BULK INSERT` | aucun droit bulk et aucun montage de progression | erreur SQL + montages inspectés |
| attente infinie | timeout + `Cancel` | statut `TimedOut` |
| annulation utilisateur | jeton serveur + `SqlCommand.Cancel` | statut `Cancelled` |
| résultat massif | lecture arrêtée au premier dépassement | aucune ligne partielle exposée |
| mutation | transaction systématiquement rollbackée | dataset inchangé |
| fuite de secret/requête | projections minimales et logs structurés | valeur absente des vues et logs |

## Limites

- L'administrateur `sa` reste nécessaire au plan de contrôle pour créer/détruire les bases et logins ; son secret reste exclusivement serveur.
- La racine SQL est écrivable parce que SQL Server refuse ses fichiers système sur `tmpfs`. Elle est la couche jetable du conteneur, sans volume ou bind hôte.
- Le pont loopback existe uniquement pour les vérifications depuis l'hôte Windows. Le service SQL lui-même n'a aucun port publié.
- Docker Desktop, le noyau Linux, SQL Server et le profil seccomp restent dans la base de confiance.
- Aucun contenu SQL/EF initial, score de maîtrise, révision ou procédure OS pédagogique n'est livré en 06A.
