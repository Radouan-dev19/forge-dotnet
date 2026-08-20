# Explication

La rotation ressemble à un problème de déplacement ; c'est en réalité un problème d'arithmétique
modulaire, et toute la solution tient dans la normalisation du décalage. Le contrat accepte
n'importe quel entier : un décalage plus grand que le tableau doit s'enrouler, un décalage
négatif doit tourner dans l'autre sens, et un multiple exact de la longueur doit rendre le
tableau inchangé. Une seule expression traite les trois, et elle mérite d'être décortiquée.

Le premier modulo, `offset % values.Length`, ramène la valeur dans l'intervalle ouvert
`]-n, n[` — mais en C#, le reste hérite du signe du dividende : moins un modulo trois vaut moins
un, pas deux. D'où le deuxième temps, `+ values.Length` puis modulo à nouveau, qui replie les
restes négatifs dans `[0, n[` sans toucher les positifs. Cette danse à deux modulos est le
piège le plus rejoué de l'arithmétique d'indices, et les cas cachés visent exactement lui : un
décalage négatif, un décalage supérieur à la longueur, et un tour complet qui doit rendre une
copie à l'identique. Une implémentation qui utilise `Math.Abs` au lieu du repli échoue sur les
négatifs — la rotation de moins un vers la gauche est une rotation d'un vers la droite, pas la
même chose qu'une rotation d'un vers la gauche.

La garde du tableau vide vient avant tout le reste, et pour une raison mécanique : la longueur
sert de diviseur, et zéro diviseur lève. Rendre un tableau vide est la seule réponse sensée, et
le faire explicitement en tête documente le cas au lieu de le laisser au hasard d'une exception.

Vient ensuite un choix d'approche. La solution construit un tableau neuf en lisant chaque case
source à sa position d'arrivée — coût linéaire en temps, linéaire en espace, entrée intacte. Il
existe une variante célèbre en place, par triple renversement, qui n'alloue rien ; elle est ici
hors contrat, puisque l'entrée ne doit pas être modifiée et que le harnais le vérifie sur des cas
dédiés. Savoir que les deux existent, et choisir selon le contrat plutôt que par réflexe, est un
des acquis de l'exercice.

La transposition est plus fréquente qu'il n'y paraît : les tampons circulaires des journaux et
des files, la pagination cyclique d'un carrousel, les fenêtres glissantes sur des séries
temporelles — tous reposent sur « l'indice logique se replie dans la capacité physique », le même
`((x % n) + n) % n` écrit ici. L'apprendre sur un tableau de cinq entiers coûte trente minutes ;
le déboguer dans un tampon de production coûte une nuit.
