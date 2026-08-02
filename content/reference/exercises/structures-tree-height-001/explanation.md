# Explication

Suivre les parents jusqu’à la racine et refuser un cycle par une garde bornée. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(n²) au pire et O(1) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
