# Objectif observable

Retourner une page stable de trois commandes après un curseur composé.

Le curseur représente `(CreatedAtUtc = 2026-07-02T09:00:00, OrderId = 3)`. Retourne les trois lignes suivantes selon l’ordre `(CreatedAtUtc, OrderId)` : commandes `4`, `5`, `6`.

Le prédicat doit exprimer l’ordre lexicographique : date strictement supérieure, ou même date avec identifiant supérieur. Trie sur les deux colonnes et limite à trois lignes.

Une pagination par offset seule peut déplacer les pages lorsqu’une ligne antérieure est insérée. Le test négatif utilise un offset incorrect ; un contrôle manuel ajoute aussi une ligne avant le curseur et vérifie que la page keyset ne change pas.
