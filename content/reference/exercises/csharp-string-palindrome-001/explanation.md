# Explication

Comparer les caractères symétriques après une normalisation annoncée.

La normalisation fait partie du contrat, et son périmètre doit être annoncé : ici les espaces et la casse, rien d'autre. Retirer aussi la ponctuation paraît plus généreux et change la réponse sur des entrées où le contrat disait non — un exercice qui fait plus que ce qu'il annonce est aussi difficile à utiliser qu'un qui en fait moins.

Le parcours par les deux extrémités s'arrête au centre : chaque paire est comparée une fois, et un écart permet de conclure immédiatement. La chaîne vide et la chaîne d'un seul caractère sont des palindromes, cas limites que la condition d'arrêt traite sans branche supplémentaire. Le temps est linéaire et l'espace correspond à la chaîne normalisée.
