# Analyse statique et avertissements traités

## Objectif observable

À la fin de cette leçon, vous pourrez appliquer la règle à un cas nouveau, expliquer son compromis principal et écrire une preuve qui échoue avec une implémentation plausible mais incorrecte.

## Prérequis

Relire $previousLessonId, disposer du dépôt local et exécuter les exemples sans ressource réseau obligatoire.

## Intuition

Compilateur, analyzers et format détectent des classes de défauts avant exécution. Un avertissement n’est supprimé qu’avec une justification locale vérifiable.

## Explication

Compilateur, analyzers et format détectent des classes de défauts avant exécution. Un avertissement n’est supprimé qu’avec une justification locale vérifiable. Commencez par écrire le contrat, les entrées non fiables, la sortie observable et les limites de responsabilité. Une décision d’architecture n’est retenue que si elle réduit un risque ou rend une preuve plus directe.

## Exemple commenté

Nullable révèle une déréférence possible ; la correction encode l’absence dans le contrat au lieu d’utiliser un opérateur de suppression. Modifiez ensuite une borne et un droit pour vérifier que le raisonnement, et non une valeur mémorisée, détermine le résultat.

## Contre-exemple et erreur fréquente

Désactiver une règle au niveau de la solution pour gagner du temps masque aussi les futurs défauts pertinents. Reproduisez cette erreur dans un test avant de la corriger ; ne masquez ni exception ni code de sortie.

## Vérification de compréhension

Nommez le contrat public, une entrée hostile ou invalide, le statut ou résultat attendu et la preuve qui distingue autorisation, validation et erreur interne.

:::quiz
id=quality-static-analysis-001-check
question=Quelle preuve démontre le mieux la compréhension de cette leçon ?
option=Copier uniquement l’exemple nominal
option=Prédire puis tester succès, frontière et échec pertinent sans exposer de secret
option=Désactiver la règle qui fait échouer la vérification
correct=1
success=Correct : une preuve variée et sûre réfute les erreurs plausibles.
retry=Revenez au contrat, aux frontières et au contre-exemple avant de choisir.
:::

## Exercice guidé

1. Écrivez un scénario nominal, une frontière et un refus.
2. Prédisez statut, corps et effet avant exécution.
3. Implémentez la règle dans le composant responsable.
4. Exécutez la preuve et consignez tout écart sans le masquer.

## Exercice autonome

Transposez la technique au mini-ERP local. Gardez les règles métier hors du transport, bornez les entrées, utilisez seulement des secrets factices et fournissez les commandes de reproduction.

## Débogage

Reproduisez le symptôme, formulez une hypothèse, observez la première divergence sans modifier les données, corrigez la cause puis ajoutez un test de non-régression. Les logs ne contiennent ni corps sensible ni preuve d’authentification.

## Entretien

Présentez en cinq minutes le contrat, le compromis, une erreur fréquente, une menace pertinente et la stratégie de tests. Distinguez clairement ce qui est démontré de ce qui reste manuel.

## Résumé

- Le contrat et les frontières précèdent l’implémentation.
- La sécurité est vérifiée par des refus observables et des journaux sobres.
- Une livraison n’est verte que si toutes les commandes applicables réussissent.

## Cartes de révision

- Question : quelle frontière doit être automatisée ? Réponse : celle qui sépare deux comportements publics différents.
- Question : quelle donnée ne doit jamais entrer dans Git ou les logs ? Réponse : toute preuve d’authentification réelle.

## Test de maîtrise

Sans relire, réalisez une variante avec une donnée et un droit différents. Écrivez un test nominal, deux refus et une preuve de non-régression, puis défendez le compromis. Cette auto-évaluation ne valide aucune maîtrise automatiquement.
