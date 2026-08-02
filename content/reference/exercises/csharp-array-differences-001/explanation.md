# Explication

Un tableau de `n` valeurs contient `n - 1` paires adjacentes lorsque `n >= 1`. L’index `i` du résultat lit donc les index `i` et `i + 1` de l’entrée. La boucle s’arrête avant `values.Length - 1`.

Allouer un nouveau tableau préserve l’entrée et rend le contrat d’immutabilité observable. Le temps est linéaire et l’espace correspond au résultat produit.
