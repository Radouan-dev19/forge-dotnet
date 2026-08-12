# Explication

Définir la casse et ignorer les segments vides sans inventer de normalisation.

La tentation est d'en faire plus : uniformiser la casse, retirer les blancs, fusionner les variantes. Chacune de ces transformations change le résultat sans être annoncée, et rend le compte inexploitable par qui croyait compter des valeurs brutes. Un contrat qui fait plus que ce qu'il dit est aussi difficile à utiliser qu'un qui en fait moins.

Le retrait des segments vides, lui, est annoncé : deux séparateurs consécutifs ne décrivent aucun événement. La comparaison est ordinale, cohérente avec des identifiants techniques. Le parcours est linéaire en moyenne et l'espace croît avec le nombre de valeurs distinctes.
