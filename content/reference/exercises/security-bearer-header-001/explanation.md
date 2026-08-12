# Explication

Vérifier le schéma et la présence d'une preuve sans jamais retourner sa valeur.

La fonction répond par oui ou non, et c'est délibéré : faire remonter la valeur de la preuve la ferait circuler dans du code qui n'en a pas besoin, et tôt ou tard dans un journal. Une preuve d'identité ne quitte pas la couche qui la vérifie.

Deux contrôles distincts sont nécessaires. Le schéma se compare sans distinction de casse, comme la norme le prévoit. Et le schéma seul ne prouve rien : un en-tête réduit au mot attendu, ou suivi de blancs, doit être refusé. Enfin, la forme validée ici n'est que syntaxique — elle ne dit rien de la validité de la preuve, qui se vérifie ailleurs. Le coût est linéaire dans la longueur de l'en-tête.
