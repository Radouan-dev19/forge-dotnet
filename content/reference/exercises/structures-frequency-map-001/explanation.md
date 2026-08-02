# Explication

Convertir les clés avec la culture invariante et incrémenter une seule entrée. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(n) attendu en temps et O(k) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
