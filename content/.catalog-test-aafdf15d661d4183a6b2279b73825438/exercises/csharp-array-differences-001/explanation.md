# Explication

Passer des valeurs aux écarts entre valeurs voisines est l'opération inverse des sommes
préfixes, et le couple mérite d'être vu ensemble : dériver puis cumuler redonne la série de
départ. Cet exercice installe le côté « dérivée », le plus piégeux des deux, parce que la sortie
n'a pas la même taille que l'entrée — et c'est de là que viennent tous les bugs.

Quatre valeurs produisent trois écarts. La solution encode ce décalage une seule fois, dans
l'allocation : `values.Length - 1` cases, borné à zéro par `Math.Max` pour absorber le tableau
vide. Ensuite, la boucle parcourt *le résultat* — pas l'entrée — et chaque tour lit deux cases
sources, `index` et `index + 1`. Ce choix de borne n'est pas anodin : la version qui itère sur
l'entrée en s'arrêtant « une case avant la fin » est équivalente, mais celle qui itère sur
l'entrée entière lit une case au-delà du tableau au dernier tour et lève. Le hors-par-un se
loge toujours dans la question « sur quoi itère-t-on ? », et la réponse la plus sûre est :
sur ce qu'on écrit, jamais sur ce qu'on lit.

Le sens de la soustraction fait partie du contrat : `values[index + 1] - values[index]`, le
suivant moins le courant, si bien qu'une série croissante donne des écarts positifs. L'inverser
produit des résultats plausibles — mêmes valeurs absolues, signes opposés — qui passent le
regard rapide et échouent sur tout cas contenant une descente ; l'exemple de l'énoncé en
contient une précisément pour cela.

Les bornes basses relèvent de la convention et sont écrites dans l'énoncé : zéro ou une valeur
ne définissent aucune paire voisine, donc un tableau vide *neuf* est rendu — pas `null`, pas une
exception. `Math.Max(0, ...)` dans l'allocation traite ces deux cas sans branche dédiée, et la
boucle qui ne tourne pas fait le reste. Le `null`, lui, reste une faute d'appel signalée par
`ArgumentNullException` : l'absence de collection n'est pas une collection courte.

Le coût est linéaire, une lecture double et une écriture par case de sortie, sans état
persistant entre tours — chaque écart ne dépend que de sa paire, ce qui rend la fonction
triviale à vérifier par table.

La transposition est concrète : consommation entre deux relevés de compteur, variation d'un
solde entre deux journées, latence ajoutée entre deux étapes d'un pipeline. Chaque fois, la
série d'écarts a une case de moins que la série de mesures, et chaque fois quelqu'un l'oublie.
L'exercice existe pour que ce quelqu'un ne soit pas vous.
