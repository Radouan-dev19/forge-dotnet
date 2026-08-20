# Explication

Faire tourner une file, c'est répéter le geste élémentaire qui la définit : la tête sort, se
replace en queue, et tout le monde avance d'un rang. L'exercice ressemble à la rotation de
tableau du bloc algorithmique, et la comparaison des deux est voulue — même problème, deux
outils, deux raisonnements.

La version tableau calculait pour chaque case sa position d'arrivée par arithmétique modulaire ;
la version file *exécute* la rotation, un tour à la fois, avec `Dequeue` puis `Enqueue`. Le code
devient une transcription du geste — aucune arithmétique d'indices, aucun risque de hors-par-un
dans un calcul de position — et c'est l'argument en sa faveur : quand une structure porte
nativement l'opération du problème, l'utiliser rend la correction évidente à l'œil. Le prix est
un coût proportionnel au nombre de tours *effectués*, là où le tableau payait toujours une passe
complète ; d'où l'importance de la normalisation, qui borne les tours à moins d'une longueur.

Cette normalisation est le seul fragment calculatoire restant, et il est déjà connu : le double
modulo `((count % n) + n) % n` replie n'importe quel entier — négatif, énorme, multiple exact —
dans l'intervalle des rotations utiles. Mille tours sur une file de trois font un tour ; moins
un tour en fait deux dans l'autre sens comptés dans celui-ci ; un multiple de la longueur n'en
fait aucun, et la boucle ne touche pas la file. Les cas cachés visent ces trois régimes, plus la
file vide — sortie avant le modulo, car la longueur sert de diviseur et zéro diviserait.

Le contrat de non-mutation éclaire un choix discret : la file est construite *depuis* le
tableau — `new Queue<int>(values)` recopie — et le résultat est un tableau *neuf* produit par
`ToArray`. L'entrée traverse intacte, le harnais le vérifie, et la structure de travail vit et
meurt dans la fonction : une structure interne n'a pas à fuir dans la signature. C'est un
principe d'encapsulation en miniature — on choisit ses outils sans les imposer à l'appelant.

L'ordre FIFO préservé, que l'énoncé souligne, est précisément ce que la paire
`Dequeue`-`Enqueue` garantit : les éléments non déplacés gardent leurs positions relatives,
seule l'origine du cercle change. Une implémentation qui passerait par une pile inverserait ;
le nom de la structure fait partie de la spécification.

La transposition est celle des ronds de service : tourniquets d'astreinte, distribution
cyclique de tâches, carrousels d'affichage — partout où « chacun son tour » doit survivre à des
décalages arbitraires, la file plus le repli modulaire est le couple qui écrit court et juste.
