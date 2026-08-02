# Objectif observable

Construire un `IQueryable` qui filtre les commandes côté SQL avant matérialisation.

## Travail demandé

À partir d’un total minimal fourni par l’appelant, retourne `OrderId`, le nom client et `Total`, triés par identifiant. La requête doit rester un `IQueryable` jusqu’à son exécution, utiliser une projection et être en lecture seule.

Avec un minimum de `70`, les commandes `1` et `2` sont attendues. Le test inspecte `ToQueryString` : le SQL doit contenir un prédicat paramétré et ne doit pas charger toutes les colonnes de l’entité.

Le starter ignore le paramètre. Il compile et retourne des données, mais le test négatif prouve qu’il charge aussi les commandes sous le seuil.
