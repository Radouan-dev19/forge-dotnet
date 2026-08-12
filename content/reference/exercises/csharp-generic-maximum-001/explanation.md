# Explication

Initialiser depuis une donnée réelle et définir explicitement le cas vide.

Initialiser un maximum à zéro est l'erreur classique : elle donne le bon résultat sur toute entrée contenant au moins une valeur positive, et un résultat faux sur un tableau entièrement négatif. Le seul point de départ sûr est un élément réel de la collection, ce qui exige d'avoir traité le cas vide avant.

Le cas vide n'a pas de réponse naturelle : il faut la décider et l'annoncer. Ici le contrat retourne zéro, ce qui évite à l'appelant de tester la longueur ; lever aurait été un choix également défendable, mais il fallait choisir. Le parcours est linéaire et seul le maximum courant occupe l'espace.
