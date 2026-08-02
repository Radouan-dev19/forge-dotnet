# Async simple, ordre et annulation

## Objectif observable

À la fin de la leçon, vous pourrez expliquer le compromis principal, appliquer la règle sur une entrée nouvelle et écrire un test qui distingue le comportement correct d’une erreur plausible.

## Prérequis

Relire la leçon précédente $previousLessonId et savoir exécuter un exemple local sans réseau.

## Intuition

Une Task représente un travail futur. await conserve les exceptions et l’ordre logique sans bloquer un thread avec Wait ou Result.

## Explication

Une Task représente un travail futur. await conserve les exceptions et l’ordre logique sans bloquer un thread avec Wait ou Result. La règle doit rester visible dans le nom des opérations, les bornes et les erreurs. Avant de coder, notez l’entrée, la sortie, les invariants et ce qui doit être refusé.

## Exemple commenté

Une méthode accepte CancellationToken, le transmet et laisse OperationCanceledException signaler l’annulation attendue. L’exemple est volontairement petit : changez une borne et une valeur absente pour vérifier que le raisonnement, et non la donnée mémorisée, produit le résultat.

## Contre-exemple et erreur fréquente

Lancer une tâche sans l’attendre peut perdre son exception et terminer avant son effet. Le contre-exemple doit être reproduit par un test qui échoue avant correction et réussit après.

## Vérification de compréhension

Expliquez en deux phrases la précondition, l’invariant et le cas limite principal. Si l’un des trois manque, revenez à l’explication avant de poursuivre.

:::quiz
id=async-fundamentals-001-check
question=Quelle preuve montre que la règle de cette leçon est comprise ?
option=Répéter uniquement l’exemple mot pour mot
option=Prédire puis tester un cas nominal, une borne et une erreur plausible
option=Lire la solution sans écrire de test
correct=1
success=Correct : la variation des données et la borne distinguent une règle comprise d’un exemple mémorisé.
retry=Revenez au contrat, à l’invariant et au contre-exemple, puis choisissez la preuve qui pourrait réellement échouer.
:::

## Exercice guidé

1. Écrivez trois cas : nominal, borne et entrée invalide ou absente.
2. Prédisez chaque résultat sans exécuter.
3. Implémentez la règle minimale.
4. Comparez les résultats et nommez toute hypothèse incorrecte.

## Exercice autonome

Transposez la règle à un petit domaine de commandes. Conservez une signature testable, refusez les états impossibles et justifiez la complexité en fonction du volume d’entrée.

## Débogage

Reproduisez d’abord le contre-exemple. Placez un breakpoint à la première divergence, inspectez les données sans les modifier, puis consignez symptôme, hypothèse, preuve, cause, correction et test de non-régression.

## Entretien

Présentez le compromis à voix haute en cinq minutes : définition, exemple, erreur fréquente, méthode de test et situation où vous choisiriez une autre approche.

## Résumé

- Le contrat et les bornes précèdent l’implémentation.
- Une règle utile est observable par un test qui pourrait échouer.
- Une erreur n’est corrigée qu’après reproduction et preuve.

## Cartes de révision

- Question : quel invariant protège cette technique ? Réponse attendue : le candidat doit citer l’invariant décrit dans l’explication.
- Question : quel test réfute l’erreur fréquente ? Réponse attendue : un cas limite qui échoue avant la correction.

## Test de maîtrise

Sans relire, résolvez une variante avec une borne différente, écrivez un test nominal et deux cas limites, puis expliquez la complexité et la preuve de non-régression. Cette auto-évaluation ne crée aucune maîtrise automatique.
