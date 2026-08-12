# Explication

Normaliser le décalage, y compris négatif, avant l'indexation modulo.

Deux normalisations préalables suppriment l'essentiel des défauts. Ramener le décalage modulo la longueur rend équivalents un décalage de trois et un décalage de mille trois sur un tableau de dix. Et le reste d'une division en C# prend le signe du dividende : un décalage négatif produit un reste négatif, qu'il faut ramener dans l'intervalle en ajoutant la longueur puis en reprenant le reste.

Le tableau vide se traite avant tout calcul, la division par sa longueur n'ayant pas de sens. Produire un nouveau tableau évite la difficulté d'une rotation sur place, où chaque écriture détruit une valeur encore nécessaire. Le coût est linéaire et l'espace correspond au résultat.
