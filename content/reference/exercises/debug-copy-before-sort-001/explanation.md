# Explication

Trier uniquement une copie afin que Watch et l’appelant conservent l’état initial. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(n log n) en temps et O(n) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
