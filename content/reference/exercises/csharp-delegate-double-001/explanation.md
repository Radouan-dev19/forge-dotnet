# Explication

Appliquer le même comportement à chaque élément sans modifier l'entrée.

L'intérêt de passer par une transformation n'est pas d'économiser des lignes : c'est que le comportement appliqué devient une valeur, donc substituable. Doubler, plafonner ou convertir emploient alors exactement la même mécanique de parcours, et seule la fonction change.

La multiplication vérifiée traite le cas que le parcours ne voit pas : au-delà de la moitié de la plage entière, doubler produit une valeur de signe opposé. Sans contrôle, le résultat est faux sans qu'aucune exception ne le signale. Le parcours est linéaire et l'espace correspond au tableau produit.
