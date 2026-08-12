# Explication

Le prédicat exprime la règle et la borne reste inclusive.

La borne est la seule frontière du problème : un élément exactement égal au minimum est compté. Écrire la comparaison stricte donne un résultat juste sur presque toutes les entrées et faux sur celle qui contient la valeur du seuil, c'est-à-dire précisément celle que l'on examine en revue.

Compter directement évite de construire une collection intermédiaire dont on ne lirait que la taille. Le gain n'est pas seulement de mémoire : l'intention devient lisible, on compte au lieu de filtrer puis mesurer. Le parcours est linéaire et l'espace constant.
