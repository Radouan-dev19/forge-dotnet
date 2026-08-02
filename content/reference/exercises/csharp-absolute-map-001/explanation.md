# Explication

Projeter vers un nouveau tableau et ne pas muter la source. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(n) en temps et O(n) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
