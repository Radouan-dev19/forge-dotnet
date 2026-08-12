# Explication

Valider les mesures, traiter les erreurs en priorité, puis comparer la latence au budget annoncé.

L'ordre traduit une hiérarchie de gravité : une erreur est un service non rendu, une latence dégradée est un service rendu moins bien. Évaluer la latence en premier masquerait le signal le plus fort dès qu'un incident produit les deux à la fois, ce qui est le cas courant.

Le budget est une valeur annoncée, pas une impression, et la comparaison est stricte : la latence exactement au budget respecte l'engagement. C'est la seule frontière du problème, et elle se teste dans les deux sens. Les mesures négatives ne décrivent aucun état observé et se refusent. La décision est en temps constant.
