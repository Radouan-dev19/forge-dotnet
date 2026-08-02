# Objectif observable

Écrire une jointure explicite qui retourne uniquement les commandes ouvertes des clients actifs.

## Données et résultat attendu

`Customers.CustomerId` est la clé principale ; `Orders.CustomerId` est une clé étrangère. Retourne `OrderId`, `CustomerName` et `Total` pour les statuts `Paid` ou `Pending`, puis trie par `OrderId`.

Les lignes attendues sont les commandes `1` et `2` d’Ada, puis la commande `3` de Grace. La commande annulée de Linus est exclue à la fois par le statut et par l’inactivité du client.

## Contraintes

- utilise `INNER JOIN ... ON` ;
- qualifie les colonnes ambiguës ;
- n’utilise ni `SELECT *`, ni produit cartésien ;
- ne modifie aucune donnée.

Cas négatif testé : une condition de jointure incorrecte produit des associations supplémentaires et doit échouer sur les valeurs, pas seulement sur le nombre de colonnes.
