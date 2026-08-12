# Explication

Créer une copie indépendante qui conserve ordre et doublons.

Une copie ne transforme rien : même ordre, mêmes doublons, même longueur. Toute opération ajoutée au passage — tri, déduplication — sort du contrat, et l'appelant qui comptait sur la fidélité découvrira l'écart bien plus tard.

L'indépendance obtenue est celle de la liste, pas celle de ses éléments. Ici les éléments sont des entiers, donc la distinction ne se voit pas ; avec des objets, modifier un élément de la copie modifierait aussi celui de l'original. Savoir que la copie est de surface évite de lui prêter une garantie qu'elle n'a pas. Le coût est linéaire et l'espace correspond à la copie.
