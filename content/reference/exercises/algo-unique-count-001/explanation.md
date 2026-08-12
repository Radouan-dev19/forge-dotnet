# Explication

Utiliser un ensemble et compter zéro, les négatifs et les doublons.

Un ensemble absorbe les doublons par construction : insérer une valeur déjà présente ne fait rien, et la taille finale est exactement le nombre de valeurs distinctes. Le coût est linéaire en moyenne, contre un coût quadratique pour la comparaison de chaque paire et un coût en n log n pour un tri préalable.

Aucune valeur n'a de statut particulier : zéro et les négatifs comptent comme les autres. Le tableau vide donne zéro, ce qui est une réponse et non une erreur. L'espace croît avec le nombre de valeurs distinctes, ce qui est l'échange habituel — de la mémoire contre du temps.
