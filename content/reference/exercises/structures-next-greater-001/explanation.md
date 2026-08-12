# Explication

La pile conserve les indices encore sans réponse et chaque indice en sort une fois.

La pile contient les positions dont on attend encore le prochain élément supérieur, et elle est décroissante par construction. Quand une valeur plus grande arrive, elle répond d'un coup à toutes les positions en attente qu'elle dépasse. Chaque indice est empilé une fois et dépilé au plus une fois : le coût total reste linéaire, malgré la boucle interne.

Empiler des indices plutôt que des valeurs est ce qui rend l'écriture possible : au moment de répondre, il faut savoir à quelle position. Les indices restés dans la pile à la fin n'ont pas de suivant plus grand et gardent la valeur de remplissage. L'espace correspond au résultat et à la pile.
