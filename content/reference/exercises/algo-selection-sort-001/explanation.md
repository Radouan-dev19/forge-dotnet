# Explication

Le tri par sélection se distingue des autres tris quadratiques par une promesse précise : à la
fin du tour `start`, le préfixe `result[0..start]` contient les plus petites valeurs du tableau,
triées, et *plus rien ne les déplacera jamais*. C'est un invariant plus fort que celui de
l'insertion, dont le préfixe trié reste provisoire — chaque nouvel élément peut s'y enfoncer. La
sélection décide définitivement, tour après tour, et cette différence a une conséquence
mesurable : au plus un échange par tour, soit `n` échanges en tout, quand l'insertion peut
déplacer chaque élément sur toute la longueur du préfixe. Sur des éléments coûteux à déplacer —
grandes structures, écritures sur support lent — la sélection minimise les écritures ; c'est sa
niche réelle, et la raison de l'apprendre autrement que par folklore.

La boucle interne est une recherche d'indice minimal sur la zone restante, exactement l'exercice
voisin réutilisé comme brique. On y retrouve la comparaison stricte, qui prend le premier
minimum à égalité ; ici, cela ne suffit pourtant pas à rendre le tri stable, car l'échange final
peut faire sauter un élément par-dessus ses égaux. L'instabilité de la sélection est un fait à
connaître, pas un défaut à corriger : elle est invisible sur des entiers, et disqualifiante dès
que l'ordre relatif des égaux porte du sens.

L'échange par déconstruction en fin de tour s'exécute même quand `min == start`. Échanger une
case avec elle-même est sans effet, et accepter ce petit travail inutile évite une branche
conditionnelle — un arbitrage entre micro-économie et simplicité que la solution tranche du côté
lisible. C'est une décision minuscule mais représentative : le code de référence choisit ce qui
s'explique en une phrase.

Le contrat impose l'entrée intacte, d'où la copie initiale, contrôlée par le harnais sur des cas
dédiés qui comparent les arguments avant et après l'appel. Les cas cachés font par ailleurs ce
qu'ils font sur tous les tris : ordre inverse, doublons, tableau déjà trié — ce dernier vérifie
que les tours « pour rien » n'abîment rien — et une disposition qui réfute la sortie copiée de
l'exemple.

Le coût en comparaisons est quadratique quoi qu'il arrive : la boucle interne parcourt toute la
zone restante même si le tableau est déjà trié, là où l'insertion en profite. Résumer la famille
en une ligne chacun — insertion : peu de comparaisons sur du presque-trié ; sélection : peu
d'écritures partout ; bulles : la plus simple à prouver — donne le vocabulaire pour choisir, et
c'est ce vocabulaire qui se transpose aux choix de structures et d'index dans du code métier.
