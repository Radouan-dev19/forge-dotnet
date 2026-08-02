# Objectif observable

Décomposer un calcul mensuel avec une CTE lisible, puis filtrer le résultat agrégé.

Calcule le revenu des commandes `Paid` par mois, sous la forme `yyyy-MM`. Ne conserve que les mois dont le revenu dépasse `100`, puis trie chronologiquement.

Le résultat attendu est `2026-07, 170.75`. Juin totalise seulement `80`; la commande `Pending` de juillet ne contribue pas.

La CTE doit porter l’agrégation. La requête externe applique le seuil et l’ordre. Le test négatif inclut tous les statuts et produit un montant faux.
