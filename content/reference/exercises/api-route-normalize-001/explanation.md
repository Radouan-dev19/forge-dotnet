# Explication

Retirer seulement les séparateurs de bordure et normaliser avec une culture invariante.

La restriction est le sujet : les séparateurs de bordure se retirent, les séparateurs internes se conservent, puisque ce sont eux qui découpent les segments. Une normalisation trop généreuse fusionne deux segments et transforme un chemin valide en chemin voisin, ce qui produit une absence incompréhensible côté client.

L'ordre importe aussi : retirer les blancs d'abord, sinon un espace de bordure protège un séparateur qui aurait dû tomber. Et la culture invariante évite qu'un même chemin ne se normalise différemment selon la machine, ce qui rendrait l'acheminement dépendant de la configuration du serveur. Le coût est linéaire dans la longueur du chemin.
