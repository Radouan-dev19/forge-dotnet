# Explication

Copier une liste tient en un constructeur, et pourtant l'exercice touche à l'une des confusions
les plus coûteuses du langage : la différence entre copier une *référence* et copier une
*collection*.

Écrire `var copy = values;` ne copie rien : les deux variables désignent la même liste, et toute
modification par l'une est visible par l'autre. C'est le comportement des types référence, et il
surprend d'autant plus qu'il est silencieux — le code compile, l'exemple nominal passe, et le
bug apparaît des semaines plus tard quand un appelant modifie « sa » copie et corrompt la
source. Le constructeur `new List<int>(values)` fait la vraie copie : une liste neuve, remplie
des mêmes valeurs, indépendante — ajouter, retirer ou trier l'une ne touche plus l'autre. Les
cas du harnais qui capturent l'argument et le comparent après l'appel ferment la porte aux
pseudo-copies.

Une précision d'honnêteté que l'exercice rend indolore mais qu'il faut nommer : cette copie est
*superficielle*. Sur des entiers — des valeurs — superficielle et profonde coïncident, la copie
est totale. Sur des objets, la liste neuve contiendrait les *mêmes références* : les éléments
resteraient partagés, et modifier un objet via une liste se verrait dans l'autre. Le jour où le
type d'élément change, la question « copie de la liste ou copie des éléments ? » doit être
reposée — c'est l'une des relectures les plus rentables lors d'un changement de modèle.

Le contrat précise ce que la copie préserve : l'ordre et les doublons. Le constructeur de copie
garantit les deux par construction — il recopie séquentiellement — là où un détour par un
ensemble dédupliquerait et où un tri réordonnerait. Ces déformations « au passage » sont
exactement ce que le mot *copie* interdit : une copie est neutre, elle n'améliore rien. La liste
vide rend une liste vide neuve — pas la constante partagée, pas `null` — et le `null` d'entrée
reste une faute d'appel signalée nommément.

Le coût est linéaire en temps et en espace, incompressible pour une copie indépendante — et
c'est bien pour cela qu'on ne copie qu'aux frontières qui l'exigent : rendre une collection
interne à un appelant extérieur, capturer un instantané avant une modification, isoler un
traitement parallèle. Copier partout « par sécurité » est l'excès inverse, qui coûte mémoire et
temps sans contrat pour le justifier.

La transposition tient en une question à poser devant chaque passage de collection : qui peut la
modifier après cet appel, et qui en souffrirait ? Si la réponse inquiète, copier — vraiment — à
la frontière, et documenter de quel côté la propriété reste.
