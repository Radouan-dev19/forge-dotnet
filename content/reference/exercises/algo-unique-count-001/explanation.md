# Explication

La solution tient en deux lignes utiles, et c'est précisément ce qui mérite explication : savoir
quand *ne pas* écrire d'algorithme est une compétence, et elle se justifie, elle ne se devine
pas.

Compter les valeurs distinctes, c'est demander le cardinal d'un ensemble. Le type `HashSet<int>`
matérialise exactement cette notion : son constructeur consomme la séquence, refuse
silencieusement chaque doublon à l'insertion, et son `Count` est la réponse. Il n'y a rien à
écrire parce que la structure de données *est* la spécification. L'alternative artisanale — trier
une copie puis compter les changements de valeur entre voisins — fonctionne, coûte le tri, exige
une copie pour respecter la non-mutation de l'entrée, et surtout réintroduit à la main deux
occasions d'erreur : le hors-par-un du comptage des transitions, et le cas du tableau d'une seule
case. Le choix entre les deux n'est pas esthétique : à contrat égal, on préfère la version dont
la correction repose sur une bibliothèque éprouvée plutôt que sur notre vigilance.

Il faut aussi savoir ce que ce confort coûte. L'ensemble se construit en temps moyen linéaire,
mais alloue un espace proportionnel au nombre de valeurs distinctes ; la variante par tri
consomme moins de mémoire vive au prix d'un logarithme de temps en plus. Sur les tailles de cet
exercice, la question est théorique ; sur un flux de millions d'événements, elle devient un choix
d'architecture, et des structures probabilistes entrent en scène quand même l'ensemble ne tient
plus en mémoire. L'exercice installe le premier étage de cette échelle.

Les cas de bord confirment que la structure fait le travail. Le tableau vide donne un ensemble
vide, cardinal zéro, sans garde spéciale. Zéro et les négatifs sont des entiers comme les
autres — le contrat les nomme parce que les implémentations artisanales à base de tableaux de
comptage indexés par la valeur, elles, s'y cassent. Le tableau où tout est identique rend un, et
celui où tout est distinct rend la longueur : deux extrêmes que les cas cachés placent pour
encadrer le comportement, avec le refus du `null`, signalé par `ArgumentNullException` parce
qu'une collection absente est une faute de l'appelant et non une collection vide.

La transposition est quotidienne : nombre de clients distincts dans des commandes, de pages vues
uniques, de codes d'erreur différents dans un journal. Dans tous ces cas, la première question
utile est « quelle est la clé d'unicité ? » — ici l'entier lui-même, ailleurs un identifiant ou
un n-uplet — et la seconde est « l'ensemble tient-il en mémoire ? ». Ces deux questions, posées
dans cet ordre, remplacent avantageusement toute nostalgie de la boucle imbriquée.
