# Explication

Traiter absence et blanc avant toute déréférence, sans opérateur d'affirmation.

L'opérateur qui affirme la non-nullité n'ajoute aucune garantie : il éteint l'avertissement du compilateur et déplace le défaut vers l'exécution, où il devient une exception de déréférence dont le message ne dit pas quel maillon manquait. Une garde explicite fait le travail que l'opérateur prétendait faire.

Trois entrées se ramènent au même repli : absente, vide, composée de blancs. Les traiter séparément est une source d'oubli, et la chaîne de blancs est celle qu'on oublie — elle passe alors pour une valeur et produit un résultat vide en aval. Le coût est linéaire dans la longueur de la valeur.
