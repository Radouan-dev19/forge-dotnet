# Explication

Conserver le premier minimum et définir le tableau vide.

La comparaison stricte n'est pas une préférence : elle décide lequel des minimums ex æquo est retourné. Avec une comparaison large, le dernier gagne ; avec une comparaison stricte, le premier. Le contrat annonce le premier, donc la comparaison est stricte, et c'est le seul cas où les deux versions diffèrent.

Le tableau vide ne peut pas retourner zéro, qui est un indice valide : il faut une valeur hors du domaine. Retourner un indice plutôt qu'une valeur permet à l'appelant de retrouver l'élément et de le situer — l'inverse n'est pas possible. Le parcours est linéaire et une variable suffit.
