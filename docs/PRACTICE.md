# Pratique manuelle

Le protocole de pratique reste traçable et indépendant de tout score de maîtrise. Depuis 04D, dix exercices C# S1–S2 peuvent aussi être compilés et testés par le runner Docker isolé. Le contenu privé est chargé côté serveur ; le navigateur ne reçoit que l'énoncé public, le starter, les indices déjà consultés et, après déverrouillage, la solution explicitement demandée.

## Machine d'états

```text
ReflectionRequired
  -- réflexion complète --> ReflectionRequired (réflexion enregistrée)
  -- premier indice/tentative --> Attempting (réflexion figée)
Attempting
  -- H1, H2, H3, H4 dans l'ordre --> Attempting
  -- tentative --> Attempting
  -- 2 tentatives sérieuses + délai serveur + demande explicite --> SolutionViewed
SolutionViewed
  -- explication personnelle et variante distinctes --> PostSolutionCompleted
```

`SolutionViewed` et `PostSolutionCompleted` restent non maîtrisés. L'incrément ne calcule aucun score, aucune preuve automatique et aucune révision.

## Réflexion et tentative sérieuse

Avant toute aide ou tentative, les six champs suivants sont obligatoires : reformulation, entrées, sortie attendue, cas limites, hypothèse et plan. Les minimums sont contrôlés dans Domain et les valeurs sont bornées à 4 000 caractères. La réflexion devient immuable au premier indice ou à la première tentative.

Une proposition doit contenir entre 20 et 20 000 caractères. Elle ne compte comme sérieuse qu'à partir de 80 caractères, avec une attestation manuelle et des observations de vérification d'au moins 20 caractères. La comparaison textuelle normalise le texte et signale un doublon exact ou une similarité lexicale très élevée ; ce contrôle limite un contournement évident sans prétendre détecter toute triche. Chaque tentative, y compris non sérieuse, reste dans l'historique avec sa décision et sa comparaison.

## Indices et solution

Les quatre indices sont consultables uniquement dans l'ordre H1 à H4. Chaque consultation est persistée une seule fois. Le prochain indice et la solution restent absents de la projection Web tant qu'ils ne sont pas autorisés.

La solution exige deux tentatives sérieuses distinctes puis le délai configuré dans le manifeste de l'exercice, calculé à partir de la première tentative sérieuse avec le `TimeProvider` serveur. L'interface affiche l'éligibilité et permet de la rafraîchir, mais ne reçoit ni échéance fournie par le client ni moyen de contourner le délai. Après consultation, une explication personnelle et une variante distinctes de la solution et l'une de l'autre sont demandées.

## Persistance et concurrence

SQLite conserve quatre ensembles : `PracticeActivities`, `PracticeReflections`, `PracticeAttempts` et `PracticeHintUsages`. L'activité est unique par profil et exercice. Les historiques de tentatives et d'indices sont append-only ; leur réécriture ou suppression est refusée. Une version attendue protège chaque mutation, et le coordinateur sérialise les doubles clics dans le processus. Une requête concurrente périmée échoue explicitement au lieu de produire un doublon.

La révision privée du contenu est figée avec l'activité. Si les fichiers de l'exercice changent, l'ancienne activité n'est pas silencieusement rejouée avec une autre solution.

## Routes

- `/practice` liste les exercices publiés compatibles avec le protocole manuel ;
- `/practice/{exerciseId}` présente la réflexion, les indices, les tentatives, le verrou de solution et l'historique.

Les mutations passent par les événements Blazor interactifs et l'antiforgery ASP.NET. Web orchestre uniquement `PracticeService` ; les transitions restent dans Domain et la persistance dans Infrastructure.

La page d'exercice présente compilation et tests séparément via `RunExercise`. Le starter C# est préchargé seulement lorsqu'aucune tentative n'existe. Le mode par défaut reste manuel : il explique qu'aucune validation automatique n'a eu lieu et permet d'exporter un zip contenant seulement les sources et métadonnées publiques. Le mode déterministe reste réservé aux tests/démonstrations. Le mode Docker utilise l'adaptateur isolé uniquement lorsqu'une suite serveur approuvée correspond exactement à l'exercice. Aucun résultat de runner ne modifie l'activité, n'attribue une tentative sérieuse ou ne produit une maîtrise. L'historique, limité à vingt résultats sans code soumis, est volatil.

## Sécurité

- Les chemins de solution et de tests cachés ne font partie d'aucune vue applicative ou réponse Web.
- La source privée confine les fichiers sous la racine de contenu, refuse traversal et points de réanalyse, impose UTF-8 strict et une taille maximale.
- La solution et les indices non consultés restent côté serveur ; toutes les transitions sont revérifiées depuis l'état SQLite courant.
- Les identifiants internes d'activité, tentative et consultation sont des GUID aléatoires et ne sont pas exposés dans les routes.
- La proposition, les observations, l'explication et la solution ne sont jamais journalisées.
- Le mode Compose est explicitement `Manual` et ne monte pas le socket Docker. En mode Docker local, le code est compilé et testé hors du processus Web dans une image immuable ; aucune commande ou suite ne vient du navigateur.
- L'export manuel n'est jamais présenté comme une preuve automatique. Les résultats Docker de 04C ne deviennent ni tentative sérieuse, ni preuve de maîtrise, ni score.

## Vérification manuelle

1. Ouvrir `/practice`, choisir un exercice et confirmer l'absence d'indice et de solution.
2. Vérifier qu'aucun indice n'est proposé avant une réflexion complète, puis renseigner les six champs.
3. Consulter H1 à H4 et vérifier l'ordre, l'horodatage et l'absence de cinquième niveau.
4. Enregistrer une tentative incomplète, puis deux tentatives sérieuses distinctes et vérifier l'historique.
5. Confirmer que la solution reste verrouillée avant la fin du délai serveur, même après actualisation.
6. Après le délai, consulter la solution et vérifier le libellé « solution consultée — activité non maîtrisée ».
7. Enregistrer une explication personnelle et une variante, puis confirmer que l'état final reste non maîtrisé.
8. Parcourir les contrôles au clavier et vérifier la mise en page étroite.

## Commandes

```powershell
dotnet build ForgeDotNet.sln --no-restore
dotnet test ForgeDotNet.sln --no-build --no-restore
dotnet format ForgeDotNet.sln --verify-no-changes --no-restore
powershell -ExecutionPolicy Bypass -File scripts/validate-content.ps1 content/reference
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

## Limites après 04D

Le lot de pratique exécutable reste strictement limité aux dix exercices S1–S2 répertoriés dans `CONTENT_S1_S2_MATRIX.md`. Les deux exercices historiques de référence restent utiles au protocole manuel mais n'ont pas de suite Docker 04D. DebugLab est disponible séparément sur `/debug-lab` et documenté dans `docs/DEBUGLAB.md`. Il n'existe encore ni contenu S3+, score de maîtrise, planification de révision ou laboratoire SQL.
