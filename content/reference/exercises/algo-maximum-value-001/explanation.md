# Explication

Initialiser depuis le premier élément pour respecter les tableaux négatifs.

L'initialisation à zéro est correcte tant qu'une valeur positive existe, et fausse sinon : c'est le type de défaut qui traverse une revue et une recette avant d'apparaître en production sur un jeu de données inhabituel. Partir d'un élément réel supprime la question, au prix du traitement préalable du cas vide.

Le parcours peut démarrer au deuxième élément, puisque le premier a déjà servi ; le faire démarrer au premier n'est pas faux, seulement redondant. Le contrat retourne zéro pour une collection vide, décision annoncée plutôt que subie. Le parcours est linéaire et une variable suffit.
