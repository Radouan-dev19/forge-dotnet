# Explication

Tout cet exercice tient dans la ligne d'initialisation, et le titre — maximum *signé* — l'annonce.
L'implémentation instinctive part de `max = 0` et parcourt : elle est juste sur tous les tableaux
qui contiennent au moins une valeur positive ou nulle, c'est-à-dire sur tous les exemples qu'on
écrit spontanément. Elle est fausse sur un tableau entièrement négatif, où elle rend zéro — une
valeur qui n'appartient même pas aux données. C'est le prototype du bug qui traverse la revue et
les tests écrits à la va-vite : la classe d'entrées qui le révèle ne vient pas à l'esprit tant
qu'on raisonne sur des quantités, des prix ou des compteurs, tous positifs par habitude.

Partir du premier élément supprime la classe d'erreur entière : l'accumulateur appartient aux
données dès le départ, donc le résultat final leur appartient aussi, quel que soit leur signe.
L'alternative — initialiser à `int.MinValue` — est correcte elle aussi et se discute : elle
permet de démarrer la boucle à zéro, mais elle introduit une valeur sentinelle étrangère aux
données, qui deviendrait le résultat rendu si la garde de tableau vide disparaissait un jour.
Ancrer l'accumulateur dans les données est la version qui survit le mieux aux modifications
futures, et c'est le critère qui départage deux solutions correctes.

Le cas du tableau vide relève du contrat, pas de l'algorithme : ici, la convention est de rendre
zéro, et la garde la matérialise en tête de fonction. On peut préférer une exception — `Max` de
LINQ lève sur une séquence vide — mais l'important est ailleurs : la décision doit être écrite,
prise consciemment, et testée. Les cas cachés éprouvent précisément ce trio — tableau tout
négatif, tableau vide, maximum placé en tête ou en queue — et réfutent au passage la réponse
codée en dur sur l'exemple visible. Le maximum en première position teste que la boucle démarre
bien à l'indice un sans jamais relire l'élément d'initialisation comme un candidat oublié.

Le coût est linéaire et c'est une borne basse : affirmer qu'aucune valeur ne dépasse `max` exige
d'avoir regardé chacune. En espace, l'accumulateur unique suffit. La transposition dépasse le
maximum : toute réduction — somme, minimum, moyenne, « meilleur candidat » — pose la même
question d'initialisation, et la réponse est toujours la même. Un accumulateur initialisé hors
des données est une dette ; un accumulateur ancré dans les données est un invariant.
