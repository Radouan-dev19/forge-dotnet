# Explication

Retourner un nouvel état de même taille et préserver les données reçues.

La remise à zéro entre deux tests n'a de valeur que si elle n'emporte rien d'autre : écrire dans l'état reçu détruirait les données que l'appelant conservait, et le défaut ne se verrait qu'au test suivant, sous la forme d'un échec dépendant de l'ordre.

La longueur est préservée parce que c'est la forme de l'état, pas son contenu, qui doit survivre à la réinitialisation. Retourner la référence reçue lorsqu'elle est déjà vide paraît gratuit et rompt la même garantie pour tous les appelants qui écriront ensuite dans le résultat. Le coût est linéaire et l'espace correspond à l'état produit.
