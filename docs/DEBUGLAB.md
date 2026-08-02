# DebugLab

L’incrément 05 a fourni huit scénarios initiaux ; l’incrément 08 porte la banque S1–S7 à 25 scénarios. Tous imposent le même cycle complet : reproduire, observer, formuler une hypothèse, corriger, écrire un test de non-régression, démontrer la cause racine et documenter une prévention. Une réussite du runner seule ne termine jamais un scénario.

## Machine d’états

```text
InvestigationRequired
  -- journal symptom/context/hypotheses/evidence
     + Breakpoint/Watch/Locals/Call Stack --> CorrectionReady
CorrectionReady
  -- justification + test écrit + runner en échec --> CorrectionReady
  -- justification + test écrit + runner réussi --> RootCauseRequired
  -- 2 corrections échouées + demande explicite --> SolutionViewed
RootCauseRequired
  -- cause + prévention conformes à la grille --> Completed
```

`SolutionViewed` est terminal et explicitement non terminé. Aucun état n’attribue de score de maîtrise, de révision ou de progression SQL.

## Journal et évaluation

`BugJournalEntry` conserve exactement les huit champs v1 : `symptom`, `context`, `hypotheses`, `evidence`, `cause`, `fix`, `test`, `prevention`. Les quatre observations de débogueur sont obligatoires avant la première correction. Domain borne et valide les textes ; Application recharge l’état courant et sa révision avant chaque transition.

La grille privée de chaque scénario porte sur les champs `cause`, `evidence`, `test` ou `prevention`. Son évaluation est déterministe, insensible à la casse et aux accents. Tous les critères doivent réussir. La grille ne quitte pas le serveur et ne représente pas un score de maîtrise.

L’export Markdown contient le journal et les quatre observations. Il exclut le code soumis, la correction protégée, les tests cachés et les chemins privés.

## Contenu initial

| Scénario | Défaut réel | Preuve de non-régression |
|---|---|---|
| `debug-null-reference-001` | déréférence `null` par `Trim` | absence, blanc, valeur nominale |
| `debug-condition-001` | priorité incorrecte des branches | standard/express autour du seuil |
| `debug-loop-001` | borne `<= Length` | valeur absente et tableau vide |
| `debug-conversion-001` | `int.Parse` sur saisie attendue invalide | texte, négatif, dépassement |
| `debug-date-001` | soustraction de dates inversée | échéance et changements de période |
| `debug-linq-001` | tri ascendant des meilleurs scores | ordre, limite et entrée inchangée |
| `debug-async-001` | agrégation avant achèvement des tâches | toutes les tâches contribuent |
| `debug-di-001` | mauvaise implémentation enregistrée | comportement de l’interface résolue |

Chaque dossier sous `content/reference/debugging/` contient un manifeste v1, un ticket, des logs expurgés, un `broken/Submission.cs`, une correction privée, une consigne de non-régression, une grille privée et six cas runner, dont trois cachés. Le test `DebugLabDockerRunnerTests` prouve que chaque version cassée échoue et que chaque correction réussit.

## Persistance

SQLite conserve `DebugLabActivities` et `DebugCorrectionAttempts`. Une activité est unique par profil et scénario. Les tentatives sont append-only et protégées par une version attendue. Le code soumis n’est jamais persisté : seule son empreinte SHA-256, le résultat, les compteurs, la référence de diagnostic et l’horodatage sont enregistrés.

La révision SHA-256 couvre le manifeste, le ticket, les logs, les versions cassée/corrigée, la consigne, la grille et les cas runner. Une activité ouverte sur une autre révision est refusée au lieu d’être réinterprétée.

## Sécurité

- Les chemins restent descendants de `content/`, les traversals et points de réanalyse sont refusés, les fichiers sont bornés et lus en UTF-8 strict.
- Les logs initiaux contenant chemin hôte, chemin interne du runner, jeton ou marqueur de donnée sensible sont refusés.
- Le navigateur ne reçoit ni correction, ni grille, ni cas caché avant la transition autorisée.
- Une requête runner contient seulement l’identifiant/version/révision et un `Submission.cs` validé ; aucune commande, aucun chemin arbitraire ou projet du dépôt n’est accepté.
- Le runner Docker réutilise la politique 04C : image immuable, réseau nul, racine en lecture seule, utilisateur non-root, seccomp, quotas, délais, cas cachés chiffrés et nettoyage prouvé.
- Les sorties sont expurgées ; les noms cachés, chemins internes et sources ne sont pas journalisés.

## Routes

- `/debug-lab` liste les 25 scénarios ;
- `/debug-lab/{scenarioId}` affiche le cycle, le journal, le runner et l’export.

Les mutations passent par Blazor interactif et l’antiforgery ASP.NET. Web orchestre `DebugLabService` ; Domain décide des transitions, Infrastructure charge/persiste, CodeRunner exécute hors du processus Web.

## Vérification manuelle

1. Échantillonner chaque semaine S1–S7 et confirmer que les 25 scénarios exposent chacun un défaut reproductible, une checklist et les observations requises.
2. Tenter une correction avant l’investigation, puis sans test : les deux actions doivent être refusées.
3. Exécuter une correction erronée : le journal reste lisible et l’état revient à `CorrectionReady`.
4. Exécuter la correction attendue en mode Docker : les six cas passent, mais la cause racine reste requise.
5. Fournir une cause vague puis une cause étayée : seule la seconde termine le cycle.
6. Après deux corrections échouées, vérifier que la solution devient disponible sur demande, que les tests cachés restent absents et que l’état reste non terminé.
7. Exporter le journal et vérifier l’absence de code soumis, correction, chemins et cas cachés.
8. Redémarrer l’application et vérifier la restauration du journal, des tentatives et de l’évaluation.

## Commandes

```powershell
dotnet build ForgeDotNet.sln --no-restore
dotnet test tests/ForgeDotNet.UnitTests --no-build --filter "FullyQualifiedName~DebugLab"
dotnet test tests/ForgeDotNet.IntegrationTests --no-build --filter "Category=DebugLabRunner"
dotnet test tests/ForgeDotNet.EndToEndTests --no-build --filter "FullyQualifiedName~DebugLab"
dotnet run --project src/ForgeDotNet.Web --no-build -- --validate-content content/reference
dotnet format ForgeDotNet.sln --no-restore --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

## Limites

La banque contient 25 scénarios complets S1–S7. Elle n’intègre ni IDE distant, ni SQL Lab dans DebugLab, ni logique spécifique par scénario dans le moteur. Les preuves runner cassé/corrigé restent obligatoires.
