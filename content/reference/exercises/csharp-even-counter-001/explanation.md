# Explication

Parcourir chaque valeur une fois et compter aussi zéro et les pairs négatifs.

Le piège est dans le signe. En C#, le reste d'une division prend le signe du dividende : le reste de moins trois par deux vaut moins un, pas un. Tester l'égalité du reste à un déclare donc paires des valeurs négatives impaires. Tester l'égalité à zéro est la seule forme correcte sur les deux signes.

Zéro est pair, et c'est le cas que les jeux d'essai oublient le plus souvent. Le parcours est linéaire et seul le compteur occupe l'espace.
