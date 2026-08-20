# Explication

Le tri par insertion est celui qu'on exécute avec des cartes en main : le début de la main est
toujours rangé, et chaque nouvelle carte remonte jusqu'à sa place. En code, cela devient un
invariant précis — avant le tour `i`, le préfixe `result[0..i-1]` est trié — et toute la boucle
n'existe que pour préserver cette phrase. C'est un excellent exercice pour apprendre à nommer un
invariant, car il se vérifie visuellement à chaque itération.

La mécanique du décalage mérite d'être comprise plutôt que recopiée. On sauvegarde d'abord la
valeur à insérer, parce que les décalages vont écraser sa case. On déplace ensuite vers la droite
chaque élément du préfixe strictement supérieur à elle : le « trou » laissé par la sauvegarde
remonte vers la gauche, et l'écriture finale `result[j + 1] = current` le comble. Écrire des
échanges deux à deux à la place des décalages resterait correct mais triplerait les écritures —
l'insertion par décalage n'écrit chaque case qu'une fois par tour.

Deux détails de comparaison portent des conséquences. La garde `j >= 0` doit venir avant
`result[j] > current` : dans l'autre ordre, l'insertion d'un élément plus petit que tout le
préfixe lit l'indice moins un et lève. L'évaluation paresseuse de `&&` rend l'ordre des deux
conditions significatif — c'est l'un des rares endroits où la sémantique du langage fait partie
de l'algorithme. Et la comparaison est stricte : avec `>=`, les égaux seraient déplacés sans
nécessité, ce qui ferait perdre au tri sa stabilité — invisible sur des entiers nus, décisif dès
que les éléments portent d'autres champs.

Le contrat ajoute la non-mutation de l'entrée, et le harnais la vérifie sur des cas dédiés en
comparant les arguments avant et après l'appel : la copie initiale n'est pas une précaution de
style, c'est une exigence testée. Les cas cachés font aussi varier les dispositions — déjà trié,
ordre inverse, doublons — et réfutent la sortie codée en dur de l'exemple.

Le coût est quadratique dans le pire cas, comme tout tri par comparaisons voisines, mais avec une
propriété que les autres tris simples n'ont pas : sur une entrée déjà presque triée, la boucle
interne ne fait presque aucun tour, et le coût devient quasi linéaire. C'est pour cela que
l'insertion sert encore en pratique, comme finition des tris hybrides sur les petites plages.
Retenez la transposition : quand un flux arrive presque ordonné — journaux, événements datés —
l'insertion locale bat les algorithmes asymptotiquement meilleurs.
