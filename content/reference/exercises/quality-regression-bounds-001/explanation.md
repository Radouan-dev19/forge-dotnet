# Explication

La borne supérieure est strictement inférieure à la longueur et la borne basse vaut zéro.

Ce filet existe pour une raison précise : l'erreur de un est la plus banale de la programmation, et elle survit à la relecture parce que le code paraît correct. Fixer les cinq valeurs de bordure dans un test empêche une simplification future de déplacer la borne sans qu'on le voie.

Une longueur nulle n'admet aucun index : c'est le cas que les jeux d'essai oublient, et il tombe naturellement de la comparaison stricte. Une longueur négative, elle, ne décrit aucune collection et se refuse. La décision est en temps constant.
