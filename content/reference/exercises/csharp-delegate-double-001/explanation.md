# Explication

Le doublement des valeurs n'est pas le sujet ; le sujet est la *forme* de la solution — une
fonction passée en argument à une autre fonction — et ce que cette forme change dans la manière
de construire du code.

`Array.ConvertAll` reçoit deux choses : des données et un comportement. Le comportement est la
lambda `value => checked(value * 2)`, un delegate — une valeur qui *est* du code exécutable, avec
un type (`Converter<int, int>` ici) qui décrit sa signature. La boucle, l'allocation du tableau
de sortie, l'écriture case par case appartiennent à `ConvertAll` ; seule la transformation
appartient à l'appelant. Cette séparation est le mécanisme fondateur de LINQ, des rappels
d'événements et de l'injection de dépendances : dans les trois cas, on paramètre un squelette
générique par du comportement fourni de l'extérieur. L'exercice le montre à l'échelle d'une
ligne, là où il s'apprend sans bruit.

La lambda elle-même contient la seconde décision : `checked`. Doubler le plus grand entier
représentable ne donne pas un nombre plus grand — l'arithmétique par défaut s'enroule et produit
un négatif, silencieusement. Le mot clé transforme ce résultat faux en `OverflowException` levée
à la case fautive. Placer la vérification *dans* la lambda, plutôt qu'autour de l'appel, la fait
voyager avec le comportement : où que ce delegate soit appliqué, sa promesse arithmétique le
suit. C'est un détail qui enseigne quelque chose de plus grand — un delegate embarque ses
invariants, pas seulement son calcul.

Le contrat de non-mutation est tenu par construction : `ConvertAll` écrit dans un tableau neuf
qu'il alloue lui-même, et la source n'apparaît qu'en lecture. La version en place — doubler dans
`values` puis le retourner — rendrait les mêmes valeurs et modifierait les données de
l'appelant, ce que le harnais détecte en comparant l'argument avant et après l'appel sur des cas
dédiés. Quant au `null`, il reste une faute d'appel signalée en tête, distincte du tableau vide
qui traverse et rend un tableau vide neuf.

Les cas cachés font varier signes et zéro — doubler du négatif reste du négatif, doubler zéro
rend zéro — et réfutent la sortie recopiée de l'exemple. Le coût est linéaire, une allocation.

La transposition à emporter : chaque fois que deux boucles ne diffèrent que par l'opération
appliquée à chaque élément, la boucle mérite d'être écrite une fois et l'opération d'être
injectée. Reconnaître ce motif transforme du code répété en une bibliothèque de comportements —
et c'est exactement le geste que LINQ industrialise à l'échelle du langage.
