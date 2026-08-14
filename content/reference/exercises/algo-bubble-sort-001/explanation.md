# Explication

La première décision de cette solution n'est pas algorithmique : c'est la copie. Le contrat
interdit de modifier l'entrée, et un tri est par nature une suite de modifications. `Clone()` en
première ligne réconcilie les deux — tout le travail se fait sur le duplicata, et l'appelant
retrouve son tableau intact. Les cas cachés le vérifient réellement : le harnais capture les
arguments avant l'appel et les compare après, si bien qu'un tri « en place, puis retourné »
échoue même quand la valeur de retour est correcte. C'est une leçon qui dépasse le tri : une
fonction qui promet de ne rien toucher doit le prouver, pas seulement rendre le bon résultat.

Le tri lui-même repose sur une propriété qu'il faut savoir énoncer : après le premier passage
complet, le plus grand élément est arrivé en dernière position, quel que soit son point de
départ — chaque comparaison le pousse d'un cran vers la droite dès qu'il est rencontré. Après le
deuxième passage, le deuxième plus grand est en avant-dernière position, et ainsi de suite. C'est
cette propriété qui justifie la borne mobile `end` : la zone déjà triée en queue de tableau n'a
plus besoin d'être visitée, et la boucle interne raccourcit à chaque tour. Une version qui
parcourt toujours le tableau entier resterait correcte mais ferait deux fois trop de
comparaisons — et surtout, elle montrerait que la propriété n'a pas été comprise.

Le coût est quadratique : environ n² sur 2 comparaisons dans le pire cas, ce qui est le prix des
tris par échanges voisins. Pourquoi l'apprendre alors que la bibliothèque trie en n log n ? Parce
que c'est l'exercice le plus court qui force à raisonner sur un invariant de boucle imbriquée —
« tout ce qui est au-delà de end est trié et définitif » — et que cette compétence-là se
réutilise dans du code métier ordinaire, chaque fois qu'une boucle maintient une zone acquise et
une zone à traiter.

Les cas cachés déplacent les tailles et les dispositions : tableau déjà trié, tableau en ordre
inverse, doublons. Le tableau trié réfute l'implémentation qui échangerait sans comparer ; les
doublons réfutent une comparaison écrite `>=`, qui échangerait des égaux sans nécessité — le
résultat resterait trié, mais l'exercice apprend à choisir l'inégalité stricte, celle qui fait
le moins de travail et qui, sur des objets réels, préserverait l'ordre relatif des égaux. Ce
détail, invisible sur des entiers, devient la stabilité du tri dès que les éléments portent
d'autres champs — une propriété que les systèmes de pagination exigent.
