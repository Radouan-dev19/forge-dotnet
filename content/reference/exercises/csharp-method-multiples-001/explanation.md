# Explication

Trois paramètres, trois contrats — et l'énoncé demande explicitement de les rédiger avant de
coder, parce que c'est là que l'exercice se joue. Le comptage lui-même est trivial ; les
frontières de chaque paramètre ne le sont pas.

Le diviseur d'abord, le plus riche. Zéro est refusé par `ArgumentOutOfRangeException` avant tout
calcul : la divisibilité par zéro n'est pas définie, et laisser la boucle courir jusqu'au modulo
produirait une `DivideByZeroException` — une exception *mécanique* qui dit « le code a planté »
là où l'exception d'argument dit « l'appelant a fourni une valeur hors contrat ». La différence
n'est pas cosmétique : elle oriente le diagnostic vers la bonne personne. Le diviseur négatif,
lui, est *accepté* et équivaut à sa valeur positive — être multiple de moins trois, c'est être
multiple de trois. Le code n'a rien à faire pour l'obtenir : `current % divisor == 0` est vrai
dans les mêmes cas quel que soit le signe du diviseur, puisque seule la nullité du reste compte.
Un contrat qui documente ce qu'on obtient gratuitement vaut mieux qu'un code qui « corrige » par
un `Math.Abs` inutile.

Les bornes ensuite, désormais familières mais recombinées : plage inclusive des deux côtés —
`current <= end` — et intervalle inversé qui rend zéro par la boucle qui ne s'exécute pas. Le
zéro lui-même est un cas qui piège : zéro est multiple de tout — son reste par n'importe quel
diviseur est nul — donc une plage qui chevauche zéro compte une unité de plus que l'intuition
ne le prédit. Les négatifs de la plage se comportent comme leurs symétriques : moins six est
multiple de trois. Les cas cachés combinent ces frontières — plage négative, plage autour de
zéro, diviseur négatif — et réfutent le comptage recopié de l'exemple.

Il existe une réponse arithmétique en temps constant — compter les multiples sous une borne par
division entière, puis soustraire — et sa manipulation des bornes négatives est notoirement
piégeuse. La boucle linéaire, sur des plages bornées à deux mille valeurs, est le choix qui se
prouve d'une phrase ; la formule attendra un besoin réel de performance, et le jour venu, elle
se validera par test différentiel contre cette version-ci. Garder une implémentation lente et
sûre comme oracle d'une rapide et subtile : ce réflexe-là vaut de l'or en migration.

La transposition : chaque paramètre d'une fonction publique mérite sa phrase de contrat — domaine
accepté, comportement aux bornes, exception réservée aux valeurs sans signification. La rédaction
préalable demandée par l'énoncé n'est pas un rituel scolaire, c'est la spécification en train de
naître.
