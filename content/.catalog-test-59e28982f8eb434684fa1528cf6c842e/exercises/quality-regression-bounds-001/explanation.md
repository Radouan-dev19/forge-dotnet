# Explication

Un index est-il valide pour une longueur donnée ? Deux comparaisons — et le titre dit la vraie
nature de l'exercice : *fixer une non-régression*. Cette fonction n'existe pas pour être
difficile ; elle existe pour que la règle la plus violée de la programmation ait un domicile
testé.

La règle : un index valide va de zéro *inclus* à la longueur *exclue*. Le dernier élément d'une
collection de n cases est à l'index n moins un ; l'index n lui-même est la première valeur
*hors* de la collection — et c'est exactement là que vivent les exceptions d'accès qui
remplissent les journaux d'erreurs. L'écart entre « longueur » et « dernier index » est le
hors-par-un archétypal, celui que tout le monde connaît et que tout le monde recommet, en
particulier aux frontières : la boucle qui va jusqu'à `<=`, le calcul de position qui oublie
le moins un, la pagination qui confond compte et index. Le prédicat encode la règle en une
conjonction — positif ou nul, strictement inférieur à la longueur — et son plan de test la
verrouille au caractère près : zéro passe, le dernier index passe, la longueur exacte échoue,
le négatif échoue. La régression que ce filet empêche, comme le demande l'énoncé, c'est le
`<=` réintroduit un jour de refactorisation — la faute d'un caractère qui transforme un
prédicat de sûreté en générateur d'exceptions.

La longueur négative mérite sa garde propre : elle ne décrit aucune collection — c'est une
mesure corrompue en amont — et le prédicat répond faux plutôt que de raisonner dessus. Notons
la conséquence en cascade : la longueur nulle — collection vide, parfaitement légitime — rend
*tout* index invalide, y compris zéro, par la seconde comparaison. Le cas caché sur la
collection vide fige cette conséquence, que les implémentations écrites trop vite ratent en
traitant zéro comme « toujours bon ».

La forme est un prédicat pur, nommé, testable par table — et c'est le point de qualité : la
règle d'or des bornes, chacun la connaît par cœur et pourtant elle régresse. La différence
entre la connaître et la *fixer* est ce test de non-régression : une fois la règle capturée
dans une fonction couverte aux quatre frontières, elle ne peut plus dériver silencieusement.

Le coût est constant. La transposition est la pratique du *filet de régression* : quand un bug
de frontière a été corrigé une fois, la correction ne suffit pas — on extrait la règle dans
une unité nommée, on la couvre à ses frontières, et le bug ne peut plus revenir sans casser un
test. Un bug corrigé sans filet est un bug en congé ; celui-ci ne reviendra pas.
