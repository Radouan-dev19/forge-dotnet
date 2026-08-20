# Explication

L'algorithme d'Euclide tient en trois lignes et repose sur un théorème qu'il faut pouvoir dire à
voix haute : les diviseurs communs de `(a, b)` sont exactement ceux de `(b, a mod b)`. Remplacer
le couple par sa réduction ne perd donc aucun diviseur et n'en invente aucun — la boucle ne
cherche pas le PGCD, elle transporte l'ensemble des candidats vers un cas où la réponse devient
triviale. Quand le second membre atteint zéro, tout divise zéro, et le PGCD du couple est le
premier membre lui-même. C'est pour cela que la fonction retourne `left` après la boucle, et non
une variable accumulée pendant le parcours.

La terminaison n'est pas décorative. `a mod b` est strictement inférieur à `b` : le second membre
décroît à chaque tour dans les entiers positifs, donc atteint zéro en un nombre fini d'étapes. Ce
raisonnement — trouver la quantité qui décroît strictement — est la façon dont on prouve qu'une
boucle `while` s'arrête, et il se transpose à toutes les boucles de réduction : convergence d'un
calcul de point fixe, consommation d'une file de travail, réessais avec délai croissant.

La normalisation des signes en tête est une décision de frontière, pas un détail. Le reste C# d'un
opérande négatif est négatif : sans `Math.Abs`, le couple peut osciller et la « décroissance »
n'est plus garantie, ou le résultat sort négatif — un PGCD négatif ne veut rien dire. Traiter les
signes une fois à l'entrée, plutôt que dans la boucle, suit une règle plus générale : ramener
d'abord l'entrée dans le domaine où l'algorithme est prouvé, puis dérouler l'algorithme sans
re-vérifier à chaque tour ce qui est déjà acquis.

Les cas cachés éprouvent précisément ces frontières : un opérande nul — le PGCD de `(a, 0)` est
`a`, et la boucle ne fait alors aucun tour —, des opérandes négatifs, et des couples dont la
réponse n'est pas celle de l'exemple visible, ce qui réfute une valeur codée en dur. L'erreur la
plus fréquente reste la soustraction répétée à la place du modulo : correcte, mais linéaire dans
la valeur des entrées au lieu de logarithmique, elle transforme un calcul instantané en boucle
interminable dès que les nombres grandissent.

Le coût, justement : chaque deux tours au plus, le premier membre est au moins divisé par deux,
d'où un nombre d'étapes proportionnel au logarithme du plus petit opérande. C'est ce qui rend
Euclide utilisable tel quel dans du code réel — réduction de fractions, calculs de périodes, ou
alignement de tailles de blocs — sans jamais se demander si l'entrée est trop grande.
