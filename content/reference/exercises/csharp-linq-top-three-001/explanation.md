# Explication

Ordonner de façon décroissante puis borner sans supprimer les doublons.

Prendre trois éléments d'une séquence qui en compte moins n'est pas une erreur : l'opération rend ce qu'elle trouve. Ajouter une garde qui lève dans ce cas invente une contrainte que le contrat n'annonce pas et casse l'appelant sur une entrée parfaitement légitime.

Les doublons restent : les trois plus grandes valeurs peuvent être trois fois la même. Dédupliquer paraît raisonnable et change le résultat sur toute entrée à valeurs répétées. Le tri domine le temps, et l'espace correspond à la séquence ordonnée.
