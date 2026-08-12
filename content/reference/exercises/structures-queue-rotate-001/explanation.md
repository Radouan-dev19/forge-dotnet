# Explication

Normaliser le nombre de rotations et préserver la discipline de la file.

Deux réductions préalables évitent l'essentiel des défauts. Ramener le nombre de tours modulo la longueur transforme un million de rotations en quelques-unes, sans changer le résultat. Et le reste d'une division en C# prend le signe du dividende : un nombre de tours négatif produit un reste négatif, qu'il faut ramener dans l'intervalle en ajoutant la longueur puis en reprenant le reste.

La file vide se traite avant tout calcul, la division par sa longueur n'ayant pas de sens. La rotation elle-même respecte la discipline de la structure : on retire en tête, on remet en queue. Le coût est proportionnel à la longueur et au nombre de tours normalisé.
