# Explication

Le cas de base couvre zéro et chaque appel réduit strictement la valeur.

Deux conditions rendent une récursion sûre : un cas de base atteignable et une réduction stricte à chaque appel. Ici le cas de base couvre zéro et un — ne traiter que un laisse l'appel sur zéro descendre vers les négatifs, et la pile se remplit avant qu'une exception utile n'apparaisse.

La borne supérieure est une décision, pas une précaution : au-delà de douze, le résultat ne tient plus dans un entier de trente-deux bits. Refuser explicitement vaut mieux que multiplier et rendre une valeur fausse ; la multiplication vérifiée est la seconde ligne de défense. Le temps est linéaire et la pile croît avec la valeur.
