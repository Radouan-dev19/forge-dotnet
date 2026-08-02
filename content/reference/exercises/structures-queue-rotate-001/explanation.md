# Explication

Normaliser le nombre de rotations et préserver l’ordre FIFO. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(n+k) en temps et O(n) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
