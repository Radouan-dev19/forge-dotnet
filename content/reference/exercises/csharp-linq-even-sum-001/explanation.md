# Explication

Filtrer puis agréger sans énumérer la séquence plusieurs fois.

La composition d'un filtre et d'une agrégation ne produit pas deux parcours : le filtre est paresseux, et c'est l'agrégation qui tire les éléments un par un. Matérialiser entre les deux — en construisant une collection filtrée — ajoute un parcours et une allocation sans rien apporter.

Le test de parité passe par l'égalité du reste à zéro. En C#, le reste prend le signe du dividende, si bien que le reste de moins trois par deux vaut moins un : tester l'égalité à un déclare paires des valeurs qui ne le sont pas. L'égalité à zéro est la seule forme correcte sur les deux signes. Le parcours est linéaire et l'espace constant.
