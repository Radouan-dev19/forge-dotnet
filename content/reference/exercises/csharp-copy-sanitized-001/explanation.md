# Explication

Retourner une nouvelle collection et préserver strictement l'entrée.

Assainir n'est pas filtrer : la longueur du résultat est exactement celle de l'entrée, et une valeur négative devient zéro au lieu de disparaître. Confondre les deux produit un tableau plus court, ce qu'aucun cas nominal sans valeur négative ne révèle.

Le chemin où il n'y a rien à corriger est le plus dangereux : retourner la référence reçue paraît gratuit et rompt le contrat d'immutabilité pour tous les appelants qui modifieront ensuite le résultat. La copie est allouée dans tous les cas. Le parcours est linéaire et l'espace correspond au tableau produit.
