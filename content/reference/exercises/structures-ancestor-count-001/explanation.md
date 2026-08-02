# Explication

Valider chaque parent et borner le parcours pour détecter un cycle. La solution de référence sépare la validation de l’opération principale et ne dépend d’aucun état externe. Sa complexité est **O(h) en temps et O(1) en espace**. Les cas cachés changent valeurs, bornes et tailles afin qu’une constante mémorisée ne puisse pas réussir.
