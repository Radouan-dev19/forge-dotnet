# Explication

Utiliser une somme élargie et définir la collection vide.

Deux pièges indépendants se cumulent ici. Le premier est la division entière : diviser une somme entière par un compte entier tronque la partie fractionnaire, et convertir le quotient arrive trop tard — c'est la somme qu'il faut convertir. Le second est le dépassement de l'accumulateur : une somme de valeurs entières peut sortir des trente-deux bits bien avant qu'une valeur isolée ne le fasse.

La collection vide n'est pas une erreur : le contrat annonce zéro, ce qui évite à chaque appelant de tester la longueur avant d'appeler. Le parcours est linéaire et l'espace se limite à l'accumulateur.
