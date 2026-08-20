# Explication

Pour chaque position, la prochaine valeur strictement supérieure à droite : la question se pose
en une phrase, la réponse naïve en deux boucles — pour chaque indice, chercher devant — et son
coût quadratique est le point de départ. L'exercice existe pour la structure qui le fait tomber
à linéaire : la pile monotone, l'un des outils les moins évidents et les plus réutilisés de
l'algorithmique de tableaux.

L'idée se raconte mieux qu'elle ne se devine. On parcourt le tableau une fois, et la pile
contient à tout instant *les indices qui attendent encore leur réponse*. Quand une nouvelle
valeur arrive, elle répond d'un coup à tous les indices en attente qu'elle dépasse : on les
dépile, on écrit la réponse, et la nouvelle position rejoint à son tour les attentes. La pile
reste ainsi *monotone* — les valeurs de ses indices décroissent du fond vers le sommet — non par
décret mais par construction : tout ce qui était plus petit que l'arrivant vient d'être dépilé.
Cette monotonie est l'invariant à savoir énoncer, car c'est elle qui garantit qu'on ne dépile
que ce qui doit l'être.

L'argument de coût est le plus élégant du catalogue : chaque indice est empilé une fois et
dépilé au plus une fois — l'énoncé le dit mot pour mot — donc le travail total est linéaire,
même si un tour de boucle donné peut dépiler beaucoup. Ce raisonnement *amorti* — compter le
travail par élément sur toute la vie de l'algorithme, pas par itération — est une technique
d'analyse à part entière, et cet exercice est son exemple le plus propre.

Deux détails de contrat fixent les bords. La comparaison est *stricte* : un doublon ne répond
pas à son égal — `values[i] > values[stack.Peek()]`, et le cas caché aux valeurs égales
départage `>` de `>=`. Et les indices restés dans la pile à la fin — les maxima de fin de
tableau, dont le dernier élément — gardent le moins un posé par le remplissage initial :
l'initialisation par défaut *est* la réponse du cas « pas de supérieur », aucune passe de
nettoyage n'est nécessaire.

La pile stocke des *indices*, pas des valeurs — il faut savoir où écrire la réponse — et les
valeurs se relisent par indirection : ce choix indices-plutôt-que-valeurs revient dans toutes
les variantes du motif.

La transposition est étonnamment concrète : prochain jour plus chaud dans une série de
températures, prochaine cotation supérieure, durée avant qu'un prix ne soit dépassé — et les
variantes en miroir (précédent plus petit, prochain plus petit) servent dans le calcul
d'histogrammes et de fenêtres. Reconnaître la question « prochain élément qui domine » et
répondre « pile monotone, coût amorti linéaire » est exactement le niveau attendu en entretien.
