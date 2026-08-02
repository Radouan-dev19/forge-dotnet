# Explication

Avancer exactement l’index de la valeur consommée et conserver les doublons. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(n+m) en temps et O(n+m) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
