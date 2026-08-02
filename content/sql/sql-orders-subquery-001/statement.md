# Objectif observable

Utiliser une sous‑requête corrélée pour trouver les produits sans ligne de commande.

Retourne `ProductId` et `ProductName`, triés par identifiant. Le seul résultat attendu est `3, Screen`.

Utilise `NOT EXISTS` et relie explicitement la sous‑requête au produit externe. Un `NOT EXISTS` non corrélé répondrait à une question globale et pourrait éliminer toutes les lignes. La variante `NOT IN` est volontairement écartée afin de ne pas introduire sa sémantique à trois valeurs en présence de `NULL`.

Le test négatif inverse le prédicat et retourne les produits déjà commandés.
