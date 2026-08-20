# Explication

Deux booléens, trois statuts : la fonction est un arbre de décision minuscule, et sa valeur est
dans la sémantique HTTP qu'elle fige — que *dit* chaque statut au client, et laquelle des deux
vérités prime quand elles se contredisent.

Les trois réponses d'abord, parce que chacune est une promesse. Le 200 dit « voici la
ressource » — le client peut lire le corps et s'y fier. Le 201 dit davantage : « elle n'existait
pas, je viens de la créer » — et le client bien élevé en tire des conséquences, ranger
l'adresse de la nouveauté, rafraîchir une liste. Le 404 dit « rien ici » — et c'est une
information de plein droit, pas un échec du serveur : le client peut purger un lien mort,
proposer une création. L'énoncé demande ce qu'un succès sur une ressource absente ferait
croire : exactement le mensonge inverse — le client met en cache un corps vide, affiche une
fiche fantôme, et le bug se loge chez *lui*, à distance de sa cause. La discipline des statuts
est une discipline de vérité : chaque code est une phrase, et on ne prononce pas une phrase
fausse pour simplifier une branche.

La priorité ensuite, le cœur décidable de l'exercice : quand création et présence sont vraies
toutes deux, la création prime — le premier `if` retourne 201 sans regarder le reste. La
combinaison n'est pas exotique : créer une ressource la rend trouvée, et un flux « créer si
absent » finit naturellement avec les deux indicateurs levés. Répondre 200 dans ce cas
effacerait l'information la plus fraîche — le client ne saurait pas qu'il vient de provoquer
une création, et les caches ou les listes en aval resteraient périmés. La structure
garde-puis-décision transcrit la priorité, comme dans les grilles tarifaires : la règle
dominante en tête, le reste sur le domaine réduit.

Les quatre combinaisons s'énumèrent — l'énoncé impose de les écrire avant de coder — et les
cas les couvrent : créé et trouvé donne 201, créé seul donne 201 aussi, trouvé seul donne 200,
ni l'un ni l'autre donne 404. Le domaine fini est assumé ; la valeur de l'exercice est de
savoir *justifier* chaque ligne de cette table en une phrase de sémantique HTTP, ce que la
question d'entretien associée demande à voix haute.

Le coût est constant. La transposition est le réflexe de conception d'API : pour chaque route,
écrire la table situation-vers-statut avant le code, avec la règle de priorité quand plusieurs
vérités se superposent — et refuser le 200 paresseux qui « marche » : un statut est un contrat,
pas un ornement.
