# Explication

Réduire strictement le couple par le reste et normaliser les signes.

La terminaison vient du reste : il est strictement inférieur au diviseur, donc la seconde valeur décroît à chaque tour et atteint zéro. C'est ce qui rend l'algorithme logarithmique là où une suite de soustractions serait linéaire dans la valeur.

Les signes se normalisent d'entrée : un plus grand commun diviseur est positif par définition, et le reste en C# prend le signe du dividende, ce qui propagerait un négatif jusqu'au résultat. Le cas où l'une des valeurs est nulle se traite sans branche particulière, puisque la boucle ne s'exécute alors pas. L'espace se limite à trois variables.
