# Explication

Une soustraction et une garde. Ce que l'exercice enseigne n'est pas le calcul mais ce qu'il implique
sur la façon d'écrire un fichier de construction — et c'est la différence entre une intégration
continue de vingt secondes et une de six minutes.

**Chaque couche part de l'état laissé par la précédente.** C'est ce qui rend l'invalidation
transitive : si l'étape modifiée produit un résultat différent, l'étape suivante ne part plus du même
point, donc son propre résultat mis en cache ne correspond plus à rien. Elle est refaite, et la
suivante avec elle, jusqu'au bas de la pile. Le compte est donc « de l'étape modifiée jusqu'à la
fin », et l'oubli classique est le « plus un » : **l'étape modifiée est elle-même reconstruite**.
Une implémentation qui rend la simple différence donne zéro quand la dernière étape change, ce qui
est visiblement faux — aucune modification ne se propage sans rien reconstruire.

**Le cache ne comprend pas ce que fait l'étape.** Il compare une empreinte : l'instruction, et pour
les copies, le contenu des fichiers concernés. Changer un commentaire dans une commande invalide
autant qu'en réécrire le sens. C'est frustrant, et c'est aussi ce qui rend le comportement
prévisible : il n'y a pas d'heuristique à deviner, seulement une position dans le fichier.

**D'où la règle d'écriture, qui est le vrai enseignement.** Ce qui change rarement se place en haut,
ce qui change à chaque commit se place en bas. Le cas d'école : copier tout le code source, puis
restaurer les dépendances. À chaque modification d'une ligne de code, la copie change, donc la
restauration des dépendances est invalidée, donc elle est refaite — plusieurs minutes, à chaque
construction, pour des dépendances qui n'ont pas bougé depuis des semaines. Inverser les deux étapes
— copier d'abord les seuls fichiers de dépendances, restaurer, puis copier le code — ramène la
construction ordinaire à ce qui a réellement changé. Le gain se mesure en minutes par construction,
multipliées par le nombre de commits d'une équipe.

**Le refus des rangs hors bornes n'est pas décoratif.** Sans lui, un rang supérieur au nombre
d'étapes rend un compte négatif : une valeur que l'appelant affichera, ou pire, utilisera dans un
calcul de durée. Refuser au moment où l'incohérence est visible évite qu'elle voyage.

**Ce que le modèle simplifie**, et qu'il faut savoir nommer : les constructions à plusieurs étapes
compliquent l'image, puisqu'une étape finale peut ne copier que le résultat d'une étape antérieure
sans dépendre des couches intermédiaires. Le raisonnement reste le même à l'intérieur de chaque
étape ; c'est le graphe de dépendance qui cesse d'être une simple pile.

Le coût est constant, et il devait l'être : la réponse ne dépend pas du nombre d'étapes à parcourir,
seulement de deux nombres.
