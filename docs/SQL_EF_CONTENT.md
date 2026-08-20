# Contenu SQL/EF initial

Ce document décrit le lot initial livré par 06B puis étendu par 08. Il complète le contrat technique de `SQLLAB.md` sans modifier les garanties d’isolation du moteur 06A. Les 12 scénarios initiaux ci-dessous restent stables ; 28 scénarios SQL supplémentaires portent la banque à 40, selon la matrice détaillée `CONTENT_S1_S10.md`. Leurs contrats d'acceptation restent exclusivement côté serveur sous `tests/contract.json`. L’examen 4 réutilise une session SqlLab jetable pour six requêtes et exécute dans le CodeRunner ses candidats EF Core — deux scénarios à dossier `exam/` et, depuis le lot de densité, les trois exercices `ef-*` du catalogue ; ses attentes et suites privées sont décrites dans `EXAMS_DASHBOARD.md`.

## Matrice des scénarios

| Ordre | Identifiant | Semaine | Famille | Mode | Difficulté | Durée |
|---:|---|---:|---|---|---:|---:|
| 1 | `sql-orders-join-001` | S8 | jointure, filtres et contraintes | SQL | 2 | 35 min |
| 2 | `sql-orders-aggregate-001` | S9 | agrégations, groupement et `HAVING` | SQL | 2 | 40 min |
| 3 | `sql-orders-subquery-001` | S9 | sous-requête, `NOT EXISTS` et `NULL` | SQL | 3 | 35 min |
| 4 | `sql-orders-cte-001` | S9 | CTE et agrégat mensuel | SQL | 3 | 40 min |
| 5 | `sql-orders-transaction-001` | S9 | transaction, isolation et mise à jour atomique | SQL | 4 | 45 min |
| 6 | `sql-orders-index-plan-001` | S10 | index, sargabilité et plan | SQL | 4 | 50 min |
| 7 | `sql-orders-pagination-001` | S10 | ordre total et pagination keyset | SQL | 3 | 40 min |
| 8 | `ef-orders-migrations-001` | S10 | `DbContext` et migrations | EF Core | 3 | 50 min |
| 9 | `ef-orders-tracking-001` | S10 | tracking et `AsNoTracking` | EF Core | 2 | 35 min |
| 10 | `ef-orders-queryable-001` | S10 | composition `IQueryable` et traduction serveur | EF Core | 3 | 40 min |
| 11 | `ef-orders-loading-001` | S10 | chargement lié et détection N+1 | EF Core | 3 | 45 min |
| 12 | `ef-orders-concurrency-001` | S10 | concurrence optimiste et `rowversion` | EF Core | 4 | 50 min |

La chaîne de prérequis suit exactement cet ordre. Elle est complète, sans cycle et ne référence aucun contenu postérieur à S10.

## Organisation d'un scénario

Chaque dossier sous `content/sql/` contient :

- `scenario.json`, manifeste public conforme à `sql.schema.json` ;
- `dataset.sql`, données déterministes et non sensibles ;
- `schema.sql`, schéma visible par l'apprenant ;
- `reset.sql`, remise à zéro idempotente vérifiée ;
- `statement.md`, consigne et critères observables ;
- `solution.md`, solution expliquée, non destinée à la projection publique ;
- `tests/contract.json`, résultats attendus et variante négative réservés au serveur.

Les cinq scénarios EF ajoutent `starter/` et `solution/`. Le modèle partagé `content/sql/support/MiniErpContext.cs` garde l'exemple EF isolé de la persistance SQLite de Forge.NET. Le starter compile et illustre un défaut pédagogique réel ; il n'est jamais présenté comme une solution fonctionnelle.

## Datasets et remise à zéro

Les datasets utilisent uniquement des clients, commandes, produits, stocks et lignes de commande fictifs. Les identifiants, dates et montants sont fixes. Le scénario de plan produit 20 000 lignes par génération SQL déterministe afin de rendre l'usage de l'index observable sans dépendre d'un coût exact.

Chaque test crée une base et un login aléatoires, charge son dataset, exécute la solution avec les droits déclarés, introduit ensuite une altération contrôlée, applique `reset.sql`, puis vérifie un invariant métier. La base et le login sont supprimés même en cas d'échec ; si l'exécution et le nettoyage échouent ensemble, les deux erreurs sont conservées.

Le scénario migrations reçoit temporairement uniquement `CREATE TABLE`, `ALTER` sur le schéma `dbo`, `REFERENCES` et `VIEW DEFINITION`. Le compte reste hors de `sysadmin` et `db_owner`, ne peut administrer ni les logins ni les bases et reçoit un refus explicite de prise de possession. Tous les autres scénarios utilisent strictement les droits DML déclarés dans leur manifeste.

## Contrats de validation

Les sept scénarios SQL vérifient la solution nominale, une formulation SQL différente mais équivalente et une requête incorrecte. Les assertions portent sur les noms et l'ordre des colonnes, l'ordre des lignes quand il est significatif, les valeurs normalisées, la tolérance numérique, les effets transactionnels et le reset.

Les contrôles spécialisés sont :

- transaction : décrément atomique observé dans une transaction de test puis rollback ;
- index : présence de `IX_Orders_CustomerId_CreatedAt` et d'un `Index Seek`, sans coût exact ;
- pagination : résultat keyset stable après insertion concurrente contrôlée ;
- migrations : historique EF présent, application idempotente et différence avec `EnsureCreated` ;
- tracking : identité d'instance avec tracking et détachement avec `AsNoTracking` ;
- `IQueryable` : filtre paramétré dans la commande SQL, et détection de l'énumération prématurée ;
- chargement : une commande pour la solution et quatre pour la variante N+1 ;
- concurrence : conflit `rowversion` géré par la solution et exception non gérée par le starter.

La catégorie `SqlEfContent` contient 41 tests : un contrôle structurel, 35 exécutions SQL et cinq exécutions EF réelles. Chaque scénario possède ainsi au moins une preuve négative utile, un reset et une base/login jetables.

## Vérification locale

Depuis PowerShell à la racine du dépôt :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start-sql-lab.ps1 -NoBuild
dotnet run --project src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --no-build --no-launch-profile -- --validate-content content/sql
dotnet run --project src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --no-build --no-launch-profile -- --load-catalog content/sql --search commandes --skill sql.join
dotnet test tests/ForgeDotNet.IntegrationTests/ForgeDotNet.IntegrationTests.csproj --no-build --filter "Category=SqlEfContent"
powershell -ExecutionPolicy Bypass -File scripts/stop-sql-lab.ps1
```

La vérification de référence restaure, compile, démarre et arrête elle-même SqlLab, exécute tous les tests, valide le lot et son chargement dans le catalogue, puis contrôle le formatage :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

Un échec de validation, d'exécution, de contre-exemple, de reset ou de nettoyage invalide le lot. La dépendance `Microsoft.EntityFrameworkCore.SqlServer` est réservée aux tests d'intégration et aux sources pédagogiques compilées par ce projet ; elle ne crée pas de nouvelle dépendance du moteur SqlLab.

## Revue éditoriale 06B

Les 40 consignes ont un objectif observable, un vocabulaire défini, des critères bornés, un résultat testable, une solution distincte et une erreur fréquente démontrée. Elles progressent du modèle relationnel vers EF Core et restent réalisables hors ligne. Toute correction future doit incrémenter la version du manifeste et préserver l'identifiant stable.
