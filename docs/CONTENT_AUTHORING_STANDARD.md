# Standard de rédaction du contenu

Ce document fixe ce qu'un document publié doit contenir pour être considéré comme rédigé, et non
seulement structuré. Il est né d'un constat mesuré : soixante-dix leçons partageaient 70 % de leurs
mots, une seule contenait un bloc de code, et aucun test ne le refusait.

## Principe

> Un contenu est publiable quand il enseigne quelque chose qu'un autre document du catalogue
> n'enseigne pas déjà avec les mêmes phrases.

La structure est vérifiée par les schémas v1. L'authenticité est vérifiée par trois règles
supplémentaires du validateur, décrites plus bas. Les deux sont nécessaires ; aucune ne suffit.

## Leçon de référence

[`content/reference/curriculum/lessons/reference-types-001/`](../content/reference/curriculum/lessons/reference-types-001)
est la leçon modèle. Toute leçon reprise doit atteindre son niveau : notion nommée, code montré,
contre-exemple exécutable, quiz portant sur la notion et test de maîtrise spécifique.

Les **trente leçons des semaines 1 à 10** appliquent ce standard et servent d'exemples de référence
selon le domaine : `linq-lambdas-001` pour une notion de langage, `algo-search-001` pour un invariant
d'algorithme, `sql-joins-001` pour une notion relationnelle, `async-fundamentals-001` pour un sujet à
pièges silencieux. Comptez 1 400 à 1 800 mots et 3 à 5 blocs de code par leçon.

## Contrat d'une leçon

`SafeMarkdownLessonParser` impose **exactement quatorze sections**, dans cet ordre, plus un unique
bloc `:::quiz`. Toute section vide fait échouer la lecture.

| Section | Exigence de fond |
|---|---|
| `Objectif observable` | Ce que l'apprenant saura **produire**, pas ce qu'il aura lu. |
| `Prérequis` | Identifiants réels de leçons, ou connaissances nommées. Jamais de marqueur. |
| `Intuition` | L'idée en deux ou trois phrases, avec une image concrète. |
| `Explication` | 600 à 1 200 mots propres à la notion. Recopier l'intuition est refusé. |
| `Exemple commenté` | Au moins un bloc ` ```csharp ` réel, compilable, commenté. |
| `Contre-exemple et erreur fréquente` | Le code **fautif**, le symptôme observable, puis la correction. |
| `Vérification de compréhension` | La question de contrôle, suivie du bloc `:::quiz`. |
| `Exercice guidé` | Étapes numérotées rattachées à un exercice réel de `content/reference/exercises/`. |
| `Exercice autonome` | Une transposition, avec les hypothèses à écrire avant le code. |
| `Débogage` | Un ticket plausible, puis symptôme, hypothèse, preuve, prévention. |
| `Entretien` | La question posée à voix haute et ce qui distingue une réponse solide. |
| `Résumé` | Trois à cinq points propres à la leçon. |
| `Cartes de révision` | Question et réponse attendue, spécifiques à la notion. |
| `Test de maîtrise` | L'épreuve sans aide. Rappeler qu'elle ne crée aucune maîtrise automatique. |

### Le quiz

La question porte sur la notion de la leçon. Une bonne réponse et deux erreurs **plausibles** :
une option manifestement absurde n'évalue rien.

```text
:::quiz
id=<identifiant-de-la-leçon>-check
question=…
option=…
option=…
option=…
correct=<index base zéro>
success=… (pourquoi cette réponse est la bonne)
retry=… (quoi relire)
:::
```

### Le code

Chaque bloc doit être exact : il sera lu comme une référence. Préciser le langage
(` ```csharp `, ` ```sql `, ` ```text `). Le HTML brut est refusé par le validateur.

## Contrat d'un exercice

Modèle : [`content/reference/exercises/api-error-status-001/`](../content/reference/exercises/api-error-status-001).
Dix fichiers, dont `starter/Submission.cs`, `solution/Submission.cs`, `tests/runner.json` et les deux
jeux de cas.

Le runner n'exécute qu'une **méthode statique publique pure**, aux huit types autorisés par
`RunnerTypeCatalog` : `bool`, `date`, `decimal`, `dictionary<string,int>`, `int`, `int[]`,
`list<int>`, `string`. Une notion qui ne rentre pas dans ce moule se travaille par son **noyau
décidable** — décider d'un code de statut, normaliser un gabarit d'itinéraire, ouvrir ou non une
porte de déploiement.

| Exigence | Valeur |
|---|---|
| Cas visibles | **3 minimum** |
| Cas cachés | **4 minimum**, faisant varier valeurs, bornes et tailles |
| Indices | 4, progressifs |
| Champs de réflexion | 6 |
| `explanation.md` et `review-cards.md` | propres à l'exercice |
| Question d'entretien | dédiée, référencée par `interviewQuestionId` |

Le starter doit **compiler** et **échouer** au moins un cas : un starter qui passe déjà rend
l'exercice sans objet.

**Exception assumée.** Un exercice dont tous les paramètres sont booléens n'admet que 2ⁿ entrées
distinctes. Les couvrir toutes vaut mieux que d'atteindre un volume en répétant des arguments, ce que
`NoExerciseRepeatsTheSameArguments` interdit. Huit exercices sont dans ce cas.

> **Dette close.** 125 des 135 exercices publiés ne portaient que 2 cas visibles et 2 cachés — la
> moitié de la couverture des dix exercices relus à la main. Ils sont désormais à 3 et 4, soit
> **921 cas** au total contre 570. Le plafond de `EveryExerciseCarriesEnoughCases` est à zéro : tout
> exercice neuf doit naître à 3 visibles et 4 cachés.

## Contrat d'un projet

Un projet porte un dossier, comme une leçon ou un exercice :

```text
projects/<id>/
  project.json      manifeste, jalons, grille
  brief.md          le contrat, énoncé pour un humain
  starter/          squelette remis à l'apprenant
  solution/         corrigé de référence — vérification éditoriale seulement, jamais servi
  <jalon>/tests/    runner.json, visible/cases.json, hidden/cases.json
```

Un projet **guidé** s'arrête à `project.json` et `brief.md` : sa grille s'observe, elle ne s'exécute
pas, et il ne produit aucune preuve. Un projet **vérifiable** déclare en plus `starterPath`,
`maximumSourceFiles` et une `acceptanceSuites` par jalon. Le format de suite est identique à celui
d'un exercice — le même bac à sable l'exécute, sans quota ni surface nouvelle — et le type soumis
s'appelle toujours `Submission`.

`achievementKey` rattache le projet à une exigence de porte. C'est le **contenu** qui déclare ce
qu'il prouve, pas le code : une clé absente signifie « ce projet ne prouve rien ». La clé doit être
exigée par une porte et le projet doit porter des suites, faute de quoi
`ProjectCorrectnessTests.DeclaredAchievementKeysAreRequiredBySomeGateAndBackedBySuites` refuse le lot.

Un projet n'est livré que lorsque **toutes** ses suites passent sur une même soumission. Les jalons
qui ne sont pas mesurables — une défense, un journal de bord — restent dans la grille et sont
signalés comme observés et non mesurés.

## Vérifier un exercice sans Docker

`ExerciseCorrectnessTests` compile en mémoire la solution puis le starter et exécute chaque cas avec
la sémantique exacte du conteneur — mêmes types, même désérialisation, même comparaison par
`JsonNode.DeepEquals`, même traitement de l'exception attendue et de la non-mutation des entrées.

```powershell
dotnet test tests/ForgeDotNet.IntegrationTests --filter FullyQualifiedName~ExerciseCorrectnessTests
```

`ProjectCorrectnessTests` applique le même harnais aux suites d'acceptation des projets : chaque
corrigé de référence passe tous les cas de chaque jalon, chaque starter en échoue au moins un, et
aucun jalon déclaré ne reste sans suite. Comme la porte A repose sur ces suites, une attente fausse
rendrait l'accomplissement soit impossible, soit gratuit.

```powershell
dotnet test tests/ForgeDotNet.IntegrationTests --filter FullyQualifiedName~ProjectCorrectnessTests
```

Ce vérificateur exécute du code de contenu **dans le processus de test**, ce qui est acceptable pour
du code versionné et relu. Il ne remplace jamais le bac à sable Docker pour une soumission
d'apprenant : seul celui-ci offre isolation, quotas et nettoyage garanti.

## Les trois règles d'authenticité

Implémentées dans `src/ForgeDotNet.Infrastructure/Content/ContentAuthenticityAnalyzer.cs`.

| Code | Ce qui est refusé |
|---|---|
| `unsubstituted-placeholder` | `$identifiant`, `$(`, `{{…}}`, `TODO`, `FIXME`, `À COMPLÉTER` dans la prose ou dans une chaîne de manifeste. Les blocs de code et les segments entre accents graves sont exclus de l'analyse : `$"…"` en C# reste légitime. |
| `cloned-content` | Un paragraphe d'au moins douze mots partagé par plus de trois documents du lot. |
| `hollow-lesson` | Une `Explication` qui contient intégralement l'`Intuition`, ou une leçon sans aucun bloc de code clôturé. |

Elles s'appliquent au **lot entier**, pas document par document : c'est ce qui permet de détecter la
recopie.

## Registre de dette

`content/authoring/content-debt.json` liste les documents encore hérités du générateur, avec la
règle tolérée pour chacun. C'est un cliquet, pas une exemption :

- un défaut **non déclaré** refuse le lot ;
- une déclaration **devenue inutile** refuse aussi le lot, ce qui force à retirer la ligne ;
- `ContentAuthenticityTests` refuse toute dette supérieure au plafond figé dans le test.

Régénérer après une reprise éditoriale, puis abaisser le plafond dans le test :

```powershell
dotnet run --project src/ForgeDotNet.Web --no-launch-profile -- `
    --emit-content-debt content/reference content/sql
```

État courant : **zéro document, zéro déclaration**, contre 376 au relevé initial. Le registre est
vide, et cela change ce que le cliquet protège.

Tant qu'il restait une dette, la règle bornait un existant : un défaut nouveau pouvait toujours être
confondu avec un défaut hérité tant que le total ne dépassait pas le plafond. À zéro, cette confusion
n'est plus possible — le premier paragraphe d'au moins douze mots recopié dans plus de trois documents
d'un même lot fait échouer le build, sans qu'aucune ligne ne puisse l'absorber.

Le registre ne sert **jamais** à faire passer du contenu neuf : il ne couvrait que l'existant, son
plafond ne pouvait que descendre, et il a atteint zéro. Le rouvrir demanderait de remonter un plafond
dans `ContentAuthenticityTests`, ce qui reste une décision humaine visible en revue.

Descente, lot par lot : 376 au relevé initial, 164 après la reprise des leçons, des exercices et des
DebugLabs, 159 après les briefs de projet, 131 après les 28 scénarios SQL, 106 après les 50 cartes
d'anglais, **0** après les 191 fiches d'entretien.

## Échelle d'indices

Les quatre indices d'un exercice sont consultés dans l'ordre, et `docs/MASTERY.md` plafonne le score
à 90/80/70/**60** selon celui qui a été atteint. Le niveau 4 est donc la dernière marche avant le
déverrouillage de la solution, qui met la pratique de l'exercice à zéro.

D'où trois exigences, vérifiées par `ExerciseHintQualityTests` :

| Exigence | Pourquoi |
|---|---|
| Les quatre indices d'un exercice sont deux à deux distincts | Une marche qui répète la précédente ne fait pas progresser. |
| Aucun texte d'indice ni jeu d'erreurs fréquentes n'est partagé par plus de trois exercices | Même seuil que `cloned-content` : au-delà, le texte est recopié, donc il ne décrit plus ce problème-ci. |
| Aucun indice ne recopie une ligne de la solution | Sinon le plafond de score à 60 équivaut à un déverrouillage gratuit. |

Le niveau 4 est un **pseudocode partiel** : les étapes réelles, dans l'ordre, nommant les vrais
paramètres et la borne propre à l'exercice — jamais un corps de méthode compilable. Aucune valeur de
cas caché n'y figure : écrire « les cas cachés déplacent la borne haute », jamais la valeur.

`scripts/Repair-ExerciseHints.ps1` applique une reprise par splice textuel et refuse d'écrire un
indice qui recopie une ligne de la solution ou qui sort des bornes du schéma.

## Matériel de diagnostic

Un DebugLab n'entraîne au diagnostic que si le ticket décrit un **symptôme** — ce qui est observé,
sur quelle entrée, ce qui était attendu — et jamais la cause. Les règles et leur vérification sont
décrites dans [`DEBUGLAB.md`](DEBUGLAB.md) ; `scripts/Repair-DebugScenarios.ps1` applique une reprise
sans jamais toucher `broken/`, `correction/` ni les cas de test.

## Échafaudage

`scripts/New-S1S10Content.ps1`, `New-S11S20Content.ps1` et `New-S21S24Content.ps1` produisent la
structure et les données dérivables — signatures, cas de test, exemples calculés — puis laissent des
marqueurs `TODO:` là où une rédaction humaine est nécessaire. Ces marqueurs sont refusés par
`unsubstituted-placeholder` : un lot échafaudé mais non rédigé ne peut pas être publié.

Ces scripts **ne réécrivent jamais un fichier existant** sans `-Force`. Une leçon reprise à la main
ne peut donc pas être détruite par une réexécution.

## Vérifier avant de proposer une reprise

```powershell
dotnet run --project src/ForgeDotNet.Web --no-launch-profile -- --validate-content content/reference
dotnet test tests/ForgeDotNet.IntegrationTests --filter FullyQualifiedName~ContentAuthenticityTests
```

Puis ouvrir la leçon dans le lecteur : `dotnet run --project src/ForgeDotNet.Web`, puis
`/learn/<identifiant>`. Une leçon qui se valide mais se lit mal n'est pas terminée.
