# Explication

Appliquer une valeur par défaut positive et un plafond strict.

Deux comportements distincts pour deux entrées fautives. Une taille absente ou absurde retombe sur un défaut raisonnable, parce que l'appelant n'a probablement rien demandé de précis. Une taille excessive est ramenée au plafond plutôt que refusée : le client obtient un résultat, borné, plutôt qu'une erreur qu'il devrait apprendre à traiter.

Le plafond est ce qui empêche une seule adresse de saturer la mémoire du serveur. Il appartient au serveur, jamais au client, et gagne, dans une application réelle, à provenir d'un réglage de configuration plutôt qu'à être écrit dans le code — c'est le même principe que toute valeur qui change selon l'environnement. La décision est en temps constant.
