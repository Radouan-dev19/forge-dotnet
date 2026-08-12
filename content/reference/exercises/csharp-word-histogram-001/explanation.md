# Explication

Compter sans sensibilité à la casse et ignorer les séparateurs vides.

Le comparateur se choisit à la construction du dictionnaire, pas au moment de la lecture : le fixer après coup est impossible, et l'oublier fait de « Le » et « le » deux entrées distinctes. C'est une décision de conception, prise une fois, qui gouverne ensuite toutes les opérations.

Deux détails de découpage complètent la règle. Retirer les segments vides évite de compter un mot fantôme là où deux espaces se suivent. Et l'entrée vide retourne un dictionnaire vide plutôt qu'une valeur absente : l'appelant peut alors parcourir le résultat sans le tester. Le parcours est linéaire et l'espace croît avec le nombre de mots distincts.
