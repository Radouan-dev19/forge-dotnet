# Explication

Refuser une fermeture sans ouverture et exiger une profondeur finale nulle.

Compter les ouvertures et les fermetures ne suffit pas : une chaîne qui ferme avant d'ouvrir en compte autant des deux et n'est pas équilibrée. C'est l'ordre qui décide, et un simple compteur le capture — à condition de refuser au moment exact où il passe sous zéro, pas à la fin.

Les deux conditions sont indépendantes et toutes deux nécessaires : ne jamais passer sous zéro pendant le parcours, et finir à zéro. La chaîne vide satisfait les deux, donc elle est équilibrée. Le parcours est linéaire et un compteur suffit, là où une pile occuperait un espace proportionnel à la profondeur.
