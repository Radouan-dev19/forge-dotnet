# Explication

Diviser par deux jusqu’à la borne et compter les réductions réellement faites. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(log n) en temps et O(1) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
