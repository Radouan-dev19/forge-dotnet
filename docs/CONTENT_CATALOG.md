# Catalogue de contenu

## Portée

Le catalogue 02B charge un lot déjà conforme aux schémas v1, résout ses références, refuse les cycles puis publie un snapshot immuable en mémoire. Il ne rend pas le Markdown, ne persiste rien dans SQLite et n'ajoute aucune page Web.

Le catalogue reproductible se trouve sous `content/reference/`. À la validation de l’incrément 08, il contient 231 documents : un parcours S1–S10, 30 leçons, 85 exercices C#/algo, 25 DebugLabs, 84 questions d’entretien liées, cinq mini-projets et une activité d’anglais historique. Chaque exercice possède starter, solution et suites visibles/cachées ; chaque DebugLab possède version cassée, correction et non-régression. Ces fichiers privés ne rejoignent jamais la projection publique du catalogue.

## Contrats et index

`ContentCatalog` expose :

- un identifiant de révision SHA-256 calculé sur les manifestes triés ;
- une collection stable et en lecture seule de `ContentCatalogItem` ;
- un accès exact par ID ;
- des index immuables par type et compétence ;
- une recherche sur titre, résumé public et glossaire.

La projection publique contient seulement ID, version, type, titre, résumé, compétences, prérequis et glossaire. Elle exclut réponses modèles, indices, solutions, chemins de correction et chemins/contenu des tests cachés.

La recherche décompose les accents, ignore la casse, exige tous les termes et conserve un ordre stable `titre normalisé, ID`. Les filtres type et compétence ne modifient pas cet ordre.

## Références validées

Le chargeur refuse avant publication :

- prérequis de contenu absents ;
- leçons et exercices absents ou de mauvais type dans un module de parcours ;
- modules prérequis absents dans leur parcours ;
- variante d'exercice ou de projet absente, mal typée ou autoréférencée ;
- question d'entretien absente ou de mauvais type ;
- ID de module dupliqué ;
- cycles directs ou indirects entre contenus et entre modules.

`reviewCards` reste un identifiant syntaxiquement validé mais non résolu en 02B : aucun type « carte » ne fait partie des huit schémas v1 gelés. Cette référence devra devenir stricte avec le schéma des cartes, sans affaiblir les références déjà contrôlables.

## Chargement atomique

Le flux est le suivant :

1. validation complète du dossier par le validateur 02A ;
2. lecture des manifestes dans une zone logique privée ;
3. seconde validation pour détecter une modification concurrente ;
4. construction des projections, index et graphes ;
5. résolution des références et détection des cycles ;
6. calcul de la révision ;
7. publication par échange atomique d'une référence unique.

Les rechargements sont sérialisés. Les lecteurs observent l'ancien ou le nouveau snapshot complet, jamais un état intermédiaire. Tout échec conserve exactement l'instance précédemment publiée.

## Commandes

Charger et rechercher le catalogue minimal depuis la racine du dépôt :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/load-catalog.ps1 content/reference -Search evaluer -Skill csharp.types
```

Vérifier le refus d'un rechargement invalide et la conservation du snapshot :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-catalog.ps1
```

La CLI `--load-catalog` retourne `0` après chargement, `1` après refus de contenu et `2` pour une syntaxe invalide. Elle journalise seulement révision, nombres de documents/types/résultats, durée et diagnostics sans valeurs sensibles.

## Base de validation locale

Le lot 08 validé contient 231 documents et 1 262 fichiers sous `content/reference/`. Les 40 scénarios SQL/EF et les quatre banques d’examen restent dans leurs sources spécialisées. Ces nombres sont contrôlés par la matrice `CONTENT_S1_S10.md`, sans devenir un SLA implicite.

## Limites explicites

Le catalogue public ne sert aucune solution ni suite de tests ; seules les sources serveur de Practice et du runner lisent ces fichiers privés après contrôle de confinement et de révision. SQL/EF reste chargé par sa source spécialisée sous `content/sql/`, et les examens par `content/exams/`. La banque 4 expose seulement ses énoncés et starters : attentes SQL, requêtes solutions, solutions EF et cas cachés restent dans les sources serveur confinées.
