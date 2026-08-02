# Objectif observable

Calculer le nombre et le total des commandes facturables par client.

Regroupe les commandes dont le statut est `Paid` ou `Pending`. Retourne `CustomerId`, `OrderCount` et `TotalAmount`. Conserve uniquement les groupes dont le total atteint `20`.

L’ordre n’est pas significatif. Les groupes attendus sont : client `1`, deux commandes, `195.50`; client `2`, une commande, `40.25`. Le client `3` reste sous le seuil après exclusion de sa commande annulée.

Le filtre de lignes doit précéder l’agrégation (`WHERE`) et le filtre de groupe doit utiliser `HAVING`. Le test négatif omet le filtre de statut et produit un groupe indu.
