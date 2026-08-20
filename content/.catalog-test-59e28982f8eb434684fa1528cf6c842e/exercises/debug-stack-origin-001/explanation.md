# Explication

Lire une trace d'appels est la compétence numéro un du diagnostic, et cet exercice en automatise
le premier geste : sauter les cadres système pour trouver *notre* code. La raison mérite d'être
comprise avant la mécanique.

Une exception traverse des couches : elle naît souvent au fond d'une bibliothèque — un accès de
tableau, une conversion — mais la *cause* habite presque toujours le premier cadre applicatif,
celui où notre code a fourni la mauvaise donnée à la couche du dessous. Les cadres système
au-dessus racontent le trajet, pas l'origine. Filtrer la trace sur le préfixe du produit —
`at Forge.` — et prendre la *première* correspondance dans l'ordre de la trace, c'est pointer
directement l'endroit où ouvrir l'éditeur. C'est exactement ce que font les écrans « just my
code » des débogueurs, et l'écrire soi-même une fois dissipe leur magie.

La mécanique est un parcours de lignes avec trois précisions. Le découpage se fait sur le saut
de ligne, et chaque ligne est *rognée avant* le test de préfixe : les traces réelles indentent
leurs cadres — espaces ou tabulations selon la source — et un `StartsWith` sur la ligne brute
raterait tout. L'ordre rognage-puis-test n'est pas décoratif, c'est la condition de
fonctionnement, et le cas caché aux cadres indentés le vérifie. Le test lui-même est un
`StartsWith` ordinal — un préfixe technique se compare binairement — dont la précision compte :
le point final de `at Forge.` évite de capturer un espace de noms tiers qui commencerait par le
même mot. Enfin, le retour est *immédiat* à la première correspondance : la sortie précoce du
parcours, encore, parce que le verdict est acquis.

Le cas sans correspondance rend la chaîne vide — une trace entièrement système, un incident né
et mort hors du produit — et la même convention couvre l'entrée vide ou blanche. On aurait pu
préférer une valeur plus parlante ; le vide a l'avantage de composer : l'appelant teste la
longueur et décide.

Les cas cachés balaient les dispositions : la frame applicative en tête — elle-même la
réponse —, enfouie sous plusieurs cadres système, absente, et le rognage d'indentation.

Le coût est linéaire dans la taille de la trace, avec l'allocation du découpage — acceptable
pour un outil de diagnostic ; un analyseur de production travaillerait par plages sans
matérialiser les lignes.

La transposition est le tri du bruit dans toute sortie d'outillage : logs multi-couches, sorties
de compilateurs, rapports d'analyse — partout, la question est « quelle est la première ligne
qui parle de *mon* code ? », et la réponse est ce même filtre : normaliser la ligne, tester un
marqueur précis, s'arrêter à la première.
