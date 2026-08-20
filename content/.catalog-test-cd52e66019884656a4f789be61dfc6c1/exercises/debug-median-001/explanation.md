# Explication

La médiane est la statistique du débogueur : contrairement à la moyenne, une poignée de valeurs
aberrantes ne la déplace presque pas, et c'est elle qu'on regarde pour dire « la latence typique
est saine, seuls quelques appels dérapent ». La calculer proprement combine trois décisions déjà
rencontrées séparément — et c'est leur assemblage qui fait l'exercice.

La première est structurelle : la médiane exige un ordre, donc un tri, et `Array.Sort` mute. Le
titre de l'énoncé — inspecter *sans muter* — impose la copie préalable, pour la même raison que
dans l'exercice voisin de tri : des données de diagnostic réordonnées par l'inspection elle-même
sont des données détruites. Le harnais compare les arguments avant et après ; la version qui
trie l'original rend la bonne médiane et échoue quand même — la valeur de retour n'est pas tout
le contrat.

La deuxième est le partage pair-impair, le vrai centre technique. Longueur impaire : l'élément
du milieu, `copy[Length / 2]`, où la division entière tombe juste. Longueur paire : il n'y a
*pas* d'élément central — il y en a deux, aux indices `middle - 1` et `middle`, et la médiane
est leur moyenne. L'oubli de ce cas — prendre l'un des deux — donne une médiane décalée que
seuls les tableaux pairs révèlent ; le calcul de la moyenne en entiers — sans le passage en
`decimal` *avant* la division — perd la demi-unité : la médiane de deux et trois vaut deux et
demi, pas deux. Le transtypage sur le premier opérande suffit, la promotion faisant le reste,
mais sa position est critique — c'est la même leçon de types que la moyenne du bloc langage,
appliquée à deux valeurs.

La troisième est la convention du vide : zéro, comme le contrat l'impose — une médiane de rien
n'existe pas mathématiquement, et le choix du neutre permet aux agrégations de tableaux de bord
de sommer sans filtrer. L'alternative par exception se discuterait ; l'important, comme
toujours, est que la convention soit écrite dans le contrat et couverte par un cas.

Les cas cachés balaient les axes : pair contre impair, doublons — la médiane d'un tableau
constant est cette constante —, négatifs, singleton — sa propre médiane —, et la disposition
inédite contre le résultat figé.

Le coût est dominé par le tri, n log n, plus la copie linéaire. Il existe un algorithme de
sélection en temps linéaire moyen — utile quand n devient énorme et la médiane fréquente — et
savoir le *citer* suffit à ce niveau : le tri d'une copie reste la version qu'on relit sans
effort. La transposition est immédiate : percentiles de latence, valeurs typiques de mesures,
seuils robustes d'alerte — partout où la moyenne ment à cause des extrêmes, la médiane et ses
cousines percentiles la remplacent, avec les trois mêmes décisions à reprendre.
