# Explication

Les règles se chevauchent : une commande express peut aussi dépasser 80 €. Leur priorité doit donc être explicite. Après validation, la branche express est évaluée avant le seuil de gratuité standard.

Le test `>= 80m` encode clairement la borne incluse. Chaque chemin effectue au plus quelques comparaisons, donc temps et espace sont constants.
