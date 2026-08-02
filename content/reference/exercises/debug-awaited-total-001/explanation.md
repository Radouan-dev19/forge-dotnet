# Explication

L’exercice isole l’agrégation après await ; aucun travail lancé ne doit être oublié. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(n) en temps et O(1) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
