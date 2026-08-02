# Contrats de contenu JSON v1

## Portée

Les huit schémas canoniques de `content/schemas/` décrivent les manifestes de leçon, exercice, parcours, débogage, SQL, entretien, anglais et projet. Ils utilisent JSON Schema Draft 2020-12, restent hors ligne et n'emploient aucune référence externe. Le validateur de l'incrément 02A vérifie les contrats et les fichiers locaux ; il ne charge pas de catalogue.

| Type | Schéma | Convention de chemin du manifeste |
|---|---|---|
| Leçon | `lesson.schema.json` | `curriculum/lessons/{id}/lesson.json` |
| Exercice | `exercise.schema.json` | `exercises/{id}/exercise.json` |
| Parcours | `curriculum.schema.json` | `curriculum/{id}.json` |
| Débogage | `debug.schema.json` | `debugging/{id}/scenario.json` |
| SQL | `sql.schema.json` | `sql/{id}/scenario.json` |
| Entretien | `interview.schema.json` | `interviews/{id}.json` |
| Anglais | `english.schema.json` | `english/{id}.json` |
| Projet | `project.schema.json` | `projects/{id}.json` |

Les mêmes conventions s'appliquent sous `content/fixtures/`. Le nom canonique porté par le chemin doit être identique à `id`.

## Règles communes

- `schemaVersion` vaut exactement `1`. Une rupture de structure exigera une nouvelle version de schéma et un diagnostic explicite.
- `version` est un entier positif. Une correction éditoriale l'incrémente sans modifier l'identifiant.
- `id` est stable, en minuscules ASCII, avec segments séparés par `-` ou `.` ; il est unique dans un lot validé.
- Les objets refusent les propriétés inconnues et les champs obligatoires ne peuvent pas être omis.
- Les textes, tableaux, durées, difficultés, quotas et pondérations sont bornés par le schéma de leur type.
- Les pondérations de compétences d'une leçon et celles de la rubrique d'un projet doivent totaliser exactement `1`.
- La licence est obligatoire ; l'attribution est facultative lorsque le contenu est original et obligatoire éditorialement lorsqu'une source l'exige.
- Une leçon v1 utilise un manifeste JSON latéral `lesson.json` qui référence son Markdown par `markdownPath`. Le front matter YAML n'est pas un format canonique v1.

## Règles pédagogiques structurantes

- Une leçon déclare exactement les douze sections prévues, dans l'ordre défini par le schéma.
- Un exercice déclare exactement les six champs de réflexion et quatre indices ordonnés : socratique, localisation, stratégie et pseudocode partiel.
- Les chemins du squelette, des tests visibles, des tests cachés, de la solution et de l'explication sont distincts et obligatoires.
- Un scénario de débogage contient les huit champs du journal : symptôme, contexte, hypothèses, preuves, cause, correction, test et prévention.
- Un scénario SQL borne le temps et le nombre de lignes, épingle son image par digest et décrit résultat et effets attendus. L'exécution SQL n'appartient pas à 02A.
- Entretien et anglais exigent critères observables, réponse modèle, erreurs fréquentes et variantes.
- Un projet exige jalons, preuves, critères, rubrique pondérée et la politique interdisant de livrer une solution complète avant soumission.

## Validation et atomicité

Depuis la racine du dépôt :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/validate-content.ps1 content/fixtures/valid
```

La commande retourne :

- `0` lorsque tout le lot est valide ;
- `1` lorsque toutes les erreurs ont été agrégées et qu'aucun document du lot n'est accepté ;
- `2` lorsque la syntaxe de commande ou la racine du dépôt est invalide.

Chaque diagnostic contient le chemin relatif, la propriété JSON, un code stable et un message français. Exemple :

```text
fixtures/invalid/path-traversal/exercises/path-traversal/exercise.json | $.statement | path-traversal | Les segments '.' et '..' sont interdits dans un chemin de contenu.
```

Les fixtures positives couvrent les huit types. Les fixtures négatives couvrent notamment champ absent, type ou enum invalide, ID dupliqué, versions invalides, pondération hors bornes, traversal, section manquante et indice manquant.

## Sécurité des fichiers

- Le dossier demandé, les schémas et chaque chemin référencé sont canonicalisés et doivent rester sous la racine `content/`.
- Les chemins absolus, URI, segments `.` ou `..`, liens symboliques et points de réanalyse sont refusés.
- Les fichiers référencés doivent exister avec le type attendu ; JSON et Markdown sont lus en UTF-8 strict.
- La profondeur JSON est limitée à 64, un fichier à 256 Kio et un lot à 10 000 fichiers par défaut.
- Le HTML brut est refusé dans le Markdown. Le validateur ne restitue jamais les valeurs de contenu dans ses diagnostics.
- Les répertoires de solution et de tests cachés sont validés côté serveur uniquement ; aucune réponse HTTP ou UI n'est ajoutée par cet incrément.

## Validations différées sans ambiguïté

L'incrément 02B vérifie désormais les références représentables par les huit schémas, les graphes de prérequis, les cycles et le chargement atomique du catalogue. `reviewCards` reste différé faute de type carte v1. Les incréments de pratique et de runner vérifieront la compilation et l'exécution des exercices. Aucun lecteur UI ni exercice exécutable n'est fourni par 02A/02B.
