# Objectif observable

Mesurer un N+1 puis charger les clients et leurs commandes avec une seule commande SQL.

## Travail demandé

Retourne chaque client avec son nombre de commandes, dans l’ordre des identifiants. Le résultat attendu est `(Ada,2)`, `(Grace,1)`, `(Linus,1)`.

Le starter charge d’abord les clients puis exécute un `CountAsync` dans la boucle. Il produit les bonnes valeurs mais quatre commandes SQL. La solution doit conserver les mêmes valeurs avec une seule commande et une lecture non suivie.

Le test utilise un intercepteur EF : une comparaison de durée serait instable et n’est pas acceptée comme preuve.
