# Explication

Maintenir un préfixe trié contenant les plus petites valeurs. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(n²) en temps et O(n) en espace pour la copie**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
