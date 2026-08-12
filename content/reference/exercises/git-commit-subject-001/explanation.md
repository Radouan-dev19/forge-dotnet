# Explication

Exiger un sujet non vide, borné en longueur et sans point final.

La limite de longueur n'est pas décorative : les outils affichent la ligne de sujet seule dans les listes, et une ligne tronquée devient illisible au moment précis où elle devait servir — quand on cherche un commit parmi des centaines. La longueur se mesure après retrait des blancs, sinon un espace de bordure fait basculer un sujet valide.

L'absence de point final tient à la nature du sujet : c'est un titre, pas une phrase. La convention paraît mineure et elle rend l'historique homogène, donc lisible d'un coup d'œil. La frontière de longueur est inclusive et se teste dans les deux sens. Le coût est linéaire dans la longueur du sujet.
