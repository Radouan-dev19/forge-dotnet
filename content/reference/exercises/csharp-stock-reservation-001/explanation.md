# Explication

Préserver l’invariant stock positif et accepter exactement la quantité disponible. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(1) en temps et O(1) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
