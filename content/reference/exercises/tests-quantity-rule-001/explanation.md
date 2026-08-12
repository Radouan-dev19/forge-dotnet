# Explication

Exprimer directement la plage inclusive pour rendre ses quatre frontières testables.

Un intervalle fermé a deux frontières, donc quatre valeurs à tester : juste en dessous du minimum, le minimum, le maximum, juste au-dessus. Une valeur intérieure ne prouve rien de ces quatre-là, et c'est pourtant celle qu'un jeu d'essai spontané contient toujours.

Exprimer la plage en une seule condition inclusive, plutôt qu'en deux tests séparés, réduit la surface d'erreur : il n'y a qu'un endroit où se tromper de comparateur, et la lecture correspond directement à l'énoncé du contrat. La décision est en temps constant.
