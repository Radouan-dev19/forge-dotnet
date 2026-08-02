# Explication

La validation complète précède la construction. Chaque clé est normalisée avec `ToLowerInvariant`, puis `TryGetValue` fournit le cumul existant. Un nouveau dictionnaire garantit l’immutabilité des deux sources.

La normalisation rend la sortie déterministe et évite deux stocks pour une même référence. Le temps est linéaire en moyenne dans le nombre total de paires.
